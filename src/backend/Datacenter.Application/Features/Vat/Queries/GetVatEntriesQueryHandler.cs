using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Vat.DTOs;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Vat.Queries;

public class GetVatEntriesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetVatEntriesQuery, IReadOnlyList<VatEntryListItemDto>>
{
    public async Task<IReadOnlyList<VatEntryListItemDto>> Handle(GetVatEntriesQuery request, CancellationToken ct)
    {
        var query = db.VatEntries
            .AsNoTracking()
            .Where(v => v.ClientCompanyId == request.ClientCompanyId && v.TaxPeriod.Year == request.Year);

        if (request.Month is >= 1 and <= 12)
            query = query.Where(v => v.TaxPeriod.Month == request.Month);

        if (request.VatType is 1 or 2)
        {
            var type = (VatEntryType)request.VatType.Value;
            query = query.Where(v => v.VatType == type);
        }

        // ดึงข้อมูลก่อน แล้วค่อยแปลง enum→int ใน memory: คอลัมน์ VatType เก็บเป็น string
        // (HasConversion<string>) การ cast (int) ใน SQL จะทำให้ CONVERT(int, 'Output') ล้มเหลว
        var rows = await query
            .OrderBy(v => v.TaxPeriod).ThenBy(v => v.VatType).ThenBy(v => v.DocumentDate).ThenBy(v => v.DocumentNo)
            .ToListAsync(ct);

        return rows.Select(v => new VatEntryListItemDto(
            v.Id,
            (int)v.VatType,
            v.TaxPeriod,
            v.DocumentDate,
            v.DocumentNo,
            v.ReferenceNo,
            v.Description,
            v.CounterpartyTaxId,
            v.CounterpartyPrefix,
            v.BaseAmount,
            v.VatAmount,
            v.ZeroRatedAmount,
            v.IsLate)).ToList();
    }
}
