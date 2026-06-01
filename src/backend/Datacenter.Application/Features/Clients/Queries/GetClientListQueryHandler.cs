using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Models;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Clients.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Clients.Queries;

public class GetClientListQueryHandler(IApplicationDbContext db, ICompanyAccessGuard accessGuard)
    : IRequestHandler<GetClientListQuery, PaginatedResult<ClientListDto>>
{
    public async Task<PaginatedResult<ClientListDto>> Handle(GetClientListQuery request, CancellationToken ct)
    {
        var query = db.ClientCompanies.AsNoTracking();

        // null = Admin เห็นทุกบริษัท; ไม่ใช่ Admin → กรองเฉพาะบริษัทที่เข้าถึงได้
        var accessibleIds = await accessGuard.GetAccessibleCompanyIdsAsync(ct);
        if (accessibleIds is not null)
            query = query.Where(c => accessibleIds.Contains(c.Id));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(c =>
                c.Code.ToLower().Contains(search) ||
                c.Name.ToLower().Contains(search) ||
                c.TaxId.Contains(search));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.Code)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new ClientListDto(c.Id, c.Code, c.Name, c.TaxId, c.IsActive))
            .ToListAsync(ct);

        return PaginatedResult<ClientListDto>.Create(items, total, request.PageNumber, request.PageSize);
    }
}
