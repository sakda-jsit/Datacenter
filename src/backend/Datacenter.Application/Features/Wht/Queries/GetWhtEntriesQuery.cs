using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Wht.DTOs;
using MediatR;

namespace Datacenter.Application.Features.Wht.Queries;

/// <summary>
/// รายละเอียดรายการภาษีหัก ณ ที่จ่าย (ตัวกรอง: ปี, เดือน, แบบ).
/// Month = 0 → ทั้งปี; FormType = null → ทั้ง ภ.ง.ด.3 และ 53.
/// </summary>
public record GetWhtEntriesQuery(int ClientCompanyId, int Year, int Month = 0, int? FormType = null)
    : IRequest<IReadOnlyList<WhtEntryListItemDto>>, IRequireCompanyAccess;
