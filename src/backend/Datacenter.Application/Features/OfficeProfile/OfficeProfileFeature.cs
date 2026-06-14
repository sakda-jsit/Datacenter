using Datacenter.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OfficeProfileEntity = Datacenter.Domain.Entities.OfficeProfile;

namespace Datacenter.Application.Features.OfficeProfile;

// โปรไฟล์สำนักงานบัญชี = ค่ากลางของระบบ (singleton, ไม่แยกบริษัทลูกค้า) — ไม่ผูก IRequireCompanyAccess.

public record OfficeProfileDto(
    string OfficeName, string? TaxId, string? BranchCode, string? Address, string? Phone);

public record OfficeProfileInput(
    string OfficeName, string? TaxId, string? BranchCode, string? Address, string? Phone);

// ── อ่านโปรไฟล์ (คืนค่าว่างถ้ายังไม่ตั้ง) ──
public record GetOfficeProfileQuery : IRequest<OfficeProfileDto>;

public class GetOfficeProfileQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetOfficeProfileQuery, OfficeProfileDto>
{
    public async Task<OfficeProfileDto> Handle(GetOfficeProfileQuery req, CancellationToken ct)
    {
        var e = await db.OfficeProfiles.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        return e is null
            ? new OfficeProfileDto("", null, null, null, null)
            : new OfficeProfileDto(e.OfficeName, e.TaxId, e.BranchCode, e.Address, e.Phone);
    }
}

// ── บันทึก (upsert singleton) ──
public record SaveOfficeProfileCommand(OfficeProfileInput Data) : IRequest<OfficeProfileDto>;

public class SaveOfficeProfileCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<SaveOfficeProfileCommand, OfficeProfileDto>
{
    public async Task<OfficeProfileDto> Handle(SaveOfficeProfileCommand req, CancellationToken ct)
    {
        var d = req.Data;
        var e = await db.OfficeProfiles.OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        bool isNew = e is null;
        if (e is null)
        {
            e = new OfficeProfileEntity { CreatedBy = currentUser.Username };
            db.OfficeProfiles.Add(e);
        }
        else
        {
            e.ModifiedBy = currentUser.Username;
            e.ModifiedAt = DateTime.UtcNow;
        }

        e.OfficeName = (d.OfficeName ?? "").Trim();
        e.TaxId = string.IsNullOrWhiteSpace(d.TaxId) ? null : new string(d.TaxId.Where(char.IsDigit).ToArray());
        e.BranchCode = string.IsNullOrWhiteSpace(d.BranchCode) ? null : d.BranchCode.Trim();
        e.Address = string.IsNullOrWhiteSpace(d.Address) ? null : d.Address.Trim();
        e.Phone = string.IsNullOrWhiteSpace(d.Phone) ? null : d.Phone.Trim();

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(isNew ? "Create" : "Update", "OfficeProfile",
            entityId: e.Id.ToString(), afterValue: $"{e.OfficeName} / {e.TaxId}", cancellationToken: ct);

        return new OfficeProfileDto(e.OfficeName, e.TaxId, e.BranchCode, e.Address, e.Phone);
    }
}

public class OfficeProfileInputValidator : AbstractValidator<OfficeProfileInput>
{
    public OfficeProfileInputValidator()
    {
        RuleFor(x => x.OfficeName).MaximumLength(300);
        RuleFor(x => x.TaxId)
            .Must(v => string.IsNullOrWhiteSpace(v) || v.Count(char.IsDigit) == 13)
            .WithMessage("เลขประจำตัวผู้เสียภาษีอากรของสำนักงานต้องมี 13 หลัก");
    }
}

public class SaveOfficeProfileCommandValidator : AbstractValidator<SaveOfficeProfileCommand>
{
    public SaveOfficeProfileCommandValidator()
        => RuleFor(x => x.Data).NotNull().SetValidator(new OfficeProfileInputValidator());
}
