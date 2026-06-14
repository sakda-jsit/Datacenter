using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Vat.DTOs;
using Datacenter.Application.Features.Vat.Queries;
using Datacenter.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Vat;

// ── Query: รายการ DEPCOD ที่พบในข้อมูล + การแมพ ─────────────────────────────────
public record GetVatBranchMappingsQuery(int ClientCompanyId)
    : IRequest<IReadOnlyList<VatBranchMappingDto>>, IRequireCompanyAccess;

public class GetVatBranchMappingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetVatBranchMappingsQuery, IReadOnlyList<VatBranchMappingDto>>
{
    public async Task<IReadOnlyList<VatBranchMappingDto>> Handle(GetVatBranchMappingsQuery req, CancellationToken ct)
    {
        // DEPCOD ที่พบจริงในข้อมูล VAT ของบริษัท + จำนวนรายการ
        var found = await db.VatEntries.AsNoTracking()
            .Where(v => v.ClientCompanyId == req.ClientCompanyId)
            .GroupBy(v => v.DepartmentCode)
            .Select(g => new { Dep = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var maps = await db.VatBranchMappings.AsNoTracking()
            .Where(m => m.ClientCompanyId == req.ClientCompanyId)
            .ToListAsync(ct);

        var result = found
            .Select(f =>
            {
                var raw = (f.Dep ?? "").Trim();
                var map = maps.FirstOrDefault(m => (m.DepartmentCode ?? "") == raw);
                if (map is not null)
                    return new VatBranchMappingDto(raw, raw.Length == 0 ? "(ไม่ระบุ)" : raw,
                        map.RdBranchNo, map.IsHeadOffice, map.BranchName, true, f.Count);

                var (branchNo, isHo) = GetVatPp30BranchesQueryHandler.ConventionBranch(raw);
                return new VatBranchMappingDto(raw, raw.Length == 0 ? "(ไม่ระบุ)" : raw,
                    branchNo, isHo, null, false, f.Count);
            })
            .OrderByDescending(x => x.IsHeadOffice)
            .ThenBy(x => x.RdBranchNo, StringComparer.Ordinal)
            .ToList();

        return result;
    }
}

// ── Upsert ───────────────────────────────────────────────────────────────────────
public record UpsertVatBranchMappingCommand(int ClientCompanyId, VatBranchMappingInput Data)
    : IRequest, IRequireCompanyAccess;

public class UpsertVatBranchMappingCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<UpsertVatBranchMappingCommand>
{
    public async Task Handle(UpsertVatBranchMappingCommand req, CancellationToken ct)
    {
        var dep = (req.Data.DepartmentCode ?? "").Trim();
        var entity = await db.VatBranchMappings
            .FirstOrDefaultAsync(m => m.ClientCompanyId == req.ClientCompanyId
                                   && (m.DepartmentCode ?? "") == dep, ct);

        if (entity is null)
        {
            entity = new VatBranchMapping
            {
                ClientCompanyId = req.ClientCompanyId,
                DepartmentCode = dep,
                CreatedBy = currentUser.Username,
            };
            db.VatBranchMappings.Add(entity);
        }
        else
        {
            entity.ModifiedBy = currentUser.Username;
            entity.ModifiedAt = DateTime.UtcNow;
        }

        entity.RdBranchNo = req.Data.RdBranchNo.Trim();
        entity.IsHeadOffice = req.Data.IsHeadOffice;
        entity.BranchName = string.IsNullOrWhiteSpace(req.Data.BranchName) ? null : req.Data.BranchName.Trim();

        await audit.LogAsync("Upsert", "VatBranchMapping",
            entityId: $"{req.ClientCompanyId}:{dep}",
            afterValue: $"DEPCOD {dep} → สาขา {entity.RdBranchNo}{(entity.IsHeadOffice ? " (สนญ.)" : "")}",
            companyId: req.ClientCompanyId, cancellationToken: ct);

        await db.SaveChangesAsync(ct);
    }
}

// ── Delete (กลับไปใช้กฎอัตโนมัติ) ───────────────────────────────────────────────
public record DeleteVatBranchMappingCommand(int ClientCompanyId, string DepartmentCode)
    : IRequest, IRequireCompanyAccess;

public class DeleteVatBranchMappingCommandHandler(IApplicationDbContext db, IAuditService audit)
    : IRequestHandler<DeleteVatBranchMappingCommand>
{
    public async Task Handle(DeleteVatBranchMappingCommand req, CancellationToken ct)
    {
        var dep = (req.DepartmentCode ?? "").Trim();
        var entity = await db.VatBranchMappings
            .FirstOrDefaultAsync(m => m.ClientCompanyId == req.ClientCompanyId
                                   && (m.DepartmentCode ?? "") == dep, ct);
        if (entity is null) return;

        db.VatBranchMappings.Remove(entity);
        await audit.LogAsync("Delete", "VatBranchMapping",
            entityId: $"{req.ClientCompanyId}:{dep}",
            beforeValue: $"DEPCOD {dep} → สาขา {entity.RdBranchNo}",
            companyId: req.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);
    }
}

public class UpsertVatBranchMappingCommandValidator : AbstractValidator<UpsertVatBranchMappingCommand>
{
    public UpsertVatBranchMappingCommandValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.RdBranchNo).NotEmpty().MaximumLength(10)
            .Matches(@"^\d{1,10}$").WithMessage("เลขสาขาต้องเป็นตัวเลข (เช่น 00000, 00001)");
        RuleFor(x => x.Data.DepartmentCode).MaximumLength(8);
        RuleFor(x => x.Data.BranchName).MaximumLength(120);
    }
}
