using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.CorporateTax.DTOs;
using Datacenter.Application.Features.CorporateTax.Services;
using Datacenter.Application.Features.FinancialStatement.DTOs;
using Datacenter.Application.Features.FinancialStatement.Queries;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.CorporateTax.Queries;

/// <summary>
/// กระดาษทำการคำนวณภาษีเงินได้นิติบุคคล (ภ.ง.ด.50) ของ (บริษัท, ปีงบ) พร้อมผลการคำนวณสด.
/// ถ้ายังไม่เคยบันทึก จะคืนค่าเริ่มต้น (SME ขั้นบันได, ไม่มีรายการปรับปรุง).
/// </summary>
public record GetTaxComputationQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<TaxComputationDto>, IRequireCompanyAccess;

public class GetTaxComputationQueryHandler(IApplicationDbContext db, ISender sender)
    : IRequestHandler<GetTaxComputationQuery, TaxComputationDto>
{
    public async Task<TaxComputationDto> Handle(GetTaxComputationQuery req, CancellationToken ct)
    {
        var clientName = await db.ClientCompanies.AsNoTracking()
            .Where(c => c.Id == req.ClientCompanyId)
            .Select(c => c.LegalName)
            .FirstOrDefaultAsync(ct) ?? "";

        var entity = await db.TaxComputations.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.ClientCompanyId == req.ClientCompanyId
                                   && x.FiscalYear == req.FiscalYear, ct);

        // กำไรสุทธิทางบัญชีก่อนภาษี — ดึงจากงบกำไรขาดทุน (อาจไม่มีถ้ายังไม่ post)
        var warnings = new List<string>();
        decimal profitBeforeTax = 0m;
        bool hasPl = false;
        try
        {
            ProfitLossDto pl = await sender.Send(
                new GetProfitLossQuery(req.ClientCompanyId, req.FiscalYear), ct);
            profitBeforeTax = pl.ProfitBeforeTax;
            hasPl = true;
        }
        catch
        {
            warnings.Add("ไม่พบงบกำไรขาดทุน — ตรวจสอบว่านำเข้าและ post ข้อมูลปีนี้แล้ว และตั้งค่า mapping บัญชี→งบครบ");
        }

        var scheme = entity?.RateScheme ?? TaxRateScheme.SmeTiered;
        var customRate = entity?.CustomRatePct;
        var lossBf = entity?.LossBroughtForward ?? 0m;
        var wht = entity?.WhtCredit ?? 0m;

        var lines = (entity?.Lines ?? new List<Domain.Entities.TaxAdjustmentLine>())
            .OrderBy(l => l.Kind).ThenBy(l => l.SortOrder).ThenBy(l => l.Id)
            .Select(l => new TaxAdjustmentLineDto(l.Id, l.Kind, l.Description, l.Amount, l.SortOrder))
            .ToList();

        var addBack = lines.Where(l => l.Kind == TaxAdjustmentKind.AddBack).Sum(l => l.Amount);
        var deduction = lines.Where(l => l.Kind == TaxAdjustmentKind.Deduction).Sum(l => l.Amount);

        if (scheme == TaxRateScheme.Custom && (customRate is null || customRate <= 0m))
            warnings.Add("เลือกอัตรากำหนดเองแต่ยังไม่ระบุอัตราภาษี (%)");

        var result = CorporateTaxEngine.Compute(
            profitBeforeTax, addBack, deduction, lossBf, wht, scheme, customRate);

        if (entity is null)
            warnings.Add("ยังไม่เคยบันทึกกระดาษทำการภาษีของปีนี้ — แสดงค่าเริ่มต้น (SME ขั้นบันได)");

        return new TaxComputationDto(
            req.ClientCompanyId, clientName, req.FiscalYear,
            scheme, customRate, lossBf, wht, entity?.Note,
            lines, result, hasPl, warnings);
    }
}
