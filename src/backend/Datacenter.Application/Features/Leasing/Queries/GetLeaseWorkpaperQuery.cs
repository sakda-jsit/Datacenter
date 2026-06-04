using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Leasing.DTOs;
using MediatR;

namespace Datacenter.Application.Features.Leasing.Queries;

/// <summary>กระดาษทำการรวม (sheet SUM) + เทียบยอด GL ของบริษัทในปีงบ</summary>
public record GetLeaseWorkpaperQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<LeaseWorkpaperDto>, IRequireCompanyAccess;
