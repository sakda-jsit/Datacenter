using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.CorporateTax.DTOs;
using Datacenter.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.CorporateTax.Commands;

/// <summary>
/// บันทึก (upsert) ผู้ตรวจสอบและรับรองบัญชีของ (บริษัท, ปีงบ).
/// ถ้าส่งชื่อว่าง → ลบบันทึกของปีนั้น (เคลียร์ผู้สอบ).
/// </summary>
public record SaveCompanyAuditorCommand(int ClientCompanyId, int FiscalYear, CompanyAuditorInput Data)
    : IRequest<CompanyAuditorDto>, IRequireCompanyAccess;

public class SaveCompanyAuditorCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<SaveCompanyAuditorCommand, CompanyAuditorDto>
{
    public async Task<CompanyAuditorDto> Handle(SaveCompanyAuditorCommand req, CancellationToken ct)
    {
        var d = req.Data;
        var entity = await db.CompanyAuditors
            .FirstOrDefaultAsync(x => x.ClientCompanyId == req.ClientCompanyId
                                   && x.FiscalYear == req.FiscalYear, ct);

        // ทั้งชื่อผู้สอบและผู้ทำบัญชีว่าง = เคลียร์บันทึกของปีนี้
        if (string.IsNullOrWhiteSpace(d.AuditorName) && string.IsNullOrWhiteSpace(d.BookkeeperName))
        {
            if (entity is not null)
            {
                db.CompanyAuditors.Remove(entity);
                await db.SaveChangesAsync(ct);
                await audit.LogAsync("Delete", "CompanyAuditor",
                    entityId: $"{req.ClientCompanyId}:{req.FiscalYear}",
                    companyId: req.ClientCompanyId, cancellationToken: ct);
            }
            return new CompanyAuditorDto(req.ClientCompanyId, req.FiscalYear, "", null, null, null, null, null, null, Exists: false);
        }

        bool isNew = entity is null;
        if (entity is null)
        {
            entity = new CompanyAuditor
            {
                ClientCompanyId = req.ClientCompanyId,
                FiscalYear = req.FiscalYear,
                CreatedBy = currentUser.Username,
            };
            db.CompanyAuditors.Add(entity);
        }
        else
        {
            entity.ModifiedBy = currentUser.Username;
            entity.ModifiedAt = DateTime.UtcNow;
        }

        entity.AuditorName = (d.AuditorName ?? "").Trim();
        entity.AuditorLicenseNo = string.IsNullOrWhiteSpace(d.AuditorLicenseNo) ? null : d.AuditorLicenseNo.Trim();
        entity.AuditorTaxId = string.IsNullOrWhiteSpace(d.AuditorTaxId)
            ? null : new string(d.AuditorTaxId.Where(char.IsDigit).ToArray());
        entity.BookkeeperName = string.IsNullOrWhiteSpace(d.BookkeeperName) ? null : d.BookkeeperName.Trim();
        entity.BookkeeperTaxId = string.IsNullOrWhiteSpace(d.BookkeeperTaxId)
            ? null : new string(d.BookkeeperTaxId.Where(char.IsDigit).ToArray());
        entity.SignDate = d.SignDate;
        entity.Note = string.IsNullOrWhiteSpace(d.Note) ? null : d.Note.Trim();

        await db.SaveChangesAsync(ct);

        await audit.LogAsync(isNew ? "Create" : "Update", "CompanyAuditor",
            entityId: $"{req.ClientCompanyId}:{req.FiscalYear}",
            afterValue: $"ผู้สอบ {entity.AuditorName} (ทะเบียน {entity.AuditorLicenseNo}) / ผู้ทำบัญชี {entity.BookkeeperName}",
            companyId: req.ClientCompanyId, cancellationToken: ct);

        return new CompanyAuditorDto(entity.ClientCompanyId, entity.FiscalYear, entity.AuditorName,
            entity.AuditorLicenseNo, entity.AuditorTaxId, entity.BookkeeperName, entity.BookkeeperTaxId,
            entity.SignDate, entity.Note, Exists: true);
    }
}

public class CompanyAuditorInputValidator : AbstractValidator<CompanyAuditorInput>
{
    public CompanyAuditorInputValidator()
    {
        RuleFor(x => x.AuditorName).MaximumLength(200);
        RuleFor(x => x.AuditorLicenseNo).MaximumLength(20);
        RuleFor(x => x.AuditorTaxId)
            .Must(v => string.IsNullOrWhiteSpace(v) || v.Count(char.IsDigit) == 13)
            .WithMessage("เลขประจำตัวผู้เสียภาษีอากรของผู้สอบบัญชีต้องมี 13 หลัก");
        RuleFor(x => x.BookkeeperName).MaximumLength(200);
        RuleFor(x => x.BookkeeperTaxId)
            .Must(v => string.IsNullOrWhiteSpace(v) || v.Count(char.IsDigit) == 13)
            .WithMessage("เลขประจำตัวผู้เสียภาษีอากรของผู้ทำบัญชีต้องมี 13 หลัก");
    }
}

public class SaveCompanyAuditorCommandValidator : AbstractValidator<SaveCompanyAuditorCommand>
{
    public SaveCompanyAuditorCommandValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0);
        RuleFor(x => x.FiscalYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Data).NotNull().SetValidator(new CompanyAuditorInputValidator());
    }
}
