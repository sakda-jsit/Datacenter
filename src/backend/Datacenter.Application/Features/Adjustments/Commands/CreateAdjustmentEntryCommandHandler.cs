using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Adjustments.DTOs;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Adjustments.Commands;

public class CreateAdjustmentEntryCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAuditService audit)
    : IRequestHandler<CreateAdjustmentEntryCommand, AdjustmentEntryDto>
{
    public async Task<AdjustmentEntryDto> Handle(CreateAdjustmentEntryCommand request, CancellationToken ct)
    {
        var accounts = await AdjustmentAccountGuard.LoadAndValidateAsync(
            db, request.ClientCompanyId, request.Lines, ct);

        var documentNo = await NextDocumentNoAsync(db, request.ClientCompanyId, request.FiscalYear, ct);

        var entry = new AdjustmentEntry
        {
            ClientCompanyId = request.ClientCompanyId,
            FiscalYear      = request.FiscalYear,
            DocumentNo      = documentNo,
            EntryDate       = request.EntryDate,
            SourceType      = request.SourceType,
            Reference       = request.Reference,
            Reason          = request.Reason,
            AttachmentPath  = request.AttachmentPath,
            CreatedBy       = currentUser.Username,
            Lines = request.Lines.Select(l => new AdjustmentEntryLine
            {
                AccountId    = l.AccountId,
                DebitAmount  = l.DebitAmount,
                CreditAmount = l.CreditAmount,
                Description  = l.Description,
                CreatedBy    = currentUser.Username,
            }).ToList(),
        };

        db.AdjustmentEntries.Add(entry);

        await audit.LogAsync("Create", "AdjustmentEntry",
            entityId: $"{request.ClientCompanyId}:{request.FiscalYear}:{documentNo}",
            afterValue: $"{entry.Lines.Sum(l => l.DebitAmount):N2} / {request.Reason}",
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);

        return AdjustmentMapper.ToDto(entry, accounts);
    }

    /// <summary>เลขเอกสารถัดไป รูปแบบ ADJ-{ปีงบ}-{ลำดับ 4 หลัก} ต่อบริษัท+ปีงบ</summary>
    internal static async Task<string> NextDocumentNoAsync(
        IApplicationDbContext db, int clientCompanyId, int fiscalYear, CancellationToken ct)
    {
        var prefix = $"ADJ-{fiscalYear}-";
        var existing = await db.AdjustmentEntries
            .Where(a => a.ClientCompanyId == clientCompanyId && a.FiscalYear == fiscalYear)
            .Select(a => a.DocumentNo)
            .ToListAsync(ct);

        var maxSeq = existing
            .Where(d => d.StartsWith(prefix))
            .Select(d => int.TryParse(d[prefix.Length..], out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{maxSeq + 1:D4}";
    }
}
