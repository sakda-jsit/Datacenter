using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.ComplianceCalendar.Services;

/// <summary>
/// กติกาว่าด้วยวันครบกำหนด นับจาก "วันสิ้นงวด" ของงานนั้น.
/// ใช้ได้ 2 แบบ — เลือกอย่างใดอย่างหนึ่ง:
/// <list type="bullet">
/// <item><b>DaysAfter &gt; 0</b> — นับเป็นจำนวนวันตรง ๆ (เช่น ภ.ง.ด.50 = 150 วันนับแต่วันสิ้นรอบบัญชี)</item>
/// <item><b>MonthsAfter</b> + <b>Day</b> — เดือนที่ N หลังสิ้นงวด วันที่ Day (Day ≤ 0 = วันสุดท้ายของเดือนนั้น)</item>
/// </list>
/// </summary>
public readonly record struct ComplianceDueRule(int MonthsAfter, int Day, int DaysAfter = 0);

/// <summary>
/// ทะเบียนกลางของงานในปฏิทินงาน — ชื่อ, รอบ, วันครบกำหนดเริ่มต้น, ต้องแนบหลักฐานหรือไม่
/// รวมไว้ที่เดียวเพื่อไม่ให้ข้อมูลของงานประเภทเดียวกันกระจายแล้วเพี้ยนกัน
/// </summary>
public static class ComplianceTaskCatalog
{
    /// <param name="CalendarYearBased">
    /// งานที่ผูกกับ <b>ปีปฏิทิน</b> ไม่ใช่รอบบัญชีของบริษัท (เช่น ภ.ง.ด.1ก, กท.20ก ซึ่งสรุปค่าจ้าง ม.ค.–ธ.ค.)
    /// — งวดจะสิ้นสุด 31 ธ.ค. เสมอ ต่อให้บริษัทใช้รอบบัญชีอื่น
    /// </param>
    public record Entry(
        ComplianceTaskType Type,
        string Name,
        ComplianceCycle Cycle,
        ComplianceDueRule Due,
        bool RequireEvidence,
        bool CalendarYearBased = false);

    private static readonly Entry[] Entries =
    [
        // ── รายเดือน (ครบกำหนดเดือนถัดไป) ───────────────────────────────────
        new(ComplianceTaskType.PP30,   "ภ.พ.30 (VAT)",                        ComplianceCycle.Monthly, new(1, 15), true),
        new(ComplianceTaskType.PND1,   "ภ.ง.ด.1 (ภาษีหัก ณ ที่จ่าย พนักงาน)",   ComplianceCycle.Monthly, new(1, 15), true),
        new(ComplianceTaskType.PND3,   "ภ.ง.ด.3 (ภาษีหัก ณ ที่จ่าย บุคคลธรรมดา)", ComplianceCycle.Monthly, new(1, 7),  true),
        new(ComplianceTaskType.PND53,  "ภ.ง.ด.53 (ภาษีหัก ณ ที่จ่าย นิติบุคคล)",  ComplianceCycle.Monthly, new(1, 7),  true),
        new(ComplianceTaskType.SSO,    "ประกันสังคม",                          ComplianceCycle.Monthly, new(1, 15), true),
        new(ComplianceTaskType.MonthlyClosing, "ปิดบัญชีประจำเดือน",            ComplianceCycle.Monthly, new(1, 0),  false),

        // ── ครึ่งปีบัญชี ────────────────────────────────────────────────────
        // ภ.ง.ด.51 ยื่นภายใน 2 เดือนนับแต่วันสุดท้ายของ 6 เดือนแรกของรอบบัญชี
        new(ComplianceTaskType.PND51,  "ภ.ง.ด.51 (ภาษีนิติบุคคลครึ่งปี)",       ComplianceCycle.HalfYear, new(2, 0), true),

        // ── รายปี ──────────────────────────────────────────────────────────
        // ภ.ง.ด.50 ยื่นภายใน 150 วันนับแต่วันสุดท้ายของรอบบัญชี
        new(ComplianceTaskType.PND50,  "ภ.ง.ด.50 (ภาษีนิติบุคคลประจำปี)",       ComplianceCycle.Yearly, new(5, 0, DaysAfter: 150), true),
        // งบการเงิน: ประชุมอนุมัติภายใน 4 เดือน + นำส่ง DBD ภายใน 1 เดือนหลังประชุม
        new(ComplianceTaskType.FinancialStatement, "งบการเงิน + สบช.3 (นำส่ง DBD)", ComplianceCycle.Yearly, new(5, 0), true),
        // ภ.ง.ด.1ก / กท.20ก สรุปค่าจ้างตามปีปฏิทิน (ม.ค.–ธ.ค.) ครบกำหนดสิ้น ก.พ. — ไม่ผูกกับรอบบัญชีบริษัท
        new(ComplianceTaskType.PND1K,  "ภ.ง.ด.1ก (สรุปภาษีพนักงานประจำปี)",     ComplianceCycle.Yearly, new(2, 0), true, CalendarYearBased: true),
        new(ComplianceTaskType.KT20,   "กท.20ก (รายงานค่าจ้างประจำปี)",         ComplianceCycle.Yearly, new(2, 0), true, CalendarYearBased: true),
    ];

    private static readonly Dictionary<ComplianceTaskType, Entry> ByType = Entries.ToDictionary(e => e.Type);

    /// <summary>ทุกประเภทงาน เรียงตามรอบ (รายเดือน → ครึ่งปี → รายปี)</summary>
    public static IReadOnlyList<Entry> All => Entries;

    public static Entry Get(ComplianceTaskType type)
        => ByType.TryGetValue(type, out var e)
            ? e
            : new Entry(type, type.ToString(), ComplianceCycle.Monthly, new(1, 15), true);

    public static string Name(ComplianceTaskType type) => Get(type).Name;
    public static ComplianceCycle Cycle(ComplianceTaskType type) => Get(type).Cycle;

    public static string CycleName(ComplianceCycle cycle) => cycle switch
    {
        ComplianceCycle.Monthly  => "รายเดือน",
        ComplianceCycle.HalfYear => "ครึ่งปี",
        ComplianceCycle.Yearly   => "รายปี",
        _ => cycle.ToString(),
    };

    /// <summary>
    /// เดือนที่งวดของรอบนั้น "สิ้นสุด" — ใช้เป็นคีย์ Month ของงาน และเป็นฐานคำนวณวันครบกำหนด.
    /// รายเดือน = ทุกเดือน (คืน null = ไม่ผูกกับเดือนใดเดือนหนึ่ง)
    /// </summary>
    public static int? PeriodEndMonth(ComplianceTaskType type, int fiscalYearStartMonth)
    {
        var entry = Get(type);
        // งานที่ผูกกับปีปฏิทินคิดเสมือนรอบบัญชีเริ่ม ม.ค. ไม่ว่าบริษัทจะใช้รอบไหน
        int start = entry.CalendarYearBased ? 1
                  : fiscalYearStartMonth is >= 1 and <= 12 ? fiscalYearStartMonth : 1;
        return entry.Cycle switch
        {
            ComplianceCycle.HalfYear => ((start + 4) % 12) + 1,   // เดือนที่ 6 ของรอบ
            ComplianceCycle.Yearly   => ((start + 10) % 12) + 1,  // เดือนสุดท้ายของรอบ
            _ => null,                                            // รายเดือน = ทุกเดือน
        };
    }

    /// <summary>งานประเภทนี้ต้องถูกสร้างในเดือนที่กำลัง generate หรือไม่</summary>
    public static bool OccursIn(ComplianceTaskType type, int month, int fiscalYearStartMonth)
    {
        var end = PeriodEndMonth(type, fiscalYearStartMonth);
        return end is null || end == month;
    }

    /// <summary>คำอธิบายงวดของงานหนึ่ง เช่น "ม.ค. 2026", "ครึ่งปีแรก 2026", "ปีบัญชี 2026"</summary>
    public static string PeriodLabel(ComplianceTaskType type, int year, int month)
    {
        var entry = Get(type);
        return entry.Cycle switch
        {
            ComplianceCycle.HalfYear => $"ครึ่งปีแรก {year}",
            ComplianceCycle.Yearly   => entry.CalendarYearBased ? $"ปีภาษี {year}" : $"ปีบัญชี {year}",
            _ => $"{ThaiMonths[month - 1]} {year}",
        };
    }

    /// <summary>
    /// ประเภทนี้กำลังใช้กติกา "นับเป็นจำนวนวัน" อยู่หรือไม่ (จริงเมื่อยังไม่มีการตั้งทับ)
    /// — ใช้บอก UI ว่าช่อง "วันที่" ไม่มีผลจนกว่าจะตั้งค่าเอง
    /// </summary>
    public static bool UsesDaysAfterRule(ComplianceTaskType type, int? dueDay, int? dueMonthsAfter)
        => Get(type).Due.DaysAfter > 0 && dueDay is null && dueMonthsAfter is null;

    /// <summary>
    /// อธิบายวันครบกำหนดที่ใช้จริงเป็นภาษาคน — ใช้แสดงใต้ช่องตั้งค่า
    /// (dueDay / dueMonthsAfter = ค่าที่ตั้งทับไว้; null = ใช้ค่าเริ่มต้นของประเภทงาน)
    /// </summary>
    public static string DueDescription(ComplianceTaskType type, int? dueDay, int? dueMonthsAfter)
    {
        var entry = Get(type);
        var rule = entry.Due;
        string periodEnd = entry.Cycle switch
        {
            ComplianceCycle.HalfYear => "สิ้นครึ่งปีบัญชี",
            ComplianceCycle.Yearly   => entry.CalendarYearBased ? "สิ้นปีปฏิทิน" : "สิ้นรอบบัญชี",
            _ => "สิ้นเดือน",
        };

        // กติกาแบบนับวัน (ภ.ง.ด.50) ใช้ได้ต่อเมื่อไม่มีการตั้งทับ
        if (rule.DaysAfter > 0 && dueDay is null && dueMonthsAfter is null)
            return $"{rule.DaysAfter} วันหลัง{periodEnd}";

        int months = dueMonthsAfter ?? rule.MonthsAfter;
        int day = dueDay ?? rule.Day;

        string when = months == 0 ? $"ภายในเดือนที่{periodEnd}"
                    : months == 1 ? "ของเดือนถัดไป"
                    : $"ของเดือนที่ {months} หลัง{periodEnd}";

        return day <= 0 ? $"วันสุดท้าย{(months == 1 ? "ของเดือนถัดไป" : when)}" : $"วันที่ {day} {when}";
    }

    private static readonly string[] ThaiMonths =
        ["ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค."];
}
