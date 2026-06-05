using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Models;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Import.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Import.Queries;

public class GetImportHistoryQueryHandler(IApplicationDbContext db, ICompanyAccessGuard accessGuard)
    : IRequestHandler<GetImportHistoryQuery, PaginatedResult<ImportBatchListDto>>
{
    public async Task<PaginatedResult<ImportBatchListDto>> Handle(GetImportHistoryQuery request, CancellationToken ct)
    {
        var query = db.ImportBatches
            .AsNoTracking()
            .Include(x => x.ClientCompany)
            .AsQueryable();

        if (request.ClientCompanyId.HasValue)
        {
            // ระบุบริษัทเจาะจง → ตรวจสิทธิ์ก่อนกรอง
            await accessGuard.EnsureAccessAsync(request.ClientCompanyId.Value, ct);
            query = query.Where(x => x.ClientCompanyId == request.ClientCompanyId.Value);
        }
        else
        {
            // ไม่ระบุบริษัท → จำกัดเฉพาะบริษัทที่ผู้ใช้เข้าถึงได้ (null = Admin เห็นทั้งหมด)
            var accessibleIds = await accessGuard.GetAccessibleCompanyIdsAsync(ct);
            if (accessibleIds is not null)
                query = query.Where(x => accessibleIds.Contains(x.ClientCompanyId));
        }

        if (request.FiscalYear.HasValue)
            query = query.Where(x => x.FiscalYear == request.FiscalYear.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ImportBatchListDto(
                x.Id,
                x.ClientCompanyId,
                x.ClientCompany.Code,
                x.ClientCompany.LegalName,
                x.SourceType,
                x.ImportType,
                x.FiscalYear,
                x.Status,
                x.TotalRows,
                x.SuccessRows,
                x.ErrorRows,
                x.Message,
                x.CreatedAt,
                x.CreatedBy,
                x.FinishedAt,
                x.IsPosted,
                x.PostedAt))
            .ToListAsync(ct);

        return PaginatedResult<ImportBatchListDto>.Create(items, total, request.PageNumber, request.PageSize);
    }
}
