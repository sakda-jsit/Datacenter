using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Adjustments.DTOs;
using MediatR;

namespace Datacenter.Application.Features.Adjustments.Queries;

/// <summary>รายการ adjustment ทั้งหมดของบริษัทในปีงบ</summary>
public record GetAdjustmentEntriesQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<IReadOnlyList<AdjustmentEntryDto>>, IRequireCompanyAccess;
