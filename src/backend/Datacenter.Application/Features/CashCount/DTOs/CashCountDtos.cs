namespace Datacenter.Application.Features.CashCount.DTOs;

// ── รายการ ──────────────────────────────────────────────────────────────────────
public record CashCountListItemDto(
    int Id, int FiscalYear, DateTime CountDate, string? Reference,
    int CashAccountId, string? CashAccountCode, decimal CountedTotal, bool IsActive);

// ── line ───────────────────────────────────────────────────────────────────────
public record CashCountLineInput(decimal Denomination, int Quantity);
public record CashCountLineDto(decimal Denomination, int Quantity, decimal Amount);

/// <summary>ฟิลด์ที่แก้ไขได้ (ใช้ทั้ง create/update)</summary>
public record CashCountInput(
    int FiscalYear, DateTime CountDate, string? Reference, int CashAccountId,
    string? Notes, string? AttachmentPath, bool IsActive,
    IReadOnlyList<CashCountLineInput> Lines);

/// <summary>ใบตรวจนับเต็ม (header + บัญชี + รายการนับ + ยอดรวม)</summary>
public record CashCountDto(
    int Id, int ClientCompanyId, int FiscalYear, DateTime CountDate, string? Reference,
    int CashAccountId, string? CashAccountCode, string? CashAccountName,
    string? Notes, string? AttachmentPath, bool IsActive,
    decimal CountedTotal, IReadOnlyList<CashCountLineDto> Lines);

// ── กระดาษทำการ + เทียบ GL ───────────────────────────────────────────────────────
public record CashCountWorkpaperRowDto(
    int Id, DateTime CountDate, string? Reference,
    int CashAccountId, string? CashAccountCode, string? CashAccountName, decimal CountedTotal);

/// <summary>เทียบยอดนับจริง (รวมตามบัญชีเงินสด) กับยอด GL บัญชีนั้น (debit − credit) สะสมถึงสิ้นปีงบ</summary>
public record CashCountGlCompareDto(
    int AccountId, string AccountCode, string AccountName,
    decimal CountedTotal,   // ยอดนับจริงรวมของบัญชีนี้
    decimal GlClosing,      // ยอด GL สะสมถึงสิ้นปีงบ (debit − credit)
    decimal Diff);          // นับจริง − GL (>0 = เกิน, <0 = ขาด)

public record CashCountWorkpaperDto(
    int ClientCompanyId, string ClientCode, string ClientName, int FiscalYear,
    IReadOnlyList<CashCountWorkpaperRowDto> Rows,
    IReadOnlyList<CashCountGlCompareDto> GlComparison)
{
    public decimal TotalCounted => Rows.Sum(r => r.CountedTotal);
    public bool HasDifference => GlComparison.Any(g => Math.Round(g.Diff, 2) != 0);
}
