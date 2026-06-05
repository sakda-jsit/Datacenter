using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using MediatR;

namespace Datacenter.Application.Features.FinancialStatement.Queries;

/// <summary>ส่งออกหมายเหตุประกอบงบ (NOTE2) เป็นไฟล์ Excel รูปแบบงบที่ยื่น (ClosedXML).</summary>
public record GetNotesExcelQuery(int ClientCompanyId, int FiscalYear, string? DirectorName = null)
    : IRequest<byte[]>, IRequireCompanyAccess;

public class GetNotesExcelQueryHandler(IMediator mediator, INote2ExcelExporter exporter)
    : IRequestHandler<GetNotesExcelQuery, byte[]>
{
    public async Task<byte[]> Handle(GetNotesExcelQuery request, CancellationToken ct)
    {
        var data = await mediator.Send(
            new GetNotesToFsQuery(request.ClientCompanyId, request.FiscalYear), ct);
        return exporter.Build(data, request.DirectorName ?? "");
    }
}
