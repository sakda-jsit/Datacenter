using MediatR;

namespace Datacenter.Application.Features.Auth.Commands;

public record LoginCommand(string Username, string Password)
    : IRequest<LoginResult>;

public record LoginResult(
    int UserId,
    string Username,
    string DisplayName,
    string Role,
    string Token);
