using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Vat.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Vat.Queries;

/// <summary>
/// ไฟล์โอนย้ายข้อมูล ภ.พ.30 (.txt) ของงวดเดือนที่เลือก — สำหรับอัปโหลดหน้า e-Filing.
/// </summary>
public record GetVatPp30TransferQuery(
    int ClientCompanyId, int Year, int Month, string Delimiter = "|", bool IncludeHeader = true)
    : IRequest<byte[]>, IRequireCompanyAccess;

public class GetVatPp30TransferQueryHandler(
    IApplicationDbContext db, ISender sender, IVatPp30TransferExportService svc)
    : IRequestHandler<GetVatPp30TransferQuery, byte[]>
{
    public async Task<byte[]> Handle(GetVatPp30TransferQuery req, CancellationToken ct)
    {
        var company = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == req.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", req.ClientCompanyId);

        var report = await sender.Send(new GetVatReportQuery(req.ClientCompanyId, req.Year), ct);
        var m = report.Months.FirstOrDefault(x => x.Month == req.Month)
            ?? throw new NotFoundException("VatMonth", req.Month);

        var dto = new Pp30TransferDto(
            CompanyName: string.IsNullOrWhiteSpace(company.LegalName) ? company.Name : company.LegalName,
            TaxId: company.TaxId,
            BranchCode: company.BranchCode,
            Year: req.Year,
            Month: req.Month,
            TotalSales: decimal.Round(m.OutputBase + m.OutputZeroRated, 2),
            ZeroRatedSales: m.OutputZeroRated,
            ExemptSales: 0m,
            EligiblePurchase: m.InputBase,
            OutputVat: m.OutputVat,
            InputVat: m.InputVat);

        return svc.BuildTransferFile(dto, req.Delimiter, req.IncludeHeader);
    }
}
