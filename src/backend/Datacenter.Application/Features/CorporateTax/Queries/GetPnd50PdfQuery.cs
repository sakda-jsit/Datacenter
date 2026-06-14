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

        // schedule รายการ 8 (รายจ่ายขายและบริหาร): aggregate ยอดบัญชีตาม mapping → บรรทัด CIT50
        var scheduleCells = new List<Pnd50ScheduleCell>();
        if (pl is not null)
        {
            var lines8 = await db.Cit50ScheduleLines.AsNoTracking()
                .Where(l => l.ScheduleNo == 8).ToListAsync(ct);
            if (lines8.Count > 0)
            {
                var maps = await db.AccountCit50Mappings.AsNoTracking()
                    .Where(m => m.ClientCompanyId == req.ClientCompanyId)
                    .ToDictionaryAsync(m => m.AccountCode, m => m.Cit50LineCode, ct);
                var catchAll = lines8.FirstOrDefault(l => l.IsCatchAll)?.Code;
                var sums = lines8.ToDictionary(l => l.Code, _ => 0m);
                foreach (var line in pl.ExpenseLines.Append(pl.FinanceCost))
                    foreach (var a in line.Accounts)
                    {
                        var code = maps.GetValueOrDefault(a.AccountCode) ?? catchAll;
                        if (code is not null && sums.ContainsKey(code)) sums[code] += Math.Abs(a.NetBalance);
                    }
                var total = sums.Where(kv => lines8.First(l => l.Code == kv.Key) is { IsTotal: false }).Sum(kv => kv.Value);
                const double col2Offset = 108.3; // ระยะจากคอลัมน์ "รวม" ไป "เสียภาษี" (รายการ 8)
                foreach (var l in lines8)
                {
                    var v = l.IsTotal ? total : sums[l.Code];
                    scheduleCells.Add(new Pnd50ScheduleCell(l.PdfPage, l.PdfX, l.PdfY, l.PdfW, v));            // รวม
                    scheduleCells.Add(new Pnd50ScheduleCell(l.PdfPage, l.PdfX - col2Offset, l.PdfY, l.PdfW, v)); // เสียภาษี
                }
            }
        }

        // หน้า 7 (รายการที่ 12 งบดุล): crosswalk บรรทัด CIT50 ← RefCode ผังงบ (ใช้ยอด presentation จาก BS)
        Pnd50Page7Data? page7 = null;
        try
        {
            var bs = await sender.Send(
                new FinancialStatement.Queries.GetBalanceSheetQuery(req.ClientCompanyId, req.FiscalYear), ct);
            var amt = bs.Assets.Concat(bs.Liabilities).Concat(bs.Equity)
                .GroupBy(l => l.RefCode).ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
            decimal R(params string[] codes) => codes.Sum(c => amt.TryGetValue(c, out var v) ? v : 0m);
            var re = R("RE");
            page7 = new Pnd50Page7Data(
                Cash: R("A1"), Ar: R("A7"), Inventory: R("A3"), OtherCurrentAsset: R("A2", "A4", "TXR"),
                LoansToRelated: R("A8"), Ppe: R("A5"), OtherAssetNet: R("A9", "A10"), OtherNonCurrentAsset: R("A6"),
                TotalAssets: bs.TotalAssets,
                BankOdShortLoan: R("L3"), Ap: R("L1"), CurrentLoan: R("L5"), OtherCurrentLiab: R("L2", "TXP"),
                LongTermLoan: R("L6"), OtherNonCurrentLiab: R("L4"),
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
            Page3: page3,
            Page7: page7,
            ScheduleCells: scheduleCells);

        return svc.Build(data);
    }
}
