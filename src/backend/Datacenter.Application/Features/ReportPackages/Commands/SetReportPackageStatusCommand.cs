using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FinancialStatement.Queries;
using Datacenter.Application.Features.ReportPackages.DTOs;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ReportPackages.Commands;

/// <summary>
/// เปลี่ยนสถานะชุดรายงานงบ (Draft/Review/Final/Locked). ดูกฎใน handler:
/// - เข้าสู่ Final = snapshot ชื่อบริษัท + ยอดสรุปงบ ณ ตอนนั้น
/// - Final → Locked (ยื่นแล้ว ห้ามแก้); Locked → Final = ปลดล็อก (ผู้มีสิทธิ์ในบริษัท + audit)
/// - Locked แก้ไปสถานะอื่นที่ไม่ใช่ Final ไม่ได้ (ต้องปลดล็อกก่อน)
/// </summary>
public record SetReportPackageStatusCommand(int ClientCompanyId, int Id, int TargetStatus)
    : IRequest<ReportPackageDto>, IRequireCompanyAccess;

public class SetReportPackageStatusCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit, IMediator mediator)
    : IRequestHandler<SetReportPackageStatusCommand, ReportPackageDto>
{
    public async Task<ReportPackageDto> Handle(SetReportPackageStatusCommand request, CancellationToken ct)
    {
        var pkg = await db.ReportPackages
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("ReportPackage", request.Id);

        if (!Enum.IsDefined(typeof(ReportPackageStatus), request.TargetStatus))
            throw Invalid("สถานะไม่ถูกต้อง");
        var target = (ReportPackageStatus)request.TargetStatus;
        var current = pkg.Status;
        if (target == current) return ReportPackageMapper.ToDto(pkg);

        // Locked → ไปได้แค่ Final (ปลดล็อก) เท่านั้น
        if (current == ReportPackageStatus.Locked && target != ReportPackageStatus.Final)
            throw Invalid("ชุดรายงานถูกล็อก (ยื่นแล้ว) — ต้องปลดล็อกเป็น Final ก่อน หรือเปิด version ใหม่");

        // Locked เท่านั้นที่มาจาก Final
        if (target == ReportPackageStatus.Locked && current != ReportPackageStatus.Final)
            throw Invalid("ต้องอยู่สถานะ Final ก่อนจึงล็อกได้");

        var now = DateTime.UtcNow;
        var user = currentUser.Username;
        string action;

        if (current == ReportPackageStatus.Locked && target == ReportPackageStatus.Final)
        {
            // ปลดล็อก: คง snapshot เดิมไว้ (ไม่ re-finalize), เคลียร์ข้อมูลล็อก
            pkg.LockedAt = null;
            pkg.LockedBy = null;
            action = "UnlockReportPackage";
        }
        else if (target == ReportPackageStatus.Final && current != ReportPackageStatus.Final)
        {
            await CaptureSnapshotAsync(pkg, ct);
            pkg.FinalizedAt = now;
            pkg.FinalizedBy = user;
            action = "FinalizeReportPackage";
        }
        else if (target == ReportPackageStatus.Locked)
        {
            pkg.LockedAt = now;
            pkg.LockedBy = user;
            action = "LockReportPackage";
        }
        else
        {
            action = "SetReportPackageStatus";
        }

        pkg.Status = target;
        pkg.ModifiedBy = user;
        pkg.ModifiedAt = now;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(action, "ReportPackage", pkg.Id.ToString(),
            beforeValue: current.ToString(), afterValue: target.ToString(),
            companyId: request.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);

        return ReportPackageMapper.ToDto(pkg);
    }

    /// <summary>เก็บ snapshot ชื่อบริษัท + ยอดสรุปงบ ณ ตอน finalize (ไม่ให้ finalize ล้มถ้าคำนวณงบไม่ได้)</summary>
    private async Task CaptureSnapshotAsync(Domain.Entities.ReportPackage pkg, CancellationToken ct)
    {
        var company = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == pkg.ClientCompanyId, ct);
        if (company is not null)
        {
            pkg.SnapshotCompanyName = company.LegalName;
            pkg.SnapshotTaxId = company.TaxId;
            pkg.SnapshotBranchCode = company.BranchCode;
            pkg.SnapshotAddress = company.Address;
        }

        try
        {
            var bs = await mediator.Send(new GetBalanceSheetQuery(pkg.ClientCompanyId, pkg.FiscalYear), ct);
            pkg.TotalAssets = bs.TotalAssets;
            pkg.TotalLiabilities = bs.TotalLiabilities;
            pkg.TotalEquity = bs.TotalEquity;
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { /* งบดุลคำนวณไม่ได้ — ข้าม totals */ }

        try
        {
            var pl = await mediator.Send(new GetProfitLossQuery(pkg.ClientCompanyId, pkg.FiscalYear), ct);
            pkg.TotalRevenue = pl.TotalIncome;
            pkg.NetProfit = pl.NetProfit;
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { /* P&L คำนวณไม่ได้ — ข้าม */ }
    }

    private static ValidationException Invalid(string msg)
        => new(new[] { new FluentValidation.Results.ValidationFailure("Status", msg) });
}
