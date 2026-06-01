using Datacenter.Application.Common.Models;
using Datacenter.Application.Features.Clients.DTOs;
using MediatR;

namespace Datacenter.Application.Features.Clients.Queries;

public record GetClientListQuery(int PageNumber = 1, int PageSize = 20, string? Search = null)
    : IRequest<PaginatedResult<ClientListDto>>;
