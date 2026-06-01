using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.TrialBalance.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.TrialBalance.Queries;

public class GetAccountListQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAccountListQuery, IReadOnlyList<AccountListDto>>
{
    public async Task<IReadOnlyList<AccountListDto>> Handle(GetAccountListQuery request, CancellationToken ct)
    {
        var query = db.Accounts
            .AsNoTracking()
            .Where(a => a.ClientCompanyId == request.ClientCompanyId);

        if (request.ActiveOnly)
            query = query.Where(a => a.IsActive);

        return await query
            .OrderBy(a => a.AccountCode)
            .Select(a => new AccountListDto(
                a.Id, a.AccountCode, a.AccountName, a.AccountName2,
                a.AccountType, a.Level, a.ParentCode, a.IsPostable, a.IsActive))
            .ToListAsync(ct);
    }
}
