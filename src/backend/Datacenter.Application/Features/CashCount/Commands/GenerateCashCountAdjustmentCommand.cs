using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Adjustments.Commands;
using Datacenter.Application.Features.Adjustments.DTOs;
using Datacenter.Domain.Enums;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.CashCount.Commands;

/// <summary>
/// สร้างรายการปรับปรุงเงินสดขาด/เกิน จากใบตรวจนับที่เลือก:
/// ปรับยอดบัญชีเงินสดใน GL ให้เท่ายอดนับจริง โดยลงคู่กับบัญชีเงินสดขาด/เกิน (counterpart)
/// - นับจริง &gt; GL (เงินเกิน) → Dr เงินสด / Cr counterpart
/// - นับจริง &lt; GL (เงินขาด) → Dr counterpart / Cr เงินสด
/// </summary>
public record GenerateCashCountAdjustmentCommand(
    int ClientCompanyId, int FiscalYear, IReadOnlyList<int> CashCountIds, int CounterpartAccountId, DateTime? EntryDate)
    : IRequest<AdjustmentEntryDto>, IRequireCompanyAccess;

public class GenerateCashCountAdjustmentCommandHandler(IApplicationDbContext db, IMediator mediator)
    : IRequestHandler<GenerateCashCountAdjustmentCommand, AdjustmentEntryDto>
{
    public async Task<AdjustmentEntryDto> Handle(GenerateCashCountAdjustmentCommand request, CancellationToken ct)
    {
        if (request.CashCountIds is not { Count: > 0 })
            throw new DomainException("ต้องเลือกใบตรวจนับอย่างน้อย 1 รายการ");
        if (request.CounterpartAccountId <= 0)
            throw new DomainException("ต้องระบุบัญชีเงินสดขาด/เกิน (คู่บัญชี)");

        await CashCountAccountGuard.LoadAndValidateAsync(db, request.ClientCompanyId, new[] { request.CounterpartAccountId }, ct);

        var sheets = await db.CashCounts.AsNoTracking().Include(x => x.Lines)
            .Where(x => x.ClientCompanyId == request.ClientCompanyId
                     && request.CashCountIds.Contains(x.Id) && x.IsActive)
            .ToListAsync(ct);
        if (sheets.Count == 0) throw new DomainException("ไม่พบใบตรวจนับที่เลือก");

        // รวมยอดนับจริงตามบัญชีเงินสด
        var countedByAcc = new Dictionary<int, decimal>();
        foreach (var s in sheets)
            countedByAcc[s.CashAccountId] = countedByAcc.GetValueOrDefault(s.CashAccountId) + CashCountMapper.CountedTotal(s);

        // ยอด GL สะสมถึงสิ้นปีงบ (debit − credit) ของบัญชีเงินสด
        var accIds = countedByAcc.Keys.ToList();
        var yearEndExclusive = new DateTime(request.FiscalYear, 12, 31).AddDays(1);
        var glNet = await db.JournalEntryLines.AsNoTracking()
            .Where(l => l.JournalEntry.ClientCompanyId == request.ClientCompanyId
                     && l.JournalEntry.JournalDate < yearEndExclusive
                     && accIds.Contains(l.AccountId))
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.DebitAmount), Credit = g.Sum(x => x.CreditAmount) })
            .ToDictionaryAsync(x => x.AccountId, ct);

        var lines = new List<AdjustmentLineInput>();
        decimal counterpartNet = 0m; // + = ต้องลง credit counterpart, − = debit counterpart
        var desc = $"ปรับปรุงเงินสดขาด/เกินจากการตรวจนับปี {request.FiscalYear}";

        foreach (var (accId, counted) in countedByAcc)
        {
            var net = glNet.GetValueOrDefault(accId);
            var glClosing = Math.Round((net?.Debit ?? 0m) - (net?.Credit ?? 0m), 2);
            var diff = Math.Round(counted - glClosing, 2); // >0 เกิน, <0 ขาด
            if (diff == 0) continue;

            lines.Add(diff > 0
                ? new AdjustmentLineInput(accId, diff, 0m, desc)   // เงินเกิน → Dr เงินสด
                : new AdjustmentLineInput(accId, 0m, -diff, desc)); // เงินขาด → Cr เงินสด
            counterpartNet += diff;
        }

        if (lines.Count == 0)
            throw new DomainException("ยอดนับจริงตรงกับ GL ทุกบัญชี ไม่มีรายการปรับปรุง");

        // คู่บัญชีเงินสดขาด/เกิน (ลงตรงข้ามกับผลรวม diff ให้สมดุล)
        var cp = Math.Round(counterpartNet, 2);
        lines.Add(cp > 0
            ? new AdjustmentLineInput(request.CounterpartAccountId, 0m, cp, desc)   // เกินสุทธิ → Cr (รายได้เงินเกิน)
            : new AdjustmentLineInput(request.CounterpartAccountId, -cp, 0m, desc)); // ขาดสุทธิ → Dr (ค่าใช้จ่ายเงินขาด)

        var command = new CreateAdjustmentEntryCommand(
            ClientCompanyId: request.ClientCompanyId,
            FiscalYear: request.FiscalYear,
            EntryDate: request.EntryDate ?? new DateTime(request.FiscalYear, 12, 31),
            SourceType: AdjustmentSourceType.CashCount,
            Reference: $"ตรวจนับเงินสด {sheets.Count} ใบ",
            Reason: $"ปรับปรุงเงินสดขาด/เกินจากการตรวจนับปี {request.FiscalYear} ({sheets.Count} ใบ)",
            AttachmentPath: null,
            Lines: lines);

        return await mediator.Send(command, ct);
    }
}
