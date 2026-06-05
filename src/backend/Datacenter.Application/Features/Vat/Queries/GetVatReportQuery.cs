using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Vat.DTOs;
using MediatR;

namespace Datacenter.Application.Features.Vat.Queries;

/// <summary>รายงาน ภ.พ.30 รายเดือน (ม.ค.–ธ.ค.) ของปีปฏิทินที่ระบุ</summary>
public record GetVatReportQuery(int ClientCompanyId, int Year)
    : IRequest<VatReportDto>, IRequireCompanyAccess;
