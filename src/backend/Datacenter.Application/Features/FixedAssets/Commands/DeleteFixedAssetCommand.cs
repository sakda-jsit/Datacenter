using Datacenter.Application.Common.Security;
using MediatR;

namespace Datacenter.Application.Features.FixedAssets.Commands;

/// <summary>ลบสินทรัพย์ออกจากทะเบียน (ทุกคนที่มีสิทธิ์ในบริษัทลบได้ + audit trail, req v11 #7)</summary>
public record DeleteFixedAssetCommand(int Id, int ClientCompanyId)
    : IRequest, IRequireCompanyAccess;
