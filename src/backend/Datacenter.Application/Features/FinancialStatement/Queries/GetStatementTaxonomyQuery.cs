using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FinancialStatement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FinancialStatement.Queries;

/// <summary>
/// ผังมาตรฐานงบการเงิน (DBD/NPAE group-code taxonomy) — รายการบรรทัดมาตรฐานทั้งหมด (master `StatementLine`,
/// ใช้ร่วมทุกบริษัท = single source of truth สำหรับ RefCode) + จำนวนบัญชีของบริษัทที่เลือกที่ map เข้าแต่ละบรรทัด.
/// </summary>
public record GetStatementTaxonomyQuery(int ClientCompanyId)
    : IRequest<StatementTaxonomyDto>, IRequireCompanyAccess;

public class GetStatementTaxonomyQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetStatementTaxonomyQuery, StatementTaxonomyDto>
{
    public async Task<StatementTaxonomyDto> Handle(GetStatementTaxonomyQuery request, CancellationToken ct)
    {
        var lines = await db.StatementLines.AsNoTracking()
            .OrderBy(l => l.SortOrder)
            .ToListAsync(ct);

        var counts = await db.AccountStatementMappings.AsNoTracking()
            .Where(m => m.ClientCompanyId == request.ClientCompanyId)
            .GroupBy(m => m.RefCode)
            .Select(g => new { RefCode = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RefCode, x => x.Count, ct);

        var dto = lines
            .Select(l => new StatementTaxonomyLineDto(
                l.RefCode, l.LineName, l.Section, l.SortOrder, counts.GetValueOrDefault(l.RefCode)))
            .ToList();

        return new StatementTaxonomyDto(request.ClientCompanyId, dto);
    }
}
