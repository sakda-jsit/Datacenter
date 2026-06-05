using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FixedAssets.DTOs;
using MediatR;

namespace Datacenter.Application.Features.FixedAssets.Commands;

/// <summary>แก้ไขสินทรัพย์ถาวร (รวมบันทึกการจำหน่าย/ขาย/ตัดจำหน่าย)</summary>
public record UpdateFixedAssetCommand(int Id, int ClientCompanyId, FixedAssetInput Data)
    : IRequest<FixedAssetDto>, IRequireCompanyAccess;
