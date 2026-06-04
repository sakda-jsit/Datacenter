using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Leasing.DTOs;
using MediatR;

namespace Datacenter.Application.Features.Leasing.Queries;

/// <summary>รายละเอียดสัญญา + ตารางตัดบัญชี + สรุปสิ้นปีของ FiscalYear</summary>
public record GetLeaseContractQuery(int Id, int ClientCompanyId, int FiscalYear)
    : IRequest<LeaseContractDetailDto>, IRequireCompanyAccess;
