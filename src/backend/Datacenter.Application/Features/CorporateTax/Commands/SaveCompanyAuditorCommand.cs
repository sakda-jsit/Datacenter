using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.CorporateTax.DTOs;
using Datacenter.Application.Features.CorporateTax.Queries;
using Datacenter.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.CorporateTax.Commands;

/// <summary>ตั้งผู้ลงนามประจำบริษัท (ค่าเริ่มต้น ใช้ทุกปีที่ไม่มี override).</summary>
public record SetCompanyDefaultSignersCommand(int ClientCompanyId, CompanyDefaultSignersInput Data)
    : IRequest<CompanySignersDto>, IRequireCompanyAccess;

public class SetCompanyDefaultSignersCommandHandler(
    IApplicationDbContext db, ISender sender, ICurrentUserService user, IAuditService audit)
    : IRequestHandler<SetCompanyDefaultSignersCommand, CompanySignersDto>
{
    public async Task<CompanySignersDto> Handle(SetCompanyDefaultSignersCommand req, CancellationToken ct)
    {
        var company = await db.ClientCompanies.FirstOrDefaultAsync(c => c.Id == req.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", req.ClientCompanyId);

        company.DefaultAuditorId = req.Data.AuditorId;
        company.DefaultBookkeeperId = req.Data.BookkeeperId;
        company.ModifiedBy = user.Username;
        company.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("Update", "ClientCompany", company.Id.ToString(),
            afterValue: $"ผู้ลงนามประจำ: ผู้สอบ#{req.Data.AuditorId} / ผู้ทำบัญชี#{req.Data.BookkeeperId}",
            companyId: req.ClientCompanyId, cancellationToken: ct);

        // ปีที่ใช้ส่งกลับ = ปีปัจจุบันของ company (ไม่สำคัญสำหรับ default) → คืน resolved ของปีนั้น ๆ ผ่าน query แยก
        return await sender.Send(new GetCompanySignersQuery(req.ClientCompanyId, DateTime.UtcNow.Year), ct);
    }
}

/// <summary>บันทึกผู้ลงนามเฉพาะรอบปี (override + วันที่ในรายงาน). ว่างทั้งหมด = ลบ override ของปีนั้น.</summary>
public record SaveCompanyYearSignersCommand(int ClientCompanyId, int FiscalYear, CompanyYearSignersInput Data)
    : IRequest<CompanySignersDto>, IRequireCompanyAccess;

public class SaveCompanyYearSignersCommandHandler(
    IApplicationDbContext db, ISender sender, ICurrentUserService user, IAuditService audit)
    : IRequestHandler<SaveCompanyYearSignersCommand, CompanySignersDto>
{
    public async Task<CompanySignersDto> Handle(SaveCompanyYearSignersCommand req, CancellationToken ct)
    {
        var d = req.Data;
        var row = await db.CompanyAuditors
            .FirstOrDefaultAsync(x => x.ClientCompanyId == req.ClientCompanyId
                                   && x.FiscalYear == req.FiscalYear, ct);

        bool empty = d.AuditorId is null && d.BookkeeperId is null && d.SignDate is null;
        if (empty)
        {
            if (row is not null)
            {
                db.CompanyAuditors.Remove(row);
                await db.SaveChangesAsync(ct);
                await audit.LogAsync("Delete", "CompanyAuditor",
                    entityId: $"{req.ClientCompanyId}:{req.FiscalYear}",
                    companyId: req.ClientCompanyId, cancellationToken: ct);
            }
            return await sender.Send(new GetCompanySignersQuery(req.ClientCompanyId, req.FiscalYear), ct);
        }

        bool isNew = row is null;
        if (row is null)
        {
            row = new CompanyAuditor
            {
                ClientCompanyId = req.ClientCompanyId,
                FiscalYear = req.FiscalYear,
                CreatedBy = user.Username,
            };
            db.CompanyAuditors.Add(row);
        }
        else { row.ModifiedBy = user.Username; row.ModifiedAt = DateTime.UtcNow; }

        row.AuditorId = d.AuditorId;
        row.BookkeeperId = d.BookkeeperId;
        row.SignDate = d.SignDate;

        // เปลี่ยนผู้ลงนามปีนี้ → เลื่อนเป็นค่าเริ่มต้นบริษัทด้วย (ปีถัดไปดึงคนใหม่อัตโนมัติ);
        // ปีเก่าที่ override ไว้ยังคงค่าเดิม (freeze ประวัติ) เพราะมี AuditorId/BookkeeperId ของตัวเอง
        if (d.AuditorId is not null || d.BookkeeperId is not null)
        {
            var company = await db.ClientCompanies.FirstOrDefaultAsync(c => c.Id == req.ClientCompanyId, ct);
            if (company is not null)
            {
                if (d.AuditorId is not null) company.DefaultAuditorId = d.AuditorId;
                if (d.BookkeeperId is not null) company.DefaultBookkeeperId = d.BookkeeperId;
                company.ModifiedBy = user.Username;
                company.ModifiedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(ct);

        await audit.LogAsync(isNew ? "Create" : "Update", "CompanyAuditor",
            entityId: $"{req.ClientCompanyId}:{req.FiscalYear}",
            afterValue: $"override+เลื่อน default ผู้สอบ#{d.AuditorId} / ผู้ทำบัญชี#{d.BookkeeperId} / ลงวันที่ {d.SignDate:yyyy-MM-dd}",
            companyId: req.ClientCompanyId, cancellationToken: ct);

        return await sender.Send(new GetCompanySignersQuery(req.ClientCompanyId, req.FiscalYear), ct);
    }
}

public class SetCompanyDefaultSignersCommandValidator : AbstractValidator<SetCompanyDefaultSignersCommand>
{
    public SetCompanyDefaultSignersCommandValidator()
        => RuleFor(x => x.ClientCompanyId).GreaterThan(0);
}

public class SaveCompanyYearSignersCommandValidator : AbstractValidator<SaveCompanyYearSignersCommand>
{
    public SaveCompanyYearSignersCommandValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0);
        RuleFor(x => x.FiscalYear).InclusiveBetween(2000, 2100);
    }
}
