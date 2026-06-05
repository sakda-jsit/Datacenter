using Datacenter.Application.Features.FixedAssets.DTOs;
using MediatR;

namespace Datacenter.Application.Features.FixedAssets.Queries;

/// <summary>มาสเตอร์ประเภทสินทรัพย์ + อัตราค่าเสื่อมมาตรฐาน (global)</summary>
public record GetAssetTypesQuery : IRequest<IReadOnlyList<AssetTypeDto>>;
