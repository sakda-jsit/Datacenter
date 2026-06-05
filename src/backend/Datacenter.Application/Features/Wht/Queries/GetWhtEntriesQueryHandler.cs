using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Wht.DTOs;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Wht.Queries;

public class GetWhtEntriesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetWhtEntriesQuery, IReadOnlyList<WhtEntryListItemDto>>
{
    public async Task<IReadOnlyList<WhtEntryListItemDto>> Handle(GetWhtEntriesQuery request, CancellationToken ct)
    {
        var query = db.WhtEntries
            .AsNoTracking()
            .Where(w => w.ClientCompanyId == request.ClientCompanyId && w.TaxPeriod.Year == request.Year);

        if (request.Month is >= 1 and <= 12)
            query = query.Where(w => w.TaxPeriod.Month == request.Month);

        if (request.FormType is 3 or 53)
        {
            var form = (WhtFormType)request.FormType.Value;
            query = query.Where(w => w.FormType == form);
        }

        // materialize ก่อนแล้ว map: FormType/EmailStatus เก็บเป็น string (HasConversion) — cast (int) ใน SQL จะ fail
        var rows = await query
            .OrderBy(w => w.TaxPeriod).ThenBy(w => w.FormType).ThenBy(w => w.WithholdDate).ThenBy(w => w.DocumentNo)
            .ToListAsync(ct);

        // อีเมลผู้ถูกหัก (จาก WhtPayee by TaxId)
        var emails = await db.WhtPayees
            .AsNoTracking()
            .Where(p => p.ClientCompanyId == request.ClientCompanyId)
            .ToDictionaryAsync(p => p.TaxId, p => p.Email, ct);

        return rows.Select(w => new WhtEntryListItemDto(
            w.Id,
            (int)w.FormType,
            w.TaxPeriod,
            w.WithholdDate,
            w.DocumentNo,
            w.PayeeName,
            w.PayeePrefix,
            w.PayeeTaxId,
            w.IncomeType,
            w.BaseAmount,
            w.TaxRate,
            w.TaxAmount,
            w.IsLate,
            w.PayeeTaxId != null && emails.TryGetValue(w.PayeeTaxId, out var em) ? em : null,
            (int)w.EmailStatus,
            w.EmailSentAt,
            w.EmailSentBy,
            w.EmailError)).ToList();
    }
}
