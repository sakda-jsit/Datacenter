namespace Datacenter.Application.Features.Vat.DTOs;

/// <summary>สรุปภาษีมูลค่าเพิ่มหนึ่งเดือน (หนึ่งงวด ภ.พ.30)</summary>
public record VatMonthlyDto(
    int Month,
    decimal OutputBase,        // ยอดขายที่ต้องเสียภาษี
    decimal OutputVat,         // ภาษีขาย
    decimal OutputZeroRated,   // ยอดขายอัตรา 0%
    int OutputCount,
    decimal InputBase,         // ยอดซื้อที่มีสิทธิ์ขอคืน
    decimal InputVat,          // ภาษีซื้อ
    int InputCount)
{
    /// <summary>ภาษีที่ต้องชำระ (&gt;0) หรือ ชำระเกิน/ยกไป (&lt;0) = ภาษีขาย − ภาษีซื้อ</summary>
    public decimal NetVat => Math.Round(OutputVat - InputVat, 2);
}

/// <summary>
/// ยอด ภ.พ.30 ของหนึ่งสาขา (group ตาม DEPCOD ใน ISVAT) — ใช้ทั้งแสดงผลและสร้างไฟล์โอนย้าย.
/// blank DEPCOD ถูกรวมเข้าสำนักงานใหญ่ (HO00 → สาขา 00000) ตามมติผู้ใช้.
/// </summary>
public record Pp30BranchRow(
    /// <summary>รหัสแผนก/สาขาใน Express ที่ group มา (เช่น HO00, BR01); "(รวม)" สำหรับ blank+HO</summary>
    string DepartmentCode,
    /// <summary>เลขสาขาตามแบบ RD (สำนักงานใหญ่ = 00000, สาขา = 00001…) จากกฎ DEPCOD</summary>
    string BranchNo,
    bool IsHeadOffice,
    decimal TotalSales,         // ยอดขายในเดือนนี้ (= ที่ต้องเสียภาษี + อัตรา 0)
    decimal ZeroRatedSales,     // ยอดขายอัตรา 0
    decimal ExemptSales,        // ยอดขายยกเว้น (ISVAT ไม่เก็บ = 0)
    decimal EligiblePurchase,   // ยอดซื้อที่มีสิทธิ์
    decimal OutputVat,          // ภาษีขาย
    decimal InputVat);          // ภาษีซื้อ

/// <summary>ยอด ภ.พ.30 แยกตามสาขา ของหนึ่งงวดเดือน (สำหรับยื่นรวมกัน/ไฟล์โอนย้าย).</summary>
public record Pp30BranchesDto(
    string CompanyName,
    string TaxId,
    int Year,
    int Month,
    bool IsMultiBranch,
    IReadOnlyList<Pp30BranchRow> Branches);

/// <summary>หนึ่งรหัสแผนก/สาขา (DEPCOD) ที่พบในข้อมูล VAT + การแมพเลขสาขา RD (ถ้ามี).</summary>
public record VatBranchMappingDto(
    /// <summary>รหัส DEPCOD ดิบ ("" = ไม่ระบุ)</summary>
    string DepartmentCode,
    /// <summary>ป้ายแสดง (เช่น HO00, BR01, "(ไม่ระบุ)")</summary>
    string DisplayCode,
    /// <summary>เลขสาขา RD ที่ใช้ (จาก mapping ถ้ามี ไม่งั้นจากกฎอัตโนมัติ)</summary>
    string RdBranchNo,
    bool IsHeadOffice,
    string? BranchName,
    /// <summary>มี mapping บันทึกไว้หรือยัง (false = ใช้ค่าจากกฎอัตโนมัติ)</summary>
    bool IsMapped,
    int EntryCount);

public record VatBranchMappingInput(string DepartmentCode, string RdBranchNo, bool IsHeadOffice, string? BranchName);

/// <summary>รายงาน ภ.พ.30 รายเดือนตลอดปีปฏิทิน + ยอดรวมทั้งปี</summary>
public record VatReportDto(
    int ClientCompanyId,
    string ClientName,
    int Year,
    IReadOnlyList<VatMonthlyDto> Months,
    DateTime? DataAsOf = null)   // เวลานำเข้า VAT ล่าสุด (snapshot — ไม่ใช่ real-time)
{
    public decimal TotalOutputBase => Months.Sum(m => m.OutputBase);
    public decimal TotalOutputVat => Months.Sum(m => m.OutputVat);
    public decimal TotalOutputZeroRated => Months.Sum(m => m.OutputZeroRated);
    public decimal TotalInputBase => Months.Sum(m => m.InputBase);
    public decimal TotalInputVat => Months.Sum(m => m.InputVat);
    public decimal TotalNetVat => Math.Round(TotalOutputVat - TotalInputVat, 2);
    public int TotalOutputCount => Months.Sum(m => m.OutputCount);
    public int TotalInputCount => Months.Sum(m => m.InputCount);
}

// ── รายงานภาษีขาย / รายงานภาษีซื้อ (สำหรับยื่น ภ.พ.30 + เก็บหลักฐาน) ──────────────
public record VatTaxReportRow(
    int Seq, DateTime? Date, string DocNo, string? Name, string? TaxId,
    decimal BaseAmount, decimal VatAmount, decimal ZeroRatedAmount);

/// <summary>รายงานภาษีขาย (VatType=1) หรือรายงานภาษีซื้อ (VatType=2) หนึ่งงวด</summary>
public record VatTaxReportDto(
    string CompanyName, string TaxId, int Year, int Month, int VatType,
    IReadOnlyList<VatTaxReportRow> Rows,
    decimal TotalBase, decimal TotalVat, decimal TotalZeroRated);

/// <summary>หนึ่งรายการในรายละเอียดภาษีซื้อ/ขาย</summary>
public record VatEntryListItemDto(
    int Id,
    int VatType,               // 1=ขาย(Output), 2=ซื้อ(Input)
    DateTime TaxPeriod,
    DateTime? DocumentDate,
    string DocumentNo,
    string? ReferenceNo,
    string? Description,
    string? CounterpartyTaxId,
    string? CounterpartyPrefix,
    decimal BaseAmount,
    decimal VatAmount,
    decimal ZeroRatedAmount,
    bool IsLate);
