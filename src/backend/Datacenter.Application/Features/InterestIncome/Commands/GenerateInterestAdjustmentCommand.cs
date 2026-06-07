using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Adjustments.Commands;
using Datacenter.Application.Features.Adjustments.DTOs;
using Datacenter.Application.Features.InterestIncome.Services;
using Datacenter.Domain.Enums;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.InterestIncome.Commands;

/// <summary>
/// สร้างรายการปรับปรุงรับรู้ดอกเบี้ยรับในปีงบจากเงินให้กู้ที่เลือก:
/// Dr ดอกเบี้ยค้างรับ / Cr รายได้ดอกเบี้ย (ต่อบัญชี รวมยอด).
/// (ภาษีธุรกิจเฉพาะ/ส่วนท้องถิ่นแสดงในกระดาษทำการเพื่อนำส่ง ไม่ลงรายการในนี้)
/// </summary>
public record GenerateInterestAdjustmentCommand(
    int ClientCompanyId, int FiscalYear, IReadOnlyList<int> LoanIds, DateTime? EntryDate)
    : IRequest<AdjustmentEntryDto>, IRequireCompanyAccess;

public class GenerateInterestAdjustmentCommandHandler(IApplicationDbContext db, IMediator mediator)
    : IRequestHandler<GenerateInterestAdjustmentCommand, AdjustmentEntryDto>
{
    public async Task<AdjustmentEntryDto> Handle(GenerateInterestAdjustmentCommand request, CancellationToken ct)
    {
        if (request.LoanIds is not { Count: > 0 })
            throw new DomainException("ต้องเลือกเงินให้กู้อย่างน้อย 1 รายการ");

        var loans = await db.InterestBearingLoans.AsNoTracking().Include(x => x.Movements)
            .Where(x => x.ClientCompanyId == request.ClientCompanyId
                     && request.LoanIds.Contains(x.Id) && x.IsActive)
            .ToListAsync(ct);
        if (loans.Count == 0) throw new DomainException("ไม่พบเงินให้กู้ที่เลือก");

        var debitByAcc = new Dictionary<int, decimal>();   // ดอกเบี้ยค้างรับ
        var creditByAcc = new Dictionary<int, decimal>();  // รายได้ดอกเบี้ย
        var names = new List<string>();

        foreach (var loan in loans)
        {
            var asOf = InterestIncomeEngine.AsOf(loan, request.FiscalYear);
            var interest = Math.Round(asOf.InterestForYear, 2);
            if (interest <= 0) continue;
            debitByAcc[loan.InterestReceivableAccountId] = debitByAcc.GetValueOrDefault(loan.InterestReceivableAccountId) + interest;
            creditByAcc[loan.InterestIncomeAccountId] = creditByAcc.GetValueOrDefault(loan.InterestIncomeAccountId) + interest;
            names.Add(loan.Name);
        }

        var lines = new List<AdjustmentLineInput>();
        var desc = $"รับรู้ดอกเบี้ยรับเงินให้กู้ปี {request.FiscalYear}";
        foreach (var accId in debitByAcc.Keys.Union(creditByAcc.Keys))
        {
            var net = Math.Round(debitByAcc.GetValueOrDefault(accId) - creditByAcc.GetValueOrDefault(accId), 2);
            if (net == 0) continue;
            lines.Add(net > 0
                ? new AdjustmentLineInput(accId, net, 0m, desc)
                : new AdjustmentLineInput(accId, 0m, -net, desc));
        }
        if (lines.Count == 0)
            throw new DomainException("ไม่มีดอกเบี้ยรับในปีนี้สำหรับรายการที่เลือก");

        var reference = string.Join(", ", names);
        if (reference.Length > 100) reference = reference[..97] + "...";

        var command = new CreateAdjustmentEntryCommand(
            ClientCompanyId: request.ClientCompanyId,
            FiscalYear: request.FiscalYear,
            EntryDate: request.EntryDate ?? new DateTime(request.FiscalYear, 12, 31),
            SourceType: AdjustmentSourceType.InterestIncome,
            Reference: reference,
            Reason: $"รับรู้ดอกเบี้ยรับเงินให้กู้ในปี {request.FiscalYear} จากกระดาษทำการ ({loans.Count} รายการ)",
            AttachmentPath: null,
            Lines: lines);

        return await mediator.Send(command, ct);
    }
}
