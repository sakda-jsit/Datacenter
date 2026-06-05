using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Wht.DTOs;
using MediatR;

namespace Datacenter.Application.Features.Wht.Queries;

/// <summary>รายงานภาษีหัก ณ ที่จ่ายรายเดือน (ม.ค.–ธ.ค.) ของปีปฏิทินที่ระบุ — แยก ภ.ง.ด.3/53</summary>
public record GetWhtReportQuery(int ClientCompanyId, int Year)
    : IRequest<WhtReportDto>, IRequireCompanyAccess;
