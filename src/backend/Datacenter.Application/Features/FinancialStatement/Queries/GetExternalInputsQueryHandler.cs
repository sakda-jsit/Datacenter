using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.FinancialStatement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FinancialStatement.Queries;

public class GetExternalInputsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetExternalInputsQuery, IReadOnlyList<FsExternalInputDto>>
{
    public async Task<IReadOnlyList<FsExternalInputDto>> Handle(
        GetExternalInputsQuery request, CancellationToken ct)
    {
        return await db.FsExternalInputs.AsNoTracking()
            .Where(x => x.ClientCompanyId == request.ClientCompanyId
                     && x.FiscalYear == request.FiscalYear)
            .OrderBy(x => x.RefCode)
            .Select(x => new FsExternalInputDto(x.Id, x.FiscalYear, x.RefCode, x.Amount, x.Note))
            .ToListAsync(ct);
    }
}
