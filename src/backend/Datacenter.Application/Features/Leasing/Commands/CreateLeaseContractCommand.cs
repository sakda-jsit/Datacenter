using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Leasing.DTOs;
using MediatR;

namespace Datacenter.Application.Features.Leasing.Commands;

/// <summary>สร้างสัญญาเช่าซื้อ/เงินกู้ใหม่</summary>
public record CreateLeaseContractCommand(int ClientCompanyId, LeaseContractInput Data)
    : IRequest<LeaseContractDto>, IRequireCompanyAccess;
