using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Clients.Commands;

public class DeactivateClientCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<DeactivateClientCommand>
{
    public async Task Handle(DeactivateClientCommand request, CancellationToken ct)
    {
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("เฉพาะ Admin เท่านั้นที่สามารถปิดใช้งานบริษัทลูกค้าได้");

        var client = await db.ClientCompanies
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException("ClientCompany", request.Id);

        client.IsActive = false;
        client.ModifiedAt = DateTime.UtcNow;
        client.ModifiedBy = currentUser.Username;

        await audit.LogAsync("Deactivate", "ClientCompany", client.Id.ToString(),
            beforeValue: "Active", afterValue: "Inactive", cancellationToken: ct);

        await db.SaveChangesAsync(ct);
    }
}
