using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Import.DTOs;
using Datacenter.Application.Features.Import.Services;
using Datacenter.Domain.Enums;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Import.Commands;

public class PostImportBatchCommandHandler(
    IApplicationDbContext db,
    ICompanyAccessGuard accessGuard,
    IAuditService audit,
    ICurrentUserService currentUser)
    : IRequestHandler<PostImportBatchCommand, PostImportResultDto>
{
    public async Task<PostImportResultDto> Handle(PostImportBatchCommand request, CancellationToken ct)
    {
        var batch = await db.ImportBatches
            .FirstOrDefaultAsync(b => b.Id == request.ImportBatchId, ct)
            ?? throw new NotFoundException("ImportBatch", request.ImportBatchId);

        // batch อ้างบริษัทผ่าน id จึงตรวจสิทธิ์หลังโหลด entity (ไม่ผ่าน pipeline behaviour)
        await accessGuard.EnsureAccessAsync(batch.ClientCompanyId, ct);

        if (batch.Status != ImportStatus.Success)
            throw new DomainException("ต้องนำเข้าข้อมูลสำเร็จ (ไม่มี error) ก่อนจึงจะ post เข้าระบบบัญชีได้");

        var result = await ExpressPostingService.PostAsync(db, batch, currentUser.Username, ct);

        await audit.LogAsync(
            action: "PostImport",
            entityName: "ImportBatch",
            entityId: batch.Id.ToString(),
            afterValue: $"FY{result.FiscalYear}: accounts={result.AccountsUpserted}, opening={result.OpeningLines}, movement={result.MovementLines}",
            companyId: batch.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);

        return result;
    }
}
