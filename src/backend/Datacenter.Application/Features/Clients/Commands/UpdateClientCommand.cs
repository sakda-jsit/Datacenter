using Datacenter.Application.Common.Security;
using MediatR;

namespace Datacenter.Application.Features.Clients.Commands;

public record UpdateClientCommand(
    int Id,
    string LegalName,
    string TaxId,
    string BranchCode,
    string? Address,
    int FiscalYearStartMonth,
    string? SsoAccountNo = null,
    string? SsoBranchCode = null,
    string? Phone = null,
    string? PostalCode = null)
    : IRequest, IRequireCompanyAccess
{
    public int ClientCompanyId => Id;
}
