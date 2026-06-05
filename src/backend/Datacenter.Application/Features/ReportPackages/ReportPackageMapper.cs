using Datacenter.Application.Features.ReportPackages.DTOs;
using Datacenter.Domain.Entities;

namespace Datacenter.Application.Features.ReportPackages;

public static class ReportPackageMapper
{
    public static ReportPackageDto ToDto(ReportPackage p) => new(
        p.Id, p.ClientCompanyId, p.FiscalYear, p.Version, (int)p.Status, p.Title, p.Note,
        p.SnapshotCompanyName, p.SnapshotTaxId, p.SnapshotBranchCode, p.SnapshotAddress,
        p.TotalAssets, p.TotalLiabilities, p.TotalEquity, p.TotalRevenue, p.NetProfit,
        p.FinalizedAt, p.FinalizedBy, p.LockedAt, p.LockedBy, p.CreatedAt, p.CreatedBy);
}
