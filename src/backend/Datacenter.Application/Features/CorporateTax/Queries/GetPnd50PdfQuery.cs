using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.CorporateTax.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.CorporateTax.Queries;

/// <summary>
/// สร้างแบบ ภ.ง.ด.50 (PDF) ของ (บริษัท, ปีงบ) — หัว + การคำนวณภาษีจาก TAX engine (เฟส A).
/// </summary>
public record GetPnd50PdfQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<byte[]>, IRequireCompanyAccess;

public class GetPnd50PdfQueryHandler(IApplicationDbContext db, ISender sender, IPnd50PdfService svc)
    : IRequestHandler<GetPnd50PdfQuery, byte[]>
{
    public async Task<byte[]> Handle(GetPnd50PdfQuery req, CancellationToken ct)
    {
        var company = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == req.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", req.ClientCompanyId);

        var startMonth = company.FiscalYearStartMonth is >= 1 and <= 12 ? company.FiscalYearStartMonth : 1;
        var periodStart = new DateTime(req.FiscalYear, startMonth, 1);
        var periodEnd = periodStart.AddYears(1).AddDays(-1);

        var tax = await sender.Send(new GetTaxComputationQuery(req.ClientCompanyId, req.FiscalYear), ct);
        var r = tax.Result;

        // งบกำไรขาดทุน (ใช้ทั้งหน้า 3 + schedule รายการ 8)
        FinancialStatement.DTOs.ProfitLossDto? pl = null;
        if (tax.HasProfitLoss)
        {
            try { pl = await sender.Send(new FinancialStatement.Queries.GetProfitLossQuery(req.ClientCompanyId, req.FiscalYear), ct); }
            catch { /* ไม่มีงบ */ }
        }

        // หน้า 3 (รายการที่ 3): reconcile กับ r — แยกรายได้โดยตรง (I1+I2) กับ รายได้อื่น (I3+I4) ตามแบบจริง
        Pnd50Page3Data? page3 = null;
        if (pl is not null)
        {
            decimal Inc(params string[] codes) => pl.IncomeLines.Where(l => codes.Contains(l.RefCode)).Sum(l => l.Amount);
            var operatingRev = Inc("I1", "I2");
            var otherIncome = pl.TotalIncome - operatingRev; // ที่เหลือ (I3/I4/อื่น) = รายได้อื่น
            var sga = pl.TotalExpenses - pl.CostOfGoods.Amount + Math.Abs(pl.FinanceCost.Amount);
            page3 = new Pnd50Page3Data(
                Revenue: operatingRev, Cogs: pl.CostOfGoods.Amount,
                GrossProfit: operatingRev - pl.CostOfGoods.Amount, OtherIncome: otherIncome, Sga: sga,
                NetAccountingProfit: r.NetProfitBeforeTax, AddBack: r.AddBackTotal, Deduction: r.DeductionTotal,
                AdjustedProfit: r.AdjustedProfit, LossUsed: r.LossUsed, NetTaxableIncome: r.NetTaxableIncome);
        }

        // Schedule รายการ 5 (รายได้อื่น, หน้า 4) / 6 (รายจ่ายอื่น, หน้า 4) / 8 (ขายและบริหาร, หน้า 5):
        // aggregate ยอดบัญชี P&L ตาม mapping (AccountCit50Mappings) → บรรทัด CIT50; แต่ละบรรทัดวาด
        // ② เสียภาษี (PdfX−108) + ③ รวม (PdfX). ① ยกเว้น (BOI) เว้นว่าง. บัญชีไม่ถูก map → catch-all.
        var scheduleCells = new List<Pnd50ScheduleCell>();
        if (pl is not null)
        {
            var maps = await db.AccountCit50Mappings.AsNoTracking()
                .Where(m => m.ClientCompanyId == req.ClientCompanyId)
                .ToDictionaryAsync(m => m.AccountCode, m => m.Cit50LineCode, ct);
            var schedLines = await db.Cit50ScheduleLines.AsNoTracking()
                .Where(l => l.ScheduleNo == 5 || l.ScheduleNo == 6 || l.ScheduleNo == 8).ToListAsync(ct);
            const double col2Offset = 108.0; // ระยะจากคอลัมน์ "รวม" ไป "เสียภาษี" (ขอบกริด)

            void Build(int scheduleNo, IEnumerable<(string Acc, decimal Amt, string? Def)> items)
            {
                var lines = schedLines.Where(l => l.ScheduleNo == scheduleNo).ToList();
                if (lines.Count == 0) return;
                var catchAll = lines.FirstOrDefault(l => l.IsCatchAll)?.Code;
                var sums = lines.ToDictionary(l => l.Code, _ => 0m);
                foreach (var (acc, amt, def) in items)
                {
                    var code = maps.GetValueOrDefault(acc) ?? def ?? catchAll;
                    if (code is not null && sums.ContainsKey(code)) sums[code] += Math.Abs(amt);
                }
                var total = sums.Where(kv => lines.First(l => l.Code == kv.Key) is { IsTotal: false }).Sum(kv => kv.Value);
                foreach (var l in lines)
                {
                    var v = l.IsTotal ? total : sums[l.Code];
                    scheduleCells.Add(new Pnd50ScheduleCell(l.PdfPage, l.PdfX, l.PdfY, l.PdfW, v));            // ③ รวม
                    scheduleCells.Add(new Pnd50ScheduleCell(l.PdfPage, l.PdfX - col2Offset, l.PdfY, l.PdfW, v)); // ② เสียภาษี
                }
            }

            // รายการ 8 (ขายและบริหาร): ค่าใช้จ่ายขาย/บริหาร (X1/X2) — ต้นทุนการเงินไปรายการ 6
            Build(8, pl.ExpenseLines.SelectMany(line =>
                line.Accounts.Select(a => (a.AccountCode, a.NetBalance, (string?)null))));

            // รายการ 5 (รายได้อื่น): รายได้ที่ไม่ใช่จากการขาย/บริการ (ไม่ใช่ I1/I2); ดอกเบี้ยรับ (I3) → ดอกเบี้ยรับ
            Build(5, pl.IncomeLines.Where(line => line.RefCode is not ("I1" or "I2")).SelectMany(line =>
                line.Accounts.Select(a => (a.AccountCode, a.NetBalance, (string?)(line.RefCode == "I3" ? "R5_INT" : null)))));

            // รายการ 6 (รายจ่ายอื่น): ต้นทุนทางการเงิน (FinanceCost) → ต้นทุนทางการเงิน
            Build(6, pl.FinanceCost.Accounts.Select(a => (a.AccountCode, a.NetBalance, (string?)"R6_FIN")));
        }

        // หน้างบดุล (รายการที่ 9): crosswalk บรรทัด ← RefCode ผังงบ (ยอด presentation จาก BS engine)
        // + override การจัดประเภทต่อบัญชี (AccountCit50Mapping รหัส BS_*) — ฟอร์ม ภ.ง.ด.50 แยกบรรทัด
        // ละเอียดกว่างบการเงิน (เช่น ที่ดิน+อาคาร แยกจากทรัพย์สินอื่นซึ่งหักค่าเสื่อม) โดยย้ายยอดภายใน
        // section เดียวกัน → ยอดรวม (TotalAssets/Liabilities) ไม่เปลี่ยน เปลี่ยนแค่บรรทัดที่ลง.
        Pnd50Page7Data? page7 = null;
        try
        {
            var bs = await sender.Send(
                new FinancialStatement.Queries.GetBalanceSheetQuery(req.ClientCompanyId, req.FiscalYear), ct);
            var amt = bs.Assets.Concat(bs.Liabilities).Concat(bs.Equity)
                .GroupBy(l => l.RefCode).ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
            decimal R(params string[] codes) => codes.Sum(c => amt.TryGetValue(c, out var v) ? v : 0m);

            var f = new Dictionary<string, decimal>
            {
                ["Cash"] = R("A1"), ["Ar"] = R("A7"), ["Inventory"] = R("A3"),
                ["OtherCurrentAsset"] = R("A2", "A4", "TXR"), ["LoansToRelated"] = R("A8"),
                ["Ppe"] = R("A5"), ["OtherAssetNet"] = R("A9", "A10"), ["OtherNonCurrentAsset"] = R("A6"),
                ["BankOdShortLoan"] = R("L3"), ["Ap"] = R("L1"), ["CurrentLoan"] = R("L5"),
                ["OtherCurrentLiab"] = R("L2", "TXP"), ["LongTermLoan"] = R("L6"), ["OtherNonCurrentLiab"] = R("L4"),
            };

            var bsOverrides = await db.AccountCit50Mappings.AsNoTracking()
                .Where(m => m.ClientCompanyId == req.ClientCompanyId && m.Cit50LineCode.StartsWith("BS_"))
                .ToDictionaryAsync(m => m.AccountCode, m => m.Cit50LineCode, ct);
            if (bsOverrides.Count > 0)
            {
                var refByAcc = await db.AccountStatementMappings.AsNoTracking()
                    .Where(m => m.ClientCompanyId == req.ClientCompanyId)
                    .ToDictionaryAsync(m => m.AccountCode, m => m.RefCode, ct);
                var nets = await FinancialStatement.Services.FsJournalNets.CumulativeAsync(
                    db, req.ClientCompanyId, req.FiscalYear, ct);

                foreach (var (accCode, bsCode) in bsOverrides)
                {
                    if (!Pnd50BsLines.FieldByCode.TryGetValue(bsCode, out var target)) continue;
                    var def = refByAcc.TryGetValue(accCode, out var rc)
                        ? Pnd50BsLines.FieldByRefCode.GetValueOrDefault(rc) : null;
                    if (def is null || def == target || !f.ContainsKey(def) || !f.ContainsKey(target)) continue;
                    // ย้ายเฉพาะภายใน section เดียวกัน (asset↔asset / liab↔liab) กันสลับเครื่องหมาย/ยอดรวมเพี้ยน
                    if (Pnd50BsLines.IsAssetField(def) != Pnd50BsLines.IsAssetField(target)) continue;
                    var net = nets.GetValueOrDefault(accCode);
                    var pres = Pnd50BsLines.IsAssetField(def) ? net : -net; // presentation amount
                    f[def] -= pres;
                    f[target] += pres;
                }
            }

            var re = R("RE");
            page7 = new Pnd50Page7Data(
                Cash: f["Cash"], Ar: f["Ar"], Inventory: f["Inventory"], OtherCurrentAsset: f["OtherCurrentAsset"],
                LoansToRelated: f["LoansToRelated"], Ppe: f["Ppe"], OtherAssetNet: f["OtherAssetNet"],
                OtherNonCurrentAsset: f["OtherNonCurrentAsset"],
                TotalAssets: bs.TotalAssets,
                BankOdShortLoan: f["BankOdShortLoan"], Ap: f["Ap"], CurrentLoan: f["CurrentLoan"],
                OtherCurrentLiab: f["OtherCurrentLiab"], LongTermLoan: f["LongTermLoan"],
                OtherNonCurrentLiab: f["OtherNonCurrentLiab"],
                TotalLiabilities: bs.TotalLiabilities,
                PaidUpCapital: R("C1"), RetainedEarnings: Math.Abs(re), IsRetainedProfit: re >= 0,
                TotalEquity: bs.TotalEquity, TotalLiabAndEquity: bs.TotalLiabilitiesAndEquity);
        }
        catch { /* ไม่มีงบดุล → ไม่เติมหน้า 7 */ }

        // ผู้ลงนามของรอบปีนี้: override รายปี (CompanyAuditor) ?? ค่าเริ่มต้นบริษัท (ทะเบียน master)
        var year = await db.CompanyAuditors.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientCompanyId == req.ClientCompanyId
                                   && x.FiscalYear == req.FiscalYear, ct);

        var auditorId = year?.AuditorId ?? company.DefaultAuditorId;
        var bookkeeperId = year?.BookkeeperId ?? company.DefaultBookkeeperId;
        var auditorM = auditorId is { } aid
            ? await db.Auditors.AsNoTracking().FirstOrDefaultAsync(a => a.Id == aid, ct) : null;
        var bookkeeperM = bookkeeperId is { } bid
            ? await db.Bookkeepers.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bid, ct) : null;

        // สำนักงานทำบัญชี = โปรไฟล์สำนักงานบัญชีของผู้ใช้ (ค่ากลาง singleton) → ใช้ทุกบริษัท
        var office = await db.OfficeProfiles.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync(ct);

        var isHeadOffice = string.IsNullOrWhiteSpace(company.BranchCode)
            || company.BranchCode.All(c => c == '0');

        // ใช้ที่อยู่แยกช่องที่บันทึกไว้ (แก้ได้); ถ้ายังว่างทั้งหมด fallback แยกจาก Address flat
        bool hasStructured = new[] { company.AddrHouseNo, company.AddrMoo, company.AddrRoad,
            company.AddrSubDistrict, company.AddrDistrict, company.AddrProvince }
            .Any(v => !string.IsNullOrWhiteSpace(v));
        var p = hasStructured ? null : Services.ThaiAddressParser.Parse(company.Address);

        var data = new Pnd50FormData(
            CompanyName: string.IsNullOrWhiteSpace(company.LegalName) ? company.Name : company.LegalName,
            TaxId: company.TaxId,
            IsHeadOffice: isHeadOffice,
            BusinessActivity: company.BusinessActivity,
            IsicCode: company.IsicCode,
            AuditorName: auditorM?.Name,
            AuditorLicenseNo: auditorM?.LicenseNo,
            AuditorTaxId: auditorM?.TaxId,
            BookkeeperName: bookkeeperM?.Name,
            BookkeeperTaxId: bookkeeperM?.TaxId,
            AuditFirmTaxId: auditorM?.AuditFirmTaxId,
            BookkeepingFirmTaxId: office?.TaxId,
            AuditorSignDate: year?.SignDate,
            HouseNo: company.AddrHouseNo ?? p?.HouseNo,
            Moo: company.AddrMoo ?? p?.Moo,
            Soi: company.AddrSoi ?? p?.Soi,
            Road: company.AddrRoad ?? p?.Road,
            SubDistrict: company.AddrSubDistrict ?? p?.SubDistrict,
            District: company.AddrDistrict ?? p?.District,
            Province: company.AddrProvince ?? p?.Province,
            PostalCode: company.PostalCode ?? p?.PostalCode,
            Phone: company.Phone,
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            NetTaxableIncome: r.NetTaxableIncome,
            TaxAmount: r.TaxAmount,
            WhtCredit: r.WhtCredit,
            TotalCredit: r.WhtCredit,
            NetPayable: r.NetPayable,
            RateScheme: tax.RateScheme,
            IsNetProfit: r.AdjustedProfit >= 0,
            BusinessEmail: company.Email,
            BookkeeperEmail: office?.Email,
            RevenueOver200M: pl is not null && pl.TotalIncome > 200_000_000m,
            Page3: page3,
            Page7: page7,
            ScheduleCells: scheduleCells);

        return svc.Build(data);
    }
}
