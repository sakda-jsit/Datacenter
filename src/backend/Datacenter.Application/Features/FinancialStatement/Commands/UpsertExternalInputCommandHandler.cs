using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FinancialStatement.Commands;

public class UpsertExternalInputCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<UpsertExternalInputCommand>
{
    public async Task Handle(UpsertExternalInputCommand request, CancellationToken ct)
    {
        var existing = await db.FsExternalInputs.FirstOrDefaultAsync(x =>
            x.ClientCompanyId == request.ClientCompanyId &&
            x.FiscalYear == request.FiscalYear &&
            x.RefCode == request.RefCode, ct);

        string action;
        string? before = null;

        if (existing is null)
        {
            db.FsExternalInputs.Add(new FsExternalInput
            {
                ClientCompanyId = request.ClientCompanyId,
                FiscalYear      = request.FiscalYear,
                RefCode         = request.RefCode,
                Amount          = request.Amount,
                Note            = request.Note,
                CreatedBy       = currentUser.Username,
            });
            action = "Create";
        }
        else
        {
            before = existing.Amount.ToString("N2");
            existing.Amount     = request.Amount;
            existing.Note       = request.Note;
            existing.ModifiedAt = DateTime.UtcNow;
            existing.ModifiedBy = currentUser.Username;
            action = "Update";
        }

        await audit.LogAsync(action, "FsExternalInput",
            $"{request.ClientCompanyId}:{request.FiscalYear}:{request.RefCode}",
            beforeValue: before,
            afterValue: request.Amount.ToString("N2"),
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);
    }
}
