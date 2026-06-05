using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Adjustments.Commands;
using Datacenter.Application.Features.Adjustments.DTOs;
using Datacenter.Application.Features.FixedAssets.Services;
using Datacenter.Domain.Enums;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FixedAssets.Commands;

public class GenerateDisposalAdjustmentCommandHandler(IApplicationDbContext db, IMediator mediator)
    : IRequestHandler<GenerateDisposalAdjustmentCommand, AdjustmentEntryDto>
{
    public async Task<AdjustmentEntryDto> Handle(GenerateDisposalAdjustmentCommand request, CancellationToken ct)
    {
        if (request.AssetIds is not { Count: > 0 })
            throw new DomainException("ต้องเลือกสินทรัพย์อย่างน้อย 1 รายการ");

        var assets = await db.FixedAssets
            .AsNoTracking()
            .Where(x => x.ClientCompanyId == request.ClientCompanyId
                     && request.AssetIds.Contains(x.Id)
                     && x.IsActive)
            .ToListAsync(ct);
        if (assets.Count == 0)
            throw new DomainException("ไม่พบสินทรัพย์ที่เลือก");

        // รวมเดบิต/เครดิตต่อบัญชี
        var debit = new Dictionary<int, decimal>();
        var credit = new Dictionary<int, decimal>();
        var codes = new List<string>();
        var anyProceeds = false;
        decimal totalGain = 0, totalLoss = 0;

        foreach (var a in assets)
        {
            if (a.Status == FixedAssetStatus.Active || a.DisposalDate is not { } dd)
                throw new DomainException($"สินทรัพย์ {a.AssetCode} ยังไม่ได้จำหน่าย/ขาย");
            if (dd.Year != request.FiscalYear)
                continue; // คิดเฉพาะที่จำหน่ายในปีงบนี้
            if (a.AssetAccountId is not { } costAcc || costAcc <= 0)
                throw new DomainException($"สินทรัพย์ {a.AssetCode} ยังไม่ได้ผูกบัญชีราคาทุน (ตั้งที่หน้า \"แมพบัญชี\")");

            var disp = DepreciationEngine.Disposal(a)!;
            var cost = Math.Round(a.Cost, 2);
            var accum = Math.Round(a.Cost - disp.NetBookValueAtDisposal, 2);
            var proceeds = Math.Round(disp.Proceeds, 2);
            var gainLoss = Math.Round(disp.GainLoss, 2);

            // Cr ราคาทุน / Dr ค่าเสื่อมสะสม
            Add(credit, costAcc, cost);
            if (accum != 0) Add(debit, a.AccumDepreciationAccountId, accum);

            // Dr เงินรับ (ราคาขาย)
            if (proceeds > 0)
            {
                if (request.ProceedsAccountId is not { } pAcc || pAcc <= 0)
                    throw new DomainException("มีสินทรัพย์ที่มีราคาขาย — ต้องระบุบัญชีเงินรับ/เงินสด");
                Add(debit, pAcc, proceeds);
                anyProceeds = true;
            }

            // กำไร (Cr) / ขาดทุน (Dr)
            if (gainLoss > 0) { Add(credit, request.GainAccountId, gainLoss); totalGain += gainLoss; }
            else if (gainLoss < 0) { Add(debit, request.LossAccountId, -gainLoss); totalLoss += -gainLoss; }

            codes.Add(a.AssetCode);
        }

        if (codes.Count == 0)
            throw new DomainException($"ไม่มีสินทรัพย์ที่จำหน่าย/ขายในปี {request.FiscalYear} ในรายการที่เลือก");

        var lines = BuildLines(debit, credit);
        if (lines.Count == 0)
            throw new DomainException("ไม่มียอดสำหรับรายการตัดจำหน่าย");

        var reference = string.Join(", ", codes);
        if (reference.Length > 100) reference = reference[..97] + "...";
        var pl = totalGain > 0 && totalLoss > 0 ? $"กำไร {totalGain:N2}/ขาดทุน {totalLoss:N2}"
               : totalGain > 0 ? $"กำไร {totalGain:N2}"
               : totalLoss > 0 ? $"ขาดทุน {totalLoss:N2}" : "ไม่มีกำไร/ขาดทุน";

        var command = new CreateAdjustmentEntryCommand(
            ClientCompanyId: request.ClientCompanyId,
            FiscalYear: request.FiscalYear,
            EntryDate: request.EntryDate ?? new DateTime(request.FiscalYear, 12, 31),
            SourceType: AdjustmentSourceType.FixedAsset,
            Reference: reference,
            Reason: $"ตัดจำหน่าย/ขายสินทรัพย์ปี {request.FiscalYear} ({codes.Count} รายการ; {pl})"
                  + (anyProceeds ? "" : " — ไม่มีราคาขาย"),
            AttachmentPath: null,
            Lines: lines);

        return await mediator.Send(command, ct);
    }

    private static void Add(Dictionary<int, decimal> map, int accId, decimal amount)
        => map[accId] = map.GetValueOrDefault(accId) + amount;

    private static List<AdjustmentLineInput> BuildLines(Dictionary<int, decimal> debit, Dictionary<int, decimal> credit)
    {
        var lines = new List<AdjustmentLineInput>();
        foreach (var accId in debit.Keys.Union(credit.Keys))
        {
            var net = Math.Round(debit.GetValueOrDefault(accId) - credit.GetValueOrDefault(accId), 2);
            if (net == 0) continue;
            lines.Add(net > 0
                ? new AdjustmentLineInput(accId, net, 0m, "ตัดจำหน่าย/ขายสินทรัพย์")
                : new AdjustmentLineInput(accId, 0m, -net, "ตัดจำหน่าย/ขายสินทรัพย์"));
        }
        return lines;
    }
}
