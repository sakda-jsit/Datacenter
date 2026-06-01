using Datacenter.Domain.Enums;
using MediatR;

namespace Datacenter.Application.Features.ComplianceCalendar.Commands;

public record UpdateTaskStatusCommand(
    int TaskId,
    ComplianceTaskStatus Status,
    string? Note = null
) : IRequest;
