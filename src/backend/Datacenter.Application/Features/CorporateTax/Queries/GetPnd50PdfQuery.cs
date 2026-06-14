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

        // หน้า 3 (รายการที่ 3): ดึง breakdown จากงบกำไรขาดทุน (ถ้ามี) ให้ reconcile กับ r
        Pnd50Page3Data? page3 = null;
        if (tax.HasProfitLoss)
        {
            try
            {
                var pl = await sender.Send(
                    new FinancialStatement.Queries.GetProfitLossQuery(req.ClientCompanyId, req.FiscalYear), ct);
                var sga = pl.TotalExpenses - pl.CostOfGoods.Amount + Math.Abs(pl.FinanceCost.Amount);
                page3 = new Pnd50Page3Data(
                    Revenue: pl.TotalIncome,
                    Cogs: pl.CostOfGoods.Amount,
                    GrossProfit: pl.GrossProfit,
                    Sga: sga,
                    NetAccountingProfit: r.NetProfitBeforeTax,
                    AddBack: r.AddBackTotal,
                    Deduction: r.DeductionTotal,
                    AdjustedProfit: r.AdjustedProfit,
                    LossUsed: r.LossUsed,
                    NetTaxableIncome: r.NetTaxableIncome);
            }
            catch { /* ไม่มีงบ → ไม่เติมหน้า 3 */ }
        }

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
            Page3: page3);

        return svc.Build(data);
    }
}
