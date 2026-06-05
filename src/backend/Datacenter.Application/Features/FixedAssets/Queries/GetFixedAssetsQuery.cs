using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FixedAssets.DTOs;
using MediatR;

namespace Datacenter.Application.Features.FixedAssets.Queries;

/// <summary>รายการสินทรัพย์ถาวรของบริษัท</summary>
public record GetFixedAssetsQuery(int ClientCompanyId, bool IncludeInactive = false)
    : IRequest<FixedAssetListDto>, IRequireCompanyAccess;
