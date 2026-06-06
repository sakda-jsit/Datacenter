using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Payroll.DTOs;
using Datacenter.Application.Features.Payroll.Services;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Commands;

// อัตราเงินสมทบ ปกส./กองทุนทดแทน = **ค่ากลางของระบบ** (ไม่แยกบริษัท), effective-dated,
// เปลี่ยนรายเดือนในปีเดียวกันได้ (เพิ่มแถวใหม่). จัดการในเมนูระบบ — ไม่ผูก IRequireCompanyAccess.

static class PayrollConfigMap
{
    public static PayrollRateConfigDto ToDto(PayrollRateConfig c) => new(
        c.Id, c.EffectiveFrom, c.SsoEmployeePct, c.SsoEmployerPct, c.SsoWageFloor, c.SsoWageCap,
        c.WcfRatePct, c.WcfWageCapPerYear, c.Note);
}

// ── list ทั้งหมด (เรียงตามวันที่มีผล) ─────────────────────────────────────────────
public record GetPayrollConfigsQuery : IRequest<IReadOnlyList<PayrollRateConfigDto>>;

public class GetPayrollConfigsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPayrollConfigsQuery, IReadOnlyList<PayrollRateConfigDto>>
{
    public async Task<IReadOnlyList<PayrollRateConfigDto>> Handle(GetPayrollConfigsQuery request, CancellationToken ct)
    {
        var list = await db.PayrollRateConfigs.AsNoTracking()
            .OrderByDescending(c => c.EffectiveFrom)
            .ToListAsync(ct);
        return list.Select(PayrollConfigMap.ToDto).ToList();
    }
}

// ── อัตราที่มีผล ณ วันที่ (ใช้ preview + ตัวคำนวณ P2b) ──────────────────────────
public record GetEffectivePayrollConfigQuery(DateTime AsOf) : IRequest<PayrollRateConfigDto?>;

public class GetEffectivePayrollConfigQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetEffectivePayrollConfigQuery, PayrollRateConfigDto?>
{
    public async Task<PayrollRateConfigDto?> Handle(GetEffectivePayrollConfigQuery request, CancellationToken ct)
    {
        var all = await db.PayrollRateConfigs.AsNoTracking().ToListAsync(ct);
        var eff = PayrollRates.ResolveEffective(all, request.AsOf);
        return eff is null ? null : PayrollConfigMap.ToDto(eff);
    }
}

// ── upsert (เพิ่ม/แก้อัตราค่ากลาง) ───────────────────────────────────────────────
public record UpsertPayrollConfigCommand(int? Id, PayrollRateConfigInput Data) : IRequest<int>;

public class UpsertPayrollConfigCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpsertPayrollConfigCommand, int>
{
    public async Task<int> Handle(UpsertPayrollConfigCommand request, CancellationToken ct)
    {
        PayrollRateConfig c;
        if (request.Id is { } id)
        {
            c = await db.PayrollRateConfigs.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NotFoundException("PayrollRateConfig", id);
            c.ModifiedBy = currentUser.Username;
            c.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
            c = new PayrollRateConfig { ClientCompanyId = null, CreatedBy = currentUser.Username };
            db.PayrollRateConfigs.Add(c);
        }
        var d = request.Data;
        c.ClientCompanyId = null; // ค่ากลางเสมอ
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

// ── ลบ ───────────────────────────────────────────────────────────────────────────
public record DeletePayrollConfigCommand(int Id) : IRequest<Unit>;

public class DeletePayrollConfigCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeletePayrollConfigCommand, Unit>
{
    public async Task<Unit> Handle(DeletePayrollConfigCommand request, CancellationToken ct)
    {
        var c = await db.PayrollRateConfigs.FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException("PayrollRateConfig", request.Id);
        db.PayrollRateConfigs.Remove(c);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
