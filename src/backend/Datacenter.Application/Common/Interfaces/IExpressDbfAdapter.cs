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
    /// อ่านนิยามรอบบัญชีจาก ISPRD (1 record): งวดปัจจุบัน 12 งวด + ปีถัดไป 12 งวด
    /// แต่ละงวดมีวันเริ่ม/วันสิ้นงวด และ flag LOCK ('Y' = ปิด/ล็อกแล้ว) — ข้ามงวดที่ไม่มีวันที่
    /// </summary>
    Task<IReadOnlyList<ExpressAccountingPeriodDto>> ReadAccountingPeriodsAsync(string companyFolderPath, CancellationToken ct = default);

    /// <summary>
    /// อ่านทะเบียนข้อมูลบริษัทจาก {basePath}\secure\sccomp.dbf (รายการดิบ ยังไม่กรอง)
    /// ใช้ ExpressDatasetFilter เพื่อคัดเฉพาะบริษัทปัจจุบัน
    /// </summary>
    Task<IReadOnlyList<ExpressDatasetDto>> ReadCompanyRegistryAsync(string basePath, CancellationToken ct = default);

    /// <summary>
    /// อ่านทะเบียนสินทรัพย์ถาวรจาก FAMAS.DBF (ข้ามระเบียนที่ไม่มีรหัส/ราคาทุน ≤ 0).
    /// คืนรายการว่างถ้าไม่มีไฟล์หรือไม่มีข้อมูล (บางบริษัทไม่ใช้โมดูลสินทรัพย์).
    /// </summary>
    Task<IReadOnlyList<ExpressFixedAssetDto>> ReadFixedAssetsAsync(string companyFolderPath, CancellationToken ct = default);

    /// <summary>
    /// อ่านรายงานภาษีมูลค่าเพิ่ม (ภาษีซื้อ/ภาษีขาย) จาก ISVAT.DBF — เฉพาะ VATREC='S'/'P'.
    /// คืนรายการว่างถ้าไม่มีไฟล์ (บริษัทที่ไม่ได้จด VAT).
    /// </summary>
    Task<IReadOnlyList<ExpressVatEntryDto>> ReadVatEntriesAsync(string companyFolderPath, CancellationToken ct = default);

    /// <summary>
    /// อ่านรายการภาษีหัก ณ ที่จ่าย (ภ.ง.ด.3/53) จาก ISTAX.DBF — แตกชุดเงินได้ที่ 2 เป็นรายการแยกเมื่อมีค่า.
    /// คืนรายการว่างถ้าไม่มีไฟล์ (บริษัทที่ไม่มีการหักภาษี).
    /// </summary>
    Task<IReadOnlyList<ExpressWhtEntryDto>> ReadWhtEntriesAsync(string companyFolderPath, CancellationToken ct = default);

    /// <summary>อ่านลูกค้าจาก ARMAS.DBF (คืนว่างถ้าไม่มีไฟล์).</summary>
    Task<IReadOnlyList<ExpressCustomerDto>> ReadCustomersAsync(string companyFolderPath, CancellationToken ct = default);

    /// <summary>อ่านใบแจ้งหนี้ลูกหนี้จาก ARTRN.DBF (เฉพาะ RECTYP='3' = IV; คืนว่างถ้าไม่มีไฟล์).</summary>
    Task<IReadOnlyList<ExpressArInvoiceDto>> ReadArInvoicesAsync(string companyFolderPath, CancellationToken ct = default);

    /// <summary>อ่านผู้ขายจาก APMAS.DBF (คืนว่างถ้าไม่มีไฟล์).</summary>
    Task<IReadOnlyList<ExpressSupplierDto>> ReadSuppliersAsync(string companyFolderPath, CancellationToken ct = default);

    /// <summary>อ่านใบตั้งหนี้เจ้าหนี้จาก APTRN.DBF (เฉพาะ RECTYP='3' = RR; คืนว่างถ้าไม่มีไฟล์).</summary>
    Task<IReadOnlyList<ExpressApInvoiceDto>> ReadApInvoicesAsync(string companyFolderPath, CancellationToken ct = default);
}
