using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using MediatR;

namespace Datacenter.Application.Features.Vat.Queries;

/// <summary>
/// ไฟล์โอนย้ายข้อมูล ภ.พ.30 (.txt) ของงวดเดือนที่เลือก — สำหรับอัปโหลดหน้า e-Filing.
/// </summary>
public record GetVatPp30TransferQuery(
    int ClientCompanyId, int Year, int Month, string Delimiter = "|", bool IncludeHeader = true)
    : IRequest<byte[]>, IRequireCompanyAccess;

public class GetVatPp30TransferQueryHandler(ISender sender, IVatPp30TransferExportService svc)
    : IRequestHandler<GetVatPp30TransferQuery, byte[]>
{
    public async Task<byte[]> Handle(GetVatPp30TransferQuery req, CancellationToken ct)
    {
        var data = await sender.Send(
            new GetVatPp30BranchesQuery(req.ClientCompanyId, req.Year, req.Month), ct);
        return svc.BuildTransferFile(data.Branches, req.Delimiter, req.IncludeHeader);
    }
}
