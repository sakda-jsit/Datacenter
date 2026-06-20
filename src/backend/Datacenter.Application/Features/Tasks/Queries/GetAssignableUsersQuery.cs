using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Tasks.Queries;

public record AssignableUserDto(int UserId, string DisplayName, string Username, int Role);

/// <summary>ผู้ใช้ที่มอบหมายงานในบริษัทนี้ได้ = Admin (ทุกบริษัท) + ผู้ที่มี CompanyUserAccess ของบริษัทนี้</summary>
public record GetAssignableUsersQuery(int ClientCompanyId)
    : IRequest<IReadOnlyList<AssignableUserDto>>, IRequireCompanyAccess;

public class GetAssignableUsersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAssignableUsersQuery, IReadOnlyList<AssignableUserDto>>
{
    public async Task<IReadOnlyList<AssignableUserDto>> Handle(GetAssignableUsersQuery request, CancellationToken ct)
    {
        var accessUserIds = await db.CompanyUserAccesses.AsNoTracking()
            .Where(a => a.ClientCompanyId == request.ClientCompanyId)
            .Select(a => a.UserId)
            .ToListAsync(ct);

        return await db.Users.AsNoTracking()
            .Where(u => u.IsActive && (u.Role == UserRole.Admin || accessUserIds.Contains(u.Id)))
            .OrderBy(u => u.DisplayName)
            .Select(u => new AssignableUserDto(u.Id, u.DisplayName, u.Username, (int)u.Role))
            .ToListAsync(ct);
    }
}
