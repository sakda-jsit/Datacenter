using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FixedAssets.DTOs;
using MediatR;

namespace Datacenter.Application.Features.FixedAssets.Commands;

/// <summary>
/// บันทึกการแมพหมวด→บัญชี GL (upsert ตาม CategoryCode) + เติมบัญชีให้สินทรัพย์ที่ใช้หมวดนั้น
/// ที่ยังไม่มีบัญชี เพื่อให้พร้อม generate adjustment ทันทีโดยไม่ต้อง import ซ้ำ.
/// </summary>
public record UpsertAssetAccountMappingsCommand(
    int ClientCompanyId,
    IReadOnlyList<AssetAccountMappingInput> Mappings)
    : IRequest<IReadOnlyList<AssetAccountMappingDto>>, IRequireCompanyAccess;
