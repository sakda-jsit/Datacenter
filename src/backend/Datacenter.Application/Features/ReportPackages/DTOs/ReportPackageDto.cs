namespace Datacenter.Application.Features.ReportPackages.DTOs;

public record ReportPackageDto(
    int Id,
    int ClientCompanyId,
    int FiscalYear,
    int Version,
    int Status,            // 0=Draft,1=Review,2=Final,3=Locked
    string? Title,
    string? Note,
    string? SnapshotCompanyName,
    string? SnapshotTaxId,
    string? SnapshotBranchCode,
    string? SnapshotAddress,
    decimal? TotalAssets,
    decimal? TotalLiabilities,
    decimal? TotalEquity,
    decimal? TotalRevenue,
    decimal? NetProfit,
    DateTime? FinalizedAt,
    string? FinalizedBy,
    DateTime? LockedAt,
    string? LockedBy,
    DateTime CreatedAt,
    string CreatedBy);
