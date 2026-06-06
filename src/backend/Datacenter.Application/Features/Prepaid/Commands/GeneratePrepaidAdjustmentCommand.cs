using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Adjustments.Commands;
using Datacenter.Application.Features.Adjustments.DTOs;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Prepaid.Services;
using Datacenter.Domain.Enums;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Prepaid.Commands;

/// <summary>
/// สร้างรายการปรับปรุง (AdjustmentEntry) จากค่าใช้จ่ายที่ตัดจ่ายในปีงบ:
/// Dr ค่าใช้จ่าย / Cr ค่าใช้จ่ายจ่ายล่วงหน้า (ต่อบัญชี รวมยอด)
/// </summary>
public record GeneratePrepaidAdjustmentCommand(
    int ClientCompanyId, int FiscalYear, IReadOnlyList<int> PrepaidIds, DateTime? EntryDate)
    : IRequest<AdjustmentEntryDto>, IRequireCompanyAccess;

public class GeneratePrepaidAdjustmentCommandHandler(IApplicationDbContext db, IMediator mediator)
    : IRequestHandler<GeneratePrepaidAdjustmentCommand, AdjustmentEntryDto>
{
    public async Task<AdjustmentEntryDto> Handle(GeneratePrepaidAdjustmentCommand request, CancellationToken ct)
    {
        if (request.PrepaidIds is not { Count: > 0 })
            throw new DomainException("ต้องเลือกรายการอย่างน้อย 1 รายการ");

        var items = await db.PrepaidExpenses.AsNoTracking()
            .Where(x => x.ClientCompanyId == request.ClientCompanyId
                     && request.PrepaidIds.Contains(x.Id) && x.IsActive)
            .ToListAsync(ct);
        if (items.Count == 0) throw new DomainException("ไม่พบรายการที่เลือก");

        var debitByAcc = new Dictionary<int, decimal>();   // ค่าใช้จ่าย
        var creditByAcc = new Dictionary<int, decimal>();  // ค่าใช้จ่ายจ่ายล่วงหน้า
        var names = new List<string>();

        foreach (var p in items)
        {
            var asOf = PrepaidAmortizationEngine.AsOf(p, request.FiscalYear);
            var charge = Math.Round(asOf.Charge, 2);
            if (charge <= 0) continue;
            debitByAcc[p.ExpenseAccountId] = debitByAcc.GetValueOrDefault(p.ExpenseAccountId) + charge;
            creditByAcc[p.PrepaidAccountId] = creditByAcc.GetValueOrDefault(p.PrepaidAccountId) + charge;
            names.Add(p.Name);
        }

        var lines = new List<AdjustmentLineInput>();
        var desc = $"ตัดจ่ายค่าใช้จ่ายล่วงหน้าปี {request.FiscalYear}";
        foreach (var accId in debitByAcc.Keys.Union(creditByAcc.Keys))
        {
            var net = Math.Round(debitByAcc.GetValueOrDefault(accId) - creditByAcc.GetValueOrDefault(accId), 2);
            if (net == 0) continue;
            lines.Add(net > 0
                ? new AdjustmentLineInput(accId, net, 0m, desc)
                : new AdjustmentLineInput(accId, 0m, -net, desc));
        }
        if (lines.Count == 0)
            throw new DomainException("ไม่มียอดตัดจ่ายในปีนี้สำหรับรายการที่เลือก");

        var reference = string.Join(", ", names);
        if (reference.Length > 100) reference = reference[..97] + "...";

        var command = new CreateAdjustmentEntryCommand(
            ClientCompanyId: request.ClientCompanyId,
            FiscalYear: request.FiscalYear,
            EntryDate: request.EntryDate ?? new DateTime(request.FiscalYear, 12, 31),
            SourceType: AdjustmentSourceType.Prepaid,
            Reference: reference,
            Reason: $"ตัดจ่ายค่าใช้จ่ายล่วงหน้ารับรู้ในปี {request.FiscalYear} จากกระดาษทำการ ({items.Count} รายการ)",
            AttachmentPath: null,
            Lines: lines);

        return await mediator.Send(command, ct);
    }
}
