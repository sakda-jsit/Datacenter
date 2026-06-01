using MediatR;

namespace Datacenter.Application.Features.ComplianceCalendar.Commands;

public record AssignTaskCommand(int TaskId, int? UserId) : IRequest;
