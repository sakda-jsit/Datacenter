using MediatR;

namespace Datacenter.Application.Features.Clients.Commands;

public record DeactivateClientCommand(int Id) : IRequest;
