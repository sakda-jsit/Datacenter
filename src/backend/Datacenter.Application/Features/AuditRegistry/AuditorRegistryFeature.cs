using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.AuditRegistry;

// ทะเบียนผู้สอบบัญชี = master ของสำนักงาน (ใช้ซ้ำหลายบริษัท) — ค่ากลาง ไม่ผูก IRequireCompanyAccess.

public record AuditorDto(
    int Id, string Name, AuditorType Type, string? LicenseNo, string? TaxId,
    string? AuditFirmName, string? AuditFirmTaxId, bool IsActive);

public record AuditorInput(
    string Name, AuditorType Type, string? LicenseNo, string? TaxId,
    string? AuditFirmName, string? AuditFirmTaxId, bool IsActive);

public static class AuditorMap
{
    public static AuditorDto ToDto(Auditor a) => new(
        a.Id, a.Name, a.Type, a.LicenseNo, a.TaxId, a.AuditFirmName, a.AuditFirmTaxId, a.IsActive);
}

public record GetAuditorsQuery : IRequest<IReadOnlyList<AuditorDto>>;

public class GetAuditorsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAuditorsQuery, IReadOnlyList<AuditorDto>>
{
    public async Task<IReadOnlyList<AuditorDto>> Handle(GetAuditorsQuery req, CancellationToken ct)
        => (await db.Auditors.AsNoTracking().OrderByDescending(x => x.IsActive).ThenBy(x => x.Name).ToListAsync(ct))
            .Select(AuditorMap.ToDto).ToList();
}

public record UpsertAuditorCommand(int? Id, AuditorInput Data) : IRequest<int>;

public class UpsertAuditorCommandHandler(IApplicationDbContext db, ICurrentUserService user, IAuditService audit)
    : IRequestHandler<UpsertAuditorCommand, int>
{
    public async Task<int> Handle(UpsertAuditorCommand req, CancellationToken ct)
    {
        var d = req.Data;
        Auditor a;
        bool isNew = req.Id is null;
        if (req.Id is { } id)
        {
            a = await db.Auditors.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NotFoundException("Auditor", id);
            a.ModifiedBy = user.Username; a.ModifiedAt = DateTime.UtcNow;
        }
        else { a = new Auditor { CreatedBy = user.Username }; db.Auditors.Add(a); }

        a.Name = (d.Name ?? "").Trim();
        a.Type = d.Type;
        a.LicenseNo = d.LicenseNo?.Trim();
        a.TaxId = Digits13OrNull(d.TaxId);
        a.AuditFirmName = string.IsNullOrWhiteSpace(d.AuditFirmName) ? null : d.AuditFirmName.Trim();
        a.AuditFirmTaxId = Digits13OrNull(d.AuditFirmTaxId);
        a.IsActive = d.IsActive;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(isNew ? "Create" : "Update", "Auditor", a.Id.ToString(),
            afterValue: $"{a.Name} ({a.Type}) ทะเบียน {a.LicenseNo}", cancellationToken: ct);
        return a.Id;
    }

    internal static string? Digits13OrNull(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : new string(s.Where(char.IsDigit).ToArray());
}

public record DeleteAuditorCommand(int Id) : IRequest;

public class DeleteAuditorCommandHandler(IApplicationDbContext db, ICurrentUserService user, IAuditService audit)
    : IRequestHandler<DeleteAuditorCommand>
{
    public async Task Handle(DeleteAuditorCommand req, CancellationToken ct)
    {
        var a = await db.Auditors.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("Auditor", req.Id);
        // soft-delete (อาจถูกอ้างอิงจากบริษัท/ปี) — ซ่อนจาก dropdown
        a.IsActive = false;
        a.ModifiedBy = user.Username; a.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("Delete", "Auditor", a.Id.ToString(), afterValue: a.Name, cancellationToken: ct);
    }
}

public class AuditorInputValidator : AbstractValidator<AuditorInput>
{
    public AuditorInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("ต้องระบุชื่อผู้สอบบัญชี").MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.TaxId).Must(Ok).WithMessage("เลขผู้เสียภาษีผู้สอบบัญชีต้องมี 13 หลัก");
        RuleFor(x => x.AuditFirmTaxId).Must(Ok).WithMessage("เลขผู้เสียภาษีสำนักงานสอบบัญชีต้องมี 13 หลัก");
    }
    static bool Ok(string? v) => string.IsNullOrWhiteSpace(v) || v.Count(char.IsDigit) == 13;
}

public class UpsertAuditorCommandValidator : AbstractValidator<UpsertAuditorCommand>
{
    public UpsertAuditorCommandValidator()
        => RuleFor(x => x.Data).NotNull().SetValidator(new AuditorInputValidator());
}
