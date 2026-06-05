using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FixedAssets.DTOs;
using MediatR;

namespace Datacenter.Application.Features.FixedAssets.Queries;

/// <summary>
/// รายการแมพหมวด→บัญชี GL ของบริษัท. รวมหมวด (CategoryCode) ที่พบในสินทรัพย์แต่ยังไม่มี mapping
/// (DTO ที่ Id=0) เพื่อให้หน้าจอเติมบัญชีได้ครบ.
/// </summary>
public record GetAssetAccountMappingsQuery(int ClientCompanyId)
    : IRequest<IReadOnlyList<AssetAccountMappingDto>>, IRequireCompanyAccess;
