using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Clients.DTOs;
using MediatR;

namespace Datacenter.Application.Features.Clients.Queries;

public record GetClientDetailQuery(int Id) : IRequest<ClientDetailDto>, IRequireCompanyAccess
{
    public int ClientCompanyId => Id;
}
