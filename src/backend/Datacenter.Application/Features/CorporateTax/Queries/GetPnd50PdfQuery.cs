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

        // ผู้สอบบัญชีของ "รอบปีนี้" (เปลี่ยนได้รายปี → เก็บแยกที่ CompanyAuditor)
        var auditor = await db.CompanyAuditors.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientCompanyId == req.ClientCompanyId
                                   && x.FiscalYear == req.FiscalYear, ct);

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
            AuditorName: auditor?.AuditorName,
            AuditorLicenseNo: auditor?.AuditorLicenseNo,
            AuditorTaxId: auditor?.AuditorTaxId,
            BookkeeperName: auditor?.BookkeeperName,
            BookkeeperTaxId: auditor?.BookkeeperTaxId,
            AuditorSignDate: auditor?.SignDate,
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
            NetPayable: r.NetPayable);

        return svc.Build(data);
    }
}
