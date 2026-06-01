using MediatR;

namespace Datacenter.Application.Features.Clients.Commands;

public record CreateClientCommand(
    string Code, string Name, string TaxId,
    string BranchCode, string? Address, int FiscalYearStartMonth)
    : IRequest<int>;
