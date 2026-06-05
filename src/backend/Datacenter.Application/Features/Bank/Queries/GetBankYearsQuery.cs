using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Bank.Queries;

/// <summary>ปีที่มีรายการเดินบัญชีธนาคาร</summary>
public record GetBankYearsQuery(int ClientCompanyId) : IRequest<IReadOnlyList<int>>, IRequireCompanyAccess;

public class GetBankYearsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBankYearsQuery, IReadOnlyList<int>>
{
    public async Task<IReadOnlyList<int>> Handle(GetBankYearsQuery request, CancellationToken ct)
        => await db.BankTransactions
            .AsNoTracking()
            .Where(t => t.ClientCompanyId == request.ClientCompanyId)
            .Select(t => t.TransactionDate.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync(ct);
}
