using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FixedAssets.DTOs;
using MediatR;

namespace Datacenter.Application.Features.FixedAssets.Commands;

/// <summary>สร้างสินทรัพย์ถาวรใหม่ในทะเบียน</summary>
public record CreateFixedAssetCommand(int ClientCompanyId, FixedAssetInput Data)
    : IRequest<FixedAssetDto>, IRequireCompanyAccess;
