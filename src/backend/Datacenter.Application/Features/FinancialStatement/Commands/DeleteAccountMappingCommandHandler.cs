using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FinancialStatement.Commands;

public class DeleteAccountMappingCommandHandler(IApplicationDbContext db, IAuditService audit)
    : IRequestHandler<DeleteAccountMappingCommand>
{
    public async Task Handle(DeleteAccountMappingCommand request, CancellationToken ct)
    {
        var m = await db.AccountStatementMappings
            .FirstOrDefaultAsync(x =>
                x.ClientCompanyId == request.ClientCompanyId &&
                x.AccountCode == request.AccountCode, ct)
            ?? throw new NotFoundException("AccountStatementMapping", request.AccountCode);

        db.AccountStatementMappings.Remove(m);

        await audit.LogAsync("Delete", "AccountStatementMapping",
            $"{request.ClientCompanyId}:{request.AccountCode}",
            beforeValue: m.RefCode,
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);
    }
}
