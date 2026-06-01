using Datacenter.Application.Features.Import.DTOs;

namespace Datacenter.Application.Common.Interfaces;

/// <summary>
/// Adapter interface for reading Express Accounting DBF files.
/// Concrete implementation lives in Infrastructure to keep Domain/Application clean.
/// Path pattern: {BasePath}\{ClientCode}\ — reads ISINFO, GLACC, GLBAL tables.
/// </summary>
public interface IExpressDbfAdapter
{
    /// <summary>Returns company info from ISINFO table (single record).</summary>
    Task<ExpressCompanyInfoDto> ReadCompanyInfoAsync(string companyFolderPath, CancellationToken ct = default);

    /// <summary>Returns all account rows from GLACC table.</summary>
    Task<IReadOnlyList<ExpressAccountRowDto>> ReadAccountsAsync(string companyFolderPath, CancellationToken ct = default);

    /// <summary>
    /// Returns trial balance rows from GLBAL for all three period sets (LY/CUR/NY).
    /// Each row carries aggregated debit/credit per period set, not monthly breakdown.
    /// </summary>
    Task<IReadOnlyList<ExpressTrialBalanceRowDto>> ReadTrialBalanceAsync(string companyFolderPath, CancellationToken ct = default);

    /// <summary>Returns true when the folder contains ISINFO.DBF (case-insensitive).</summary>
    Task<bool> FolderIsValidAsync(string companyFolderPath, CancellationToken ct = default);

    /// <summary>
    /// อ่านทะเบียนข้อมูลบริษัทจาก {basePath}\secure\sccomp.dbf (รายการดิบ ยังไม่กรอง)
    /// ใช้ ExpressDatasetFilter เพื่อคัดเฉพาะบริษัทปัจจุบัน
    /// </summary>
    Task<IReadOnlyList<ExpressDatasetDto>> ReadCompanyRegistryAsync(string basePath, CancellationToken ct = default);
}
