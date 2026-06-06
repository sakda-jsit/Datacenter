using Datacenter.Application.Common;
using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Payroll.DTOs;
using Datacenter.Application.Features.Payroll.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Queries;

// ── กท.20ก: แบบแสดงเงินค่าจ้างประจำปี กองทุนเงินทดแทน ────────────────────────────
// กฎ (จากคำแนะนำท้ายแบบ): เงินค่าจ้าง = ค่าตอบแทนการทำงานเวลาปกติ (ไม่รวม OT/วันหยุด/โบนัส)
// = ฐานค่าจ้างยื่น ปกส. (SsoWageBase) สูงสุดคนละไม่เกิน 240,000 บาท/ปี; ลูกจ้าง = ผู้มีค่าจ้าง
public record GetKt20Query(int ClientCompanyId, int Year) : IRequest<Kt20Dto>, IRequireCompanyAccess;

public class GetKt20QueryHandler(IApplicationDbContext db) : IRequestHandler<GetKt20Query, Kt20Dto>
{
    public async Task<Kt20Dto> Handle(GetKt20Query request, CancellationToken ct)
    {
        var company = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var cfg = PayrollRates.ResolveEffective(
            await db.PayrollRateConfigs.AsNoTracking().ToListAsync(ct), new DateTime(request.Year, 12, 31));
        var ratePct = cfg?.WcfRatePct ?? 0.2m;
        var cap = cfg?.WcfWageCapPerYear ?? 240000m;

        var items = await db.PayrollItems.AsNoTracking()
            .Include(i => i.Employee)
            .Include(i => i.PayrollRun)
            .Where(i => i.PayrollRun!.ClientCompanyId == request.ClientCompanyId && i.PayrollRun.Year == request.Year)
            .ToListAsync(ct);

        var rows = items
            .Where(i => i.Employee != null)
            .GroupBy(i => i.EmployeeId)
            .Select(g =>
            {
                var e = g.First().Employee!;
                var annual = PayrollCalculator.Round2(g.Sum(i => i.SsoWageBase));
                return new
                {
                    Nat = Digits(e.NationalId), e.Prefix, e.FirstName, e.LastName,
                    Annual = annual, Capped = Math.Min(annual, cap),
                };
            })
            .Where(x => x.Annual > 0) // ผู้มีค่าจ้าง ปกส. = ลูกจ้างตามกองทุนเงินทดแทน (กรรมการ/ผู้ไม่มีค่าจ้างไม่นับ)
            .OrderBy(x => x.Nat, StringComparer.Ordinal)
            .Select((x, idx) => new Kt20Row(
                idx + 1, x.Nat, x.Prefix ?? "", x.FirstName, x.LastName, x.Annual, x.Capped))
            .ToList();

        var totalWage = PayrollCalculator.Round2(rows.Sum(r => r.CappedWage));
        var contribution = PayrollCalculator.Round2(totalWage * ratePct / 100m);

        return new Kt20Dto(
            request.Year,
            string.IsNullOrWhiteSpace(company.LegalName) ? company.Name : company.LegalName,
            company.Address, company.PostalCode,
            company.SsoAccountNo ?? "", company.SsoBranchCode ?? "000000",
            ratePct, cap,
            rows, totalWage, rows.Count, contribution, ThaiBahtText.Convert(contribution));
    }

    private static string Digits(string s) => System.Text.RegularExpressions.Regex.Replace(s ?? "", @"\D", "");
}

public record GetKt20ExcelQuery(int ClientCompanyId, int Year) : IRequest<byte[]>, IRequireCompanyAccess;
public class GetKt20ExcelQueryHandler(ISender sender, IKt20ExportService svc)
    : IRequestHandler<GetKt20ExcelQuery, byte[]>
{
    public async Task<byte[]> Handle(GetKt20ExcelQuery req, CancellationToken ct)
        => svc.BuildExcel(await sender.Send(new GetKt20Query(req.ClientCompanyId, req.Year), ct));
}

public record GetKt20PdfQuery(int ClientCompanyId, int Year) : IRequest<byte[]>, IRequireCompanyAccess;
public class GetKt20PdfQueryHandler(ISender sender, IKt20ExportService svc)
    : IRequestHandler<GetKt20PdfQuery, byte[]>
{
    public async Task<byte[]> Handle(GetKt20PdfQuery req, CancellationToken ct)
        => svc.BuildPdf(await sender.Send(new GetKt20Query(req.ClientCompanyId, req.Year), ct));
}

public record GetKt20ImagesQuery(int ClientCompanyId, int Year) : IRequest<IReadOnlyList<string>>, IRequireCompanyAccess;
public class GetKt20ImagesQueryHandler(ISender sender, IKt20ExportService svc)
    : IRequestHandler<GetKt20ImagesQuery, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(GetKt20ImagesQuery req, CancellationToken ct)
        => svc.BuildImages(await sender.Send(new GetKt20Query(req.ClientCompanyId, req.Year), ct))
            .Select(png => "data:image/png;base64," + System.Convert.ToBase64String(png))
            .ToList();
}
