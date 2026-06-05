using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Vat.DTOs;
using MediatR;

namespace Datacenter.Application.Features.Vat.Queries;

/// <summary>
/// รายละเอียดรายการภาษีซื้อ/ขาย (ตัวกรอง: ปี, เดือน, ประเภท).
/// Month = 0 → ทั้งปี; VatType = null → ทั้งสองประเภท.
/// </summary>
public record GetVatEntriesQuery(int ClientCompanyId, int Year, int Month = 0, int? VatType = null)
    : IRequest<IReadOnlyList<VatEntryListItemDto>>, IRequireCompanyAccess;
