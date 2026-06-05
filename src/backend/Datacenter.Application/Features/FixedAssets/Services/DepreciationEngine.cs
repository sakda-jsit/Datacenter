using Datacenter.Application.Features.FixedAssets.DTOs;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.FixedAssets.Services;

/// <summary>
/// คำนวณค่าเสื่อมราคาแบบเส้นตรง (straight-line) แบบ pure — ไม่แตะ DB (req v11 docs/14).
///
/// หลักการ (ตรงกับ workbook FA: DATEDIF อายุถือครอง, ROUND, ค่าเสื่อมสะสม, มูลค่าสุทธิ):
/// - ค่าเสื่อมต่อปีเต็ม = round(ราคาทุน × อัตรา%/ปี, 2)
/// - ฐานคิดค่าเสื่อม (depreciable base) = ราคาทุน − มูลค่าซาก; ค่าเสื่อมสะสมไม่เกินฐานนี้
/// - ปีแรก/ปีจำหน่าย prorate ตามจำนวนวันใช้งานจริงในปีปฏิทินนั้น (วัน/วันทั้งปี)
/// - หยุดคิดค่าเสื่อมหลังวันที่จำหน่าย/ขาย/ตัดจำหน่าย (Status ≠ Active)
/// - อัตรา ≤ 0 (เช่น ที่ดิน) → ไม่มีค่าเสื่อม
///
/// คำนวณแยก 2 ชุด (บัญชี/ภาษี) โดยส่งอัตราต่างกันเข้ามา — ห้ามรวมยอดข้ามชุด.
/// </summary>
public static class DepreciationEngine
{
    private const int MaxYears = 200;

    /// <summary>สร้างตารางค่าเสื่อมรายปี ตั้งแต่ปีที่ได้มา จนตัดหมด/จำหน่าย</summary>
    public static IReadOnlyList<DepreciationYearDto> BuildSchedule(FixedAsset a, decimal ratePct)
    {
        var rows = new List<DepreciationYearDto>();

        var cost = a.Cost;
        var depreciableBase = Math.Max(cost - a.SalvageValue, 0m);
        if (depreciableBase <= 0m || ratePct <= 0m) return rows;

        var annual = Math.Round(cost * ratePct / 100m, 2);
        if (annual <= 0m) return rows;

        // ปีสุดท้ายที่คิดค่าเสื่อม: จำกัดด้วยวันจำหน่ายถ้า Status ≠ Active
        var disposed = a.Status != FixedAssetStatus.Active && a.DisposalDate.HasValue;
        var stopDate = disposed ? a.DisposalDate!.Value : DateTime.MaxValue;

        // ค่าเสื่อมสะสมยกมา (จาก Express ACCMBF): เริ่มสะสมที่ต้นปี BroughtForwardYear ด้วยยอดนี้
        // (cap ไม่เกินฐาน) เพื่อให้ตรง Express เป๊ะ. ไม่มียอดยกมา → เริ่มจากปีที่ได้มา ยอด 0.
        var hasBf = a.BroughtForwardYear > 0 && a.AccumulatedBroughtForward > 0m;
        var accumulated = hasBf ? Math.Min(a.AccumulatedBroughtForward, depreciableBase) : 0m;
        var year = hasBf ? a.BroughtForwardYear : a.AcquireDate.Year;

        for (var i = 0; i < MaxYears && accumulated < depreciableBase; i++, year++)
        {
            var opening = accumulated;
            var charge = ChargeForYear(a.AcquireDate, stopDate, disposed, year, annual);

            // cap ไม่ให้สะสมเกินฐาน + งวดที่ตัดถึงฐานพอดี absorb เศษ
            var remaining = Math.Round(depreciableBase - accumulated, 2);
            if (charge > remaining) charge = remaining;
            if (charge < 0m) charge = 0m;

            accumulated = Math.Round(accumulated + charge, 2);

            rows.Add(new DepreciationYearDto(
                year, opening, charge, accumulated, Math.Round(cost - accumulated, 2)));

            // จำหน่ายแล้ว: ปีที่มีวันจำหน่ายเป็นปีสุดท้าย
            if (disposed && year >= stopDate.Year) break;
        }

        return rows;
    }

    /// <summary>ค่าเสื่อมงวดของปีปฏิทิน <paramref name="year"/> (prorate ตามวันใช้งานจริง)</summary>
    private static decimal ChargeForYear(DateTime acquireDate, DateTime stopDate, bool disposed, int year, decimal annual)
    {
        var serviceStart = acquireDate.Year == year ? acquireDate : new DateTime(year, 1, 1);
        var serviceEnd = new DateTime(year, 12, 31);

        if (disposed)
        {
            if (stopDate < serviceStart) return 0m;            // จำหน่ายก่อนปีนี้
            if (stopDate < serviceEnd) serviceEnd = stopDate;  // จำหน่ายระหว่างปี → ถึงวันจำหน่าย
        }

        var days = (serviceEnd.Date - serviceStart.Date).Days + 1; // นับวันแบบรวมปลาย
        if (days <= 0) return 0m;

        var daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;
        if (days >= daysInYear) return annual;                  // ทั้งปี

        return Math.Round(annual * days / daysInYear, 2);
    }

    /// <summary>ค่าเสื่อมสะสมตั้งต้น (จากยอดยกมา ACCMBF, cap ไม่เกินฐาน); ไม่มียอดยกมา = 0</summary>
    private static decimal StartingAccum(FixedAsset a, decimal depreciableBase)
        => a.BroughtForwardYear > 0 && a.AccumulatedBroughtForward > 0m
            ? Math.Min(a.AccumulatedBroughtForward, depreciableBase)
            : 0m;

    /// <summary>ยอดค่าเสื่อม ณ สิ้นปีงบที่ขอ (ชุดที่ส่งอัตราเข้ามา)</summary>
    public static DepreciationAsOfDto AsOf(FixedAsset a, decimal ratePct, int fiscalYear)
    {
        var schedule = BuildSchedule(a, ratePct);
        var depreciableBase = Math.Max(a.Cost - a.SalvageValue, 0m);

        if (schedule.Count == 0)
        {
            // ไม่มีปีที่ต้องคิดเพิ่ม: (ก) อัตรา 0/ที่ดิน → ยอดสะสม 0  หรือ (ข) ตัดหมดแล้วตั้งแต่ยอดยกมา → คงยอด ACCMBF
            var bf = StartingAccum(a, depreciableBase);
            return new DepreciationAsOfDto(
                bf, 0m, bf, Math.Round(a.Cost - bf, 2),
                depreciableBase > 0m && Math.Round(bf, 2) >= Math.Round(depreciableBase, 2));
        }

        var first = schedule[0];
        var last = schedule[^1];

        if (fiscalYear < first.Year)
            return new DepreciationAsOfDto(0m, 0m, 0m, a.Cost, false);

        var row = schedule.FirstOrDefault(r => r.Year == fiscalYear);
        if (row is not null)
            return new DepreciationAsOfDto(
                row.OpeningAccumulated, row.Charge, row.ClosingAccumulated, row.NetBookValue,
                Math.Round(row.ClosingAccumulated, 2) >= Math.Round(depreciableBase, 2));

        // ปีงบหลังตัดหมด/จำหน่าย — คงยอดสะสมสุดท้าย ไม่มีค่าเสื่อมเพิ่ม
        return new DepreciationAsOfDto(
            last.ClosingAccumulated, 0m, last.ClosingAccumulated, last.NetBookValue,
            Math.Round(last.ClosingAccumulated, 2) >= Math.Round(depreciableBase, 2));
    }

    /// <summary>กำไร/ขาดทุนจากการจำหน่าย (ชุดบัญชี) = ราคาขาย − มูลค่าสุทธิ ณ วันจำหน่าย</summary>
    public static DisposalResultDto? Disposal(FixedAsset a)
    {
        if (a.Status == FixedAssetStatus.Active || !a.DisposalDate.HasValue) return null;

        var schedule = BuildSchedule(a, a.BookRatePct);
        var depreciableBase = Math.Max(a.Cost - a.SalvageValue, 0m);
        // ค่าเสื่อมสะสม ณ วันจำหน่าย = closing ของปีจำหน่าย; ถ้า schedule ว่าง (ตัดหมดแล้วตั้งแต่ยอดยกมา)
        // → ใช้ยอดยกมา (ACCMBF) แทน 0 มิฉะนั้นมูลค่าสุทธิจะผิดเป็นราคาทุนเต็ม
        var disposalYearRow = schedule.LastOrDefault(r => r.Year <= a.DisposalDate.Value.Year);
        var accumAtDisposal = disposalYearRow?.ClosingAccumulated ?? StartingAccum(a, depreciableBase);
        var nbv = Math.Round(a.Cost - accumAtDisposal, 2);
        var proceeds = a.DisposalProceeds ?? 0m;

        return new DisposalResultDto(
            a.DisposalDate.Value, a.Status, proceeds, nbv, Math.Round(proceeds - nbv, 2));
    }
}
