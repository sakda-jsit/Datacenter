using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Vat.DTOs;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Vat.Queries;

/// <summary>
/// ยอด ภ.พ.30 ของงวดเดือนที่เลือก แยกตามสาขา (group ตาม ISVAT.DEPCOD) สำหรับการยื่นรวมกัน.
/// กฎแปลง DEPCOD → เลขสาขา RD: ว่าง/ขึ้นต้น HO → สำนักงานใหญ่ (00000); อื่น ๆ ใช้ตัวเลขท้ายรหัส (BR01 → 00001).
/// </summary>
public record GetVatPp30BranchesQuery(int ClientCompanyId, int Year, int Month)
    : IRequest<Pp30BranchesDto>, IRequireCompanyAccess;

public class GetVatPp30BranchesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetVatPp30BranchesQuery, Pp30BranchesDto>
{
    /// <summary>แปลงรหัสแผนก/สาขา Express → (เลขสาขา RD, เป็นสำนักงานใหญ่?). ว่าง/HO* = สำนักงานใหญ่.</summary>
    public static (string BranchNo, bool IsHeadOffice) ResolveBranch(string? depcod)
    {
        var d = (depcod ?? "").Trim().ToUpperInvariant();
        if (d.Length == 0 || d.StartsWith("HO")) return ("00000", true);
        var digits = new string(d.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return ("00000", true);
        return int.TryParse(digits, out var n) ? (n.ToString("D5"), false) : ("00000", true);
    }

    public async Task<Pp30BranchesDto> Handle(GetVatPp30BranchesQuery req, CancellationToken ct)
    {
        var company = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == req.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", req.ClientCompanyId);

        // ดึงรายการ VAT ของงวดเดือนนั้น แล้ว group ตามสาขาใน memory (VatType เก็บเป็น string — เลี่ยง cast)
        var entries = await db.VatEntries.AsNoTracking()
            .Where(v => v.ClientCompanyId == req.ClientCompanyId
                     && v.TaxPeriod.Year == req.Year
                     && v.TaxPeriod.Month == req.Month)
            .Select(v => new { v.VatType, v.DepartmentCode, v.BaseAmount, v.VatAmount, v.ZeroRatedAmount })
            .ToListAsync(ct);

        var branches = entries
            .GroupBy(v => ResolveBranch(v.DepartmentCode))
            .Select(g =>
            {
                var output = g.Where(x => x.VatType == VatEntryType.Output);
                var input = g.Where(x => x.VatType == VatEntryType.Input);
                var outBase = output.Sum(x => x.BaseAmount);
                var zero = output.Sum(x => x.ZeroRatedAmount);
                return new Pp30BranchRow(
                    DepartmentCode: g.Key.IsHeadOffice ? "HO" : g.Key.BranchNo,
                    BranchNo: g.Key.BranchNo,
                    IsHeadOffice: g.Key.IsHeadOffice,
                    TotalSales: Math.Round(outBase + zero, 2),
                    ZeroRatedSales: Math.Round(zero, 2),
                    ExemptSales: 0m,
                    EligiblePurchase: Math.Round(input.Sum(x => x.BaseAmount), 2),
                    OutputVat: Math.Round(output.Sum(x => x.VatAmount), 2),
                    InputVat: Math.Round(input.Sum(x => x.VatAmount), 2));
            })
            .OrderByDescending(b => b.IsHeadOffice)   // สำนักงานใหญ่ก่อน
            .ThenBy(b => b.BranchNo, StringComparer.Ordinal)
            .ToList();

        return new Pp30BranchesDto(
            CompanyName: string.IsNullOrWhiteSpace(company.LegalName) ? company.Name : company.LegalName,
            TaxId: company.TaxId,
            Year: req.Year,
            Month: req.Month,
            IsMultiBranch: branches.Count > 1,
            Branches: branches);
    }
}
