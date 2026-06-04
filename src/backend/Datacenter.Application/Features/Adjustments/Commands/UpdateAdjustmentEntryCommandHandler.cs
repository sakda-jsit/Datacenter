using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Adjustments.DTOs;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Adjustments.Commands;

public class UpdateAdjustmentEntryCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAuditService audit)
    : IRequestHandler<UpdateAdjustmentEntryCommand, AdjustmentEntryDto>
{
    public async Task<AdjustmentEntryDto> Handle(UpdateAdjustmentEntryCommand request, CancellationToken ct)
    {
        var entry = await db.AdjustmentEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("AdjustmentEntry", request.Id);

        var accounts = await AdjustmentAccountGuard.LoadAndValidateAsync(
            db, request.ClientCompanyId, request.Lines, ct);

        // แทนที่ lines ทั้งหมด
        db.AdjustmentEntryLines.RemoveRange(entry.Lines);
        entry.Lines = request.Lines.Select(l => new AdjustmentEntryLine
        {
            AdjustmentEntryId = entry.Id,
            AccountId    = l.AccountId,
            DebitAmount  = l.DebitAmount,
            CreditAmount = l.CreditAmount,
            Description  = l.Description,
            CreatedBy    = currentUser.Username,
        }).ToList();

        entry.EntryDate      = request.EntryDate;
        entry.SourceType     = request.SourceType;
        entry.Reference      = request.Reference;
        entry.Reason         = request.Reason;
        entry.AttachmentPath = request.AttachmentPath;
        entry.ModifiedAt     = DateTime.UtcNow;
        entry.ModifiedBy     = currentUser.Username;

        await audit.LogAsync("Update", "AdjustmentEntry",
            entityId: $"{request.ClientCompanyId}:{entry.FiscalYear}:{entry.DocumentNo}",
            afterValue: $"{entry.Lines.Sum(l => l.DebitAmount):N2} / {request.Reason}",
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);

        return AdjustmentMapper.ToDto(entry, accounts);
    }
}
