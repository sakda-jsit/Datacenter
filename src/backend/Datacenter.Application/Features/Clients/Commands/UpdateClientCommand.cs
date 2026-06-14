using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Clients.DTOs;
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
    string? PostalCode = null,
    ClientAddressDto? AddressDetail = null)
    : IRequest, IRequireCompanyAccess
{
    public int ClientCompanyId => Id;
}
