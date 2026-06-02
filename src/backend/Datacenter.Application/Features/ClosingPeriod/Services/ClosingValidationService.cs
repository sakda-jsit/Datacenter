using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.ClosingPeriod.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ClosingPeriod.Services;

/// <summary>
/// รวมตรรกะตรวจสอบความพร้อมก่อนปิดงวดบัญชีไว้ที่เดียว ใช้ทั้งใน query (preview)
/// และ command (บังคับตรวจก่อนปิดจริง) ตาม business rule:
/// "Closing validation must check VAT, AR/AP, bank reconciliation, and GL balance."
///
/// หมายเหตุ: ปัจจุบันมีเฉพาะข้อมูล GL (JournalEntry) จึงตรวจ GL ได้จริง ส่วน VAT/AR-AP/Bank
/// ยังไม่เปิดใช้งาน จึงรายงานเป็น severity "Info" (ข้าม) ไม่บล็อกการปิด เมื่อโมดูลเหล่านั้นพร้อม
/// ให้เพิ่มการตรวจจริงในที่นี้โดยไม่ต้องแก้ handler ที่เรียกใช้
/// </summary>
public static class ClosingValidationService
{
    private const decimal BalanceTolerance = 0.01m;

    public static async Task<List<ClosingValidationItemDto>> ValidateAsync(
        IApplicationDbContext db, int clientCompanyId, int year, int month, CancellationToken ct)
    {
        var periodStart = new DateTime(year, month, 1);
        var periodEnd = periodStart.AddMonths(1);

        var monthLines = await db.JournalEntryLines
            .AsNoTracking()
            .Where(l => l.JournalEntry.ClientCompanyId == clientCompanyId
                     && l.JournalEntry.JournalDate >= periodStart
                     && l.JournalEntry.JournalDate < periodEnd)
            .Select(l => new { l.DebitAmount, l.CreditAmount })
            .ToListAsync(ct);

        var items = new List<ClosingValidationItemDto>();

        // 1) มีข้อมูล GL ในงวดหรือไม่
        bool hasData = monthLines.Count > 0;
        items.Add(new ClosingValidationItemDto(
            Code: "GL_HAS_DATA",
            Label: "มีรายการบัญชีในงวด",
            Severity: "Warning",
            Passed: hasData,
            Detail: hasData ? $"พบ {monthLines.Count} บรรทัดรายการ" : "ยังไม่มีรายการบัญชีในงวดนี้"));

        // 2) GL balanced (เดบิตรวม = เครดิตรวม)
        decimal totalDebit = monthLines.Sum(l => l.DebitAmount);
        decimal totalCredit = monthLines.Sum(l => l.CreditAmount);
        decimal diff = totalDebit - totalCredit;
        bool balanced = Math.Abs(diff) <= BalanceTolerance;
        items.Add(new ClosingValidationItemDto(
            Code: "GL_BALANCED",
            Label: "เดบิตรวมเท่ากับเครดิตรวม (GL balanced)",
            Severity: "Error",
            Passed: balanced,
            Detail: balanced
                ? $"เดบิต {totalDebit:N2} = เครดิต {totalCredit:N2}"
                : $"ผลต่าง {diff:N2} (เดบิต {totalDebit:N2} / เครดิต {totalCredit:N2})"));

        // 3-5) โมดูลที่ยังไม่เปิดใช้งาน — รายงานเป็น Info (ข้าม) ไม่บล็อกการปิด
        items.Add(SkippedCheck("VAT_RECONCILED", "กระทบยอดภาษีมูลค่าเพิ่มกับ GL"));
        items.Add(SkippedCheck("ARAP_RECONCILED", "กระทบยอดลูกหนี้/เจ้าหนี้กับ GL"));
        items.Add(SkippedCheck("BANK_RECONCILED", "กระทบยอดธนาคารกับ GL"));

        return items;
    }

    /// <summary>ปิดงวดได้เมื่อไม่มี item ที่เป็น Error และยังไม่ผ่าน</summary>
    public static bool CanClose(IEnumerable<ClosingValidationItemDto> items)
        => items.All(i => i.Severity != "Error" || i.Passed);

    private static ClosingValidationItemDto SkippedCheck(string code, string label)
        => new(code, label, "Info", true, "ยังไม่เปิดใช้งานโมดูลนี้ — ข้ามการตรวจ");
}
