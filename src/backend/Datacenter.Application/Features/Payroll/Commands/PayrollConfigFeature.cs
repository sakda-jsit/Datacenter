using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Payroll.DTOs;
using Datacenter.Application.Features.Payroll.Services;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Commands;

static class PayrollConfigMap
{
    public static PayrollRateConfigDto ToDto(PayrollRateConfig c) => new(
        c.Id, c.ClientCompanyId, c.ClientCompanyId == null, c.EffectiveFrom,
        c.SsoEmployeePct, c.SsoEmployerPct, c.SsoWageFloor, c.SsoWageCap,
        c.WcfRatePct, c.WcfWageCapPerYear, c.Note);
}

// ── list (ค่ากลาง + ของบริษัท) ─────────────────────────────────────────────────
public record GetPayrollConfigsQuery(int ClientCompanyId)
    : IRequest<IReadOnlyList<PayrollRateConfigDto>>, IRequireCompanyAccess;

public class GetPayrollConfigsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPayrollConfigsQuery, IReadOnlyList<PayrollRateConfigDto>>
{
    public async Task<IReadOnlyList<PayrollRateConfigDto>> Handle(GetPayrollConfigsQuery request, CancellationToken ct)
    {
        var list = await db.PayrollRateConfigs.AsNoTracking()
            .Where(c => c.ClientCompanyId == null || c.ClientCompanyId == request.ClientCompanyId)
            .OrderByDescending(c => c.EffectiveFrom).ThenBy(c => c.ClientCompanyId)
            .ToListAsync(ct);
        return list.Select(PayrollConfigMap.ToDto).ToList();
    }
}

// ── อัตราที่มีผล ณ วันที่ (ใช้ preview + ตัวคำนวณ) ───────────────────────────────
public record GetEffectivePayrollConfigQuery(int ClientCompanyId, DateTime AsOf)
    : IRequest<PayrollRateConfigDto?>, IRequireCompanyAccess;

public class GetEffectivePayrollConfigQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetEffectivePayrollConfigQuery, PayrollRateConfigDto?>
{
    public async Task<PayrollRateConfigDto?> Handle(GetEffectivePayrollConfigQuery request, CancellationToken ct)
    {
        var all = await db.PayrollRateConfigs.AsNoTracking().ToListAsync(ct);
        var eff = PayrollRates.ResolveEffective(all, request.ClientCompanyId, request.AsOf);
        return eff is null ? null : PayrollConfigMap.ToDto(eff);
    }
}

// ── upsert (สร้าง/แก้อัตราเฉพาะบริษัท — เพิ่มแถวมีผลตามวันที่) ──────────────────
public record UpsertPayrollConfigCommand(int ClientCompanyId, int? Id, PayrollRateConfigInput Data)
    : IRequest<int>, IRequireCompanyAccess;

public class UpsertPayrollConfigCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpsertPayrollConfigCommand, int>
{
    public async Task<int> Handle(UpsertPayrollConfigCommand request, CancellationToken ct)
    {
        PayrollRateConfig c;
        if (request.Id is { } id)
        {
            c = await db.PayrollRateConfigs.FirstOrDefaultAsync(
                    x => x.Id == id && x.ClientCompanyId == request.ClientCompanyId, ct)
                ?? throw new NotFoundException("PayrollRateConfig", id); // แก้ได้เฉพาะของบริษัท (ไม่ใช่ค่ากลาง)
            c.ModifiedBy = currentUser.Username;
            c.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
            c = new PayrollRateConfig { ClientCompanyId = request.ClientCompanyId, CreatedBy = currentUser.Username };
            db.PayrollRateConfigs.Add(c);
        }
        var d = request.Data;
        c.EffectiveFrom = d.EffectiveFrom;
        c.SsoEmployeePct = d.SsoEmployeePct;
        c.SsoEmployerPct = d.SsoEmployerPct;
        c.SsoWageFloor = d.SsoWageFloor;
        c.SsoWageCap = d.SsoWageCap;
        c.WcfRatePct = d.WcfRatePct;
        c.WcfWageCapPerYear = d.WcfWageCapPerYear;
        c.Note = d.Note;
        await db.SaveChangesAsync(ct);
        return c.Id;
    }
}

// ── ลบ (เฉพาะของบริษัท) ─────────────────────────────────────────────────────────
public record DeletePayrollConfigCommand(int ClientCompanyId, int Id)
    : IRequest<Unit>, IRequireCompanyAccess;

public class DeletePayrollConfigCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeletePayrollConfigCommand, Unit>
{
    public async Task<Unit> Handle(DeletePayrollConfigCommand request, CancellationToken ct)
    {
        var c = await db.PayrollRateConfigs.FirstOrDefaultAsync(
                x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("PayrollRateConfig", request.Id);
        db.PayrollRateConfigs.Remove(c);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
