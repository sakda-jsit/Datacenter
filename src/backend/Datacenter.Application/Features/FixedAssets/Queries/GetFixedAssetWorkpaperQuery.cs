using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FixedAssets.DTOs;
using MediatR;

namespace Datacenter.Application.Features.FixedAssets.Queries;

/// <summary>กระดาษทำการสินทรัพย์ถาวร (RPT-013) + สรุปตามประเภท + เทียบ GL (ชุดบัญชี)</summary>
public record GetFixedAssetWorkpaperQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<FixedAssetWorkpaperDto>, IRequireCompanyAccess;
