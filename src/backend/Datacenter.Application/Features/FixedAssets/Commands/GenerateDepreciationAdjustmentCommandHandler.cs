using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Adjustments.Commands;
using Datacenter.Application.Features.Adjustments.DTOs;
using Datacenter.Application.Features.FixedAssets.Services;
using Datacenter.Domain.Enums;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FixedAssets.Commands;

public class GenerateDepreciationAdjustmentCommandHandler(IApplicationDbContext db, IMediator mediator)
    : IRequestHandler<GenerateDepreciationAdjustmentCommand, AdjustmentEntryDto>
{
    public async Task<AdjustmentEntryDto> Handle(GenerateDepreciationAdjustmentCommand request, CancellationToken ct)
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

        // รวมค่าเสื่อมงวด: Dr ค่าเสื่อมราคา (expense) / Cr ค่าเสื่อมราคาสะสม (accum) ต่อบัญชี
        var debitByAcc = new Dictionary<int, decimal>();   // ค่าเสื่อมราคา
        var creditByAcc = new Dictionary<int, decimal>();  // ค่าเสื่อมราคาสะสม
        var assetCodes = new List<string>();

        foreach (var a in assets)
        {
            var rate = request.Set == DepreciationSet.Tax ? a.TaxRatePct : a.BookRatePct;
            var charge = Math.Round(DepreciationEngine.AsOf(a, rate, request.FiscalYear).Charge, 2);
            if (charge <= 0) continue;

            debitByAcc[a.DepreciationExpenseAccountId] = debitByAcc.GetValueOrDefault(a.DepreciationExpenseAccountId) + charge;
            creditByAcc[a.AccumDepreciationAccountId] = creditByAcc.GetValueOrDefault(a.AccumDepreciationAccountId) + charge;
            assetCodes.Add(a.AssetCode);
        }

        var lines = BuildLines(debitByAcc, creditByAcc, request.FiscalYear);
        if (lines.Count == 0)
            throw new DomainException("ไม่มีค่าเสื่อมที่ต้องรับรู้ในปีนี้สำหรับสินทรัพย์ที่เลือก");

        var setLabel = request.Set == DepreciationSet.Tax ? "ภาษี" : "บัญชี";
        var reference = string.Join(", ", assetCodes);
        if (reference.Length > 100) reference = reference[..97] + "...";

        var command = new CreateAdjustmentEntryCommand(
            ClientCompanyId: request.ClientCompanyId,
            FiscalYear: request.FiscalYear,
            EntryDate: request.EntryDate ?? new DateTime(request.FiscalYear, 12, 31),
            SourceType: AdjustmentSourceType.FixedAsset,
            Reference: reference,
            Reason: $"บันทึกค่าเสื่อมราคา (ชุด{setLabel}) รับรู้ในปี {request.FiscalYear} จากทะเบียนสินทรัพย์ ({assetCodes.Count} รายการ)",
            AttachmentPath: null,
            Lines: lines);

        return await mediator.Send(command, ct);
    }

    /// <summary>net เดบิต/เครดิตต่อบัญชี → AdjustmentLineInput</summary>
    private static List<AdjustmentLineInput> BuildLines(
        Dictionary<int, decimal> debitByAcc, Dictionary<int, decimal> creditByAcc, int fiscalYear)
    {
        var lines = new List<AdjustmentLineInput>();
        var desc = $"ค่าเสื่อมราคาปี {fiscalYear}";

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
