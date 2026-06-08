using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using MediatR;

namespace Datacenter.Application.Features.Wht.Queries;

/// <summary>
/// ไฟล์ใบแนบ ภ.ง.ด.3 / ภ.ง.ด.53 (e-Filing TXT, TIS-620) — ทั้งปีหรือเฉพาะเดือน.
/// FormType: 3 = ภ.ง.ด.3 (บุคคล), 53 = ภ.ง.ด.53 (นิติบุคคล). Month = 0 → ทั้งปี.
/// </summary>
public record GetWhtPndTxtQuery(int ClientCompanyId, int Year, int FormType, int Month = 0)
    : IRequest<byte[]>, IRequireCompanyAccess;

public class GetWhtPndTxtQueryHandler(ISender sender, IWhtEfilingExportService svc)
    : IRequestHandler<GetWhtPndTxtQuery, byte[]>
{
    public async Task<byte[]> Handle(GetWhtPndTxtQuery req, CancellationToken ct)
    {
        var entries = await sender.Send(
            new GetWhtEntriesQuery(req.ClientCompanyId, req.Year, req.Month, req.FormType), ct);
        return svc.BuildPndTxt(entries);
    }
}
