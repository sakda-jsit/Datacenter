using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Wht.Commands;

/// <summary>กำหนด/แก้ไขอีเมลของผู้ถูกหัก (req 1) — upsert WhtPayee by (บริษัท, เลขผู้เสียภาษี)</summary>
public record UpdateWhtPayeeEmailCommand(int ClientCompanyId, string TaxId, string? Email)
    : IRequest<Unit>, IRequireCompanyAccess;

public class UpdateWhtPayeeEmailCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateWhtPayeeEmailCommand, Unit>
{
    public async Task<Unit> Handle(UpdateWhtPayeeEmailCommand request, CancellationToken ct)
    {
        var taxId = request.TaxId.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        var payee = await db.WhtPayees
            .FirstOrDefaultAsync(p => p.ClientCompanyId == request.ClientCompanyId && p.TaxId == taxId, ct);

        if (payee is null)
        {
            db.WhtPayees.Add(new WhtPayee
            {
                ClientCompanyId = request.ClientCompanyId,
                TaxId = taxId,
                Email = email,
                CreatedBy = currentUser.Username,
            });
        }
        else
        {
            payee.Email = email;
            payee.ModifiedBy = currentUser.Username;
            payee.ModifiedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
