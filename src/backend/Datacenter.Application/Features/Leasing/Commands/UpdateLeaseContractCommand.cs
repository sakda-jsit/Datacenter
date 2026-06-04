using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Leasing.DTOs;
using MediatR;

namespace Datacenter.Application.Features.Leasing.Commands;

/// <summary>แก้ไขสัญญาเช่าซื้อ/เงินกู้</summary>
public record UpdateLeaseContractCommand(int Id, int ClientCompanyId, LeaseContractInput Data)
    : IRequest<LeaseContractDto>, IRequireCompanyAccess;
