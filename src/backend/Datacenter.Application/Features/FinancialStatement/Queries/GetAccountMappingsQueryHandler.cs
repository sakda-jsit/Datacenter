using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.FinancialStatement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FinancialStatement.Queries;

public class GetAccountMappingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAccountMappingsQuery, IReadOnlyList<AccountMappingDto>>
{
    public async Task<IReadOnlyList<AccountMappingDto>> Handle(
        GetAccountMappingsQuery request, CancellationToken ct)
    {
        return await db.AccountStatementMappings.AsNoTracking()
            .Where(m => m.ClientCompanyId == request.ClientCompanyId)
            .Include(m => m.StatementLine)
            .OrderBy(m => m.StatementLine.SortOrder)
            .ThenBy(m => m.AccountCode)
            .Select(m => new AccountMappingDto(
                m.AccountCode,
                m.AccountName,
                m.RefCode,
                m.StatementLine.LineName,
                m.StatementLine.Section))
            .ToListAsync(ct);
    }
}
