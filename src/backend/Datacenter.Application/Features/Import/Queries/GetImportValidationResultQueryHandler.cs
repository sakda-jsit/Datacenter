using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Import.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Import.Queries;

public class GetImportValidationResultQueryHandler(IApplicationDbContext db, ICompanyAccessGuard accessGuard)
    : IRequestHandler<GetImportValidationResultQuery, ImportValidationSummaryDto>
{
    public async Task<ImportValidationSummaryDto> Handle(GetImportValidationResultQuery request, CancellationToken ct)
    {
        var batch = await db.ImportBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ImportBatchId, ct)
            ?? throw new NotFoundException("ImportBatch", request.ImportBatchId);

        // batch อ้างถึงบริษัทผ่าน ImportBatchId จึงตรวจสิทธิ์หลังโหลด entity แทน pipeline behaviour
        await accessGuard.EnsureAccessAsync(batch.ClientCompanyId, ct);

        var errorDetails = await db.ImportBatchDetails
            .AsNoTracking()
            .Where(x => x.ImportBatchId == request.ImportBatchId && !x.IsValid)
            .OrderBy(x => x.RowNumber)
            .Select(x => new ImportBatchDetailDto(
                x.Id, x.RowNumber, x.AccountCode, x.IsValid, x.ErrorMessage, x.RawData))
            .ToListAsync(ct);

        return new ImportValidationSummaryDto(
            batch.Id,
            batch.TotalRows,
            batch.SuccessRows,
            batch.ErrorRows,
            errorDetails);
    }
}
