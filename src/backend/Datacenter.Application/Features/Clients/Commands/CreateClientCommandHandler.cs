using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Clients.Commands;

public class CreateClientCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<CreateClientCommand, int>
{
    public async Task<int> Handle(CreateClientCommand request, CancellationToken ct)
    {
        var codeExists = await db.ClientCompanies
            .AnyAsync(c => c.Code == request.Code, ct);

        if (codeExists)
            throw new Application.Common.Exceptions.ValidationException(
                new[] { new FluentValidation.Results.ValidationFailure("Code", $"รหัสลูกค้า '{request.Code}' มีในระบบแล้ว") });

        var client = new ClientCompany
        {
            Code = request.Code.Trim().ToUpper(),
            Name = request.Name.Trim(),
            TaxId = request.TaxId.Trim(),
            BranchCode = request.BranchCode.Trim(),
            Address = request.Address?.Trim(),
            FiscalYearStartMonth = request.FiscalYearStartMonth,
            IsActive = true,
            CreatedBy = currentUser.Username,
        };

        db.ClientCompanies.Add(client);
        await db.SaveChangesAsync(ct);

        // Log after first save to capture the generated Id
        await audit.LogAsync("Create", "ClientCompany", client.Id.ToString(),
            afterValue: client.Code, cancellationToken: ct);
        await db.SaveChangesAsync(ct);

        return client.Id;
    }
}
