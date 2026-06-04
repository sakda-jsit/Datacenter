using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Leasing.DTOs;
using MediatR;

namespace Datacenter.Application.Features.Leasing.Queries;

/// <summary>รายการสัญญาทั้งหมดของบริษัท</summary>
public record GetLeaseContractsQuery(int ClientCompanyId, bool IncludeInactive = false)
    : IRequest<IReadOnlyList<LeaseContractListItemDto>>, IRequireCompanyAccess;
