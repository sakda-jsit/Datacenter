using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FixedAssets.DTOs;
using MediatR;

namespace Datacenter.Application.Features.FixedAssets.Queries;

/// <summary>รายละเอียดสินทรัพย์ + ตารางค่าเสื่อม 2 ชุด + ผลการจำหน่าย ณ FiscalYear</summary>
public record GetFixedAssetQuery(int ClientCompanyId, int Id, int FiscalYear)
    : IRequest<FixedAssetDetailDto>, IRequireCompanyAccess;
