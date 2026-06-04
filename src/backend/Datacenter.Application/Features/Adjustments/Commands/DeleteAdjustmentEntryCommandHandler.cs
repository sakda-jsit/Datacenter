using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Adjustments.Commands;

public class DeleteAdjustmentEntryCommandHandler(
    IApplicationDbContext db,
    IAuditService audit)
    : IRequestHandler<DeleteAdjustmentEntryCommand>
{
    public async Task Handle(DeleteAdjustmentEntryCommand request, CancellationToken ct)
    {
        var entry = await db.AdjustmentEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("AdjustmentEntry", request.Id);

        var total = entry.Lines.Sum(l => l.DebitAmount);

        db.AdjustmentEntryLines.RemoveRange(entry.Lines);
        db.AdjustmentEntries.Remove(entry);

        await audit.LogAsync("Delete", "AdjustmentEntry",
            entityId: $"{entry.ClientCompanyId}:{entry.FiscalYear}:{entry.DocumentNo}",
            beforeValue: $"{total:N2} / {entry.Reason}",
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);
    }
}
