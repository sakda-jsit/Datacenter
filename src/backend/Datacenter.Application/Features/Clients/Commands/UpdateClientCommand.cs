using Datacenter.Application.Common.Security;
using MediatR;

namespace Datacenter.Application.Features.Clients.Commands;

public record UpdateClientCommand(
    int Id,
    string Name,
    string TaxId,
    string BranchCode,
    string? Address,
    int FiscalYearStartMonth)
    : IRequest, IRequireCompanyAccess
{
    public int ClientCompanyId => Id;
}
