using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Adjustments.Commands;
using Datacenter.Application.Features.Adjustments.DTOs;
using Datacenter.Application.Features.Leasing.Services;
using Datacenter.Domain.Enums;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Leasing.Commands;

public class GenerateLeaseAdjustmentCommandHandler(IApplicationDbContext db, IMediator mediator)
    : IRequestHandler<GenerateLeaseAdjustmentCommand, AdjustmentEntryDto>
{
    public async Task<AdjustmentEntryDto> Handle(GenerateLeaseAdjustmentCommand request, CancellationToken ct)
    {
        if (request.ContractIds is not { Count: > 0 })
            throw new DomainException("ต้องเลือกสัญญาอย่างน้อย 1 รายการ");

        var contracts = await db.LeaseContracts
            .AsNoTracking()
            .Where(x => x.ClientCompanyId == request.ClientCompanyId
                     && request.ContractIds.Contains(x.Id)
                     && x.IsActive)
            .ToListAsync(ct);

        if (contracts.Count == 0)
            throw new DomainException("ไม่พบสัญญาที่เลือก");

        // รวมยอดเดบิต/เครดิตต่อบัญชี
        var debitByAcc = new Dictionary<int, decimal>();
        var creditByAcc = new Dictionary<int, decimal>();
        var hasHirePurchase = false;
        var contractNos = new List<string>();

        foreach (var c in contracts)
        {
            var schedule = LeaseAmortizationEngine.BuildSchedule(c);
            var ye = LeaseAmortizationEngine.BuildYearEndSummary(schedule, request.FiscalYear);
            var interest = Math.Round(ye.InterestRecognizedInYear, 2);
            if (interest <= 0) continue;

            // Dr ดอกเบี้ยจ่าย
            debitByAcc[c.InterestExpenseAccountId] = debitByAcc.GetValueOrDefault(c.InterestExpenseAccountId) + interest;

            // Cr ดอกเบี้ยรอตัด (เช่าซื้อ) หรือ หนี้สิน (เงินกู้)
            var creditAcc = c.ContractType == LeaseContractType.HirePurchase
                ? c.DeferredInterestAccountId ?? c.LiabilityAccountId
                : c.LiabilityAccountId;
            creditByAcc[creditAcc] = creditByAcc.GetValueOrDefault(creditAcc) + interest;

            if (c.ContractType == LeaseContractType.HirePurchase) hasHirePurchase = true;
            contractNos.Add(c.ContractNo);
        }

        var lines = BuildLines(debitByAcc, creditByAcc, request.FiscalYear);
        if (lines.Count == 0)
            throw new DomainException("ไม่มีดอกเบี้ยที่ต้องรับรู้ในปีนี้สำหรับสัญญาที่เลือก");

        var reference = string.Join(", ", contractNos);
        if (reference.Length > 100) reference = reference[..97] + "...";

        var command = new CreateAdjustmentEntryCommand(
            ClientCompanyId: request.ClientCompanyId,
            FiscalYear: request.FiscalYear,
            EntryDate: request.EntryDate ?? new DateTime(request.FiscalYear, 12, 31),
            SourceType: hasHirePurchase ? AdjustmentSourceType.Leasing : AdjustmentSourceType.Loan,
            Reference: reference,
            Reason: $"บันทึกดอกเบี้ยเช่าซื้อ/เงินกู้รับรู้ในปี {request.FiscalYear} จากกระดาษทำการ ({contractNos.Count} สัญญา)",
            AttachmentPath: null,
            Lines: lines);

        // ส่งผ่าน pipeline เดิม (validate balanced + audit + เลขเอกสาร ADJ-)
        return await mediator.Send(command, ct);
    }

    /// <summary>net เดบิต/เครดิตต่อบัญชี → AdjustmentLineInput (เดบิตหรือเครดิตอย่างใดอย่างหนึ่ง)</summary>
    private static List<AdjustmentLineInput> BuildLines(
        Dictionary<int, decimal> debitByAcc, Dictionary<int, decimal> creditByAcc, int fiscalYear)
    {
        var lines = new List<AdjustmentLineInput>();
        var desc = $"ดอกเบี้ยรับรู้ปี {fiscalYear}";

        foreach (var accId in debitByAcc.Keys.Union(creditByAcc.Keys))
        {
            var net = Math.Round(debitByAcc.GetValueOrDefault(accId) - creditByAcc.GetValueOrDefault(accId), 2);
            if (net == 0) continue;
            lines.Add(net > 0
                ? new AdjustmentLineInput(accId, net, 0m, desc)
                : new AdjustmentLineInput(accId, 0m, -net, desc));
        }

        return lines;
    }
}
