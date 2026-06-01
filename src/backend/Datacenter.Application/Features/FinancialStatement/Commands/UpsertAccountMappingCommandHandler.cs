using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FinancialStatement.Commands;

public class UpsertAccountMappingCommandHandler(IApplicationDbContext db, IAuditService audit)
    : IRequestHandler<UpsertAccountMappingCommand>
{
    public async Task Handle(UpsertAccountMappingCommand request, CancellationToken ct)
    {
        bool refExists = await db.StatementLines.AnyAsync(l => l.RefCode == request.RefCode, ct);
        if (!refExists)
            throw new NotFoundException("StatementLine", request.RefCode);

        var existing = await db.AccountStatementMappings
            .FirstOrDefaultAsync(m =>
                m.ClientCompanyId == request.ClientCompanyId &&
                m.AccountCode == request.AccountCode, ct);

        string action;
        if (existing is null)
        {
            db.AccountStatementMappings.Add(new AccountStatementMapping
            {
                ClientCompanyId = request.ClientCompanyId,
                AccountCode     = request.AccountCode,
                AccountName     = request.AccountName,
                RefCode         = request.RefCode,
            });
            action = "Create";
        }
        else
        {
            existing.RefCode     = request.RefCode;
            existing.AccountName = request.AccountName;
            action = "Update";
        }

        await audit.LogAsync(action, "AccountStatementMapping",
            $"{request.ClientCompanyId}:{request.AccountCode}",
            afterValue: request.RefCode,
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);
    }
}
