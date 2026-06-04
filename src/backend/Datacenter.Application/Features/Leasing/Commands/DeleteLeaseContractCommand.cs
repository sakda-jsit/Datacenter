using Datacenter.Application.Common.Security;
using MediatR;

namespace Datacenter.Application.Features.Leasing.Commands;

/// <summary>ลบสัญญา (ทุกคนลบได้ + audit trail — req v11 #7)</summary>
public record DeleteLeaseContractCommand(int Id, int ClientCompanyId)
    : IRequest, IRequireCompanyAccess;
