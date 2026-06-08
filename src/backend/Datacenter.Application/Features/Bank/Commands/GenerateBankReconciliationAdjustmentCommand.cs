using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Adjustments.Commands;
using Datacenter.Application.Features.Adjustments.DTOs;
using Datacenter.Domain.Enums;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Bank.Commands;

/// <summary>
/// สร้างรายการปรับปรุงจากรายการฝั่ง statement ที่สมุดยังไม่ลง (ค่าธรรมเนียม/ดอกเบี้ย):
/// - เงินเข้า (ดอกเบี้ยรับ): Dr บัญชีธนาคาร / Cr บัญชีรายได้ (counterpart)
/// - เงินออก (ค่าธรรมเนียม): Dr บัญชีค่าใช้จ่าย (counterpart) / Cr บัญชีธนาคาร
/// </summary>
public record GenerateBankReconciliationAdjustmentCommand(
    int ClientCompanyId, int ImportId, int FiscalYear,
    IReadOnlyList<int> StatementLineIds, int BankGlAccountId, int CounterpartAccountId, DateTime? EntryDate)
    : IRequest<AdjustmentEntryDto>, IRequireCompanyAccess;

public class GenerateBankReconciliationAdjustmentCommandHandler(IApplicationDbContext db, IMediator mediator)
    : IRequestHandler<GenerateBankReconciliationAdjustmentCommand, AdjustmentEntryDto>
{
    public async Task<AdjustmentEntryDto> Handle(GenerateBankReconciliationAdjustmentCommand request, CancellationToken ct)
    {
        if (request.StatementLineIds is not { Count: > 0 })
            throw new DomainException("ต้องเลือกรายการอย่างน้อย 1 รายการ");

        var import = await db.BankStatementImports.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.ImportId && i.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("BankStatementImport", request.ImportId);

        var lines = await db.BankStatementLines.AsNoTracking()
            .Where(l => l.BankStatementImportId == request.ImportId && request.StatementLineIds.Contains(l.Id))
            .ToListAsync(ct);
        if (lines.Count == 0) throw new DomainException("ไม่พบรายการที่เลือก");

        decimal deposits = Math.Round(lines.Sum(l => l.Deposit), 2);   // ดอกเบี้ยรับ ฯลฯ
        decimal withdrawals = Math.Round(lines.Sum(l => l.Withdrawal), 2); // ค่าธรรมเนียม ฯลฯ

        // net ต่อบัญชี
        decimal bankNet = deposits - withdrawals;               // เดบิตธนาคารสุทธิ (>0 = Dr)
        decimal counterpartNet = withdrawals - deposits;        // เดบิต counterpart สุทธิ (>0 = Dr)

        var adjLines = new List<AdjustmentLineInput>();
        var desc = $"กระทบยอดธนาคาร {import.BankCode} งวด {import.PeriodEnd:yyyy-MM}";
        if (bankNet != 0)
            adjLines.Add(bankNet > 0
                ? new AdjustmentLineInput(request.BankGlAccountId, bankNet, 0m, desc)
                : new AdjustmentLineInput(request.BankGlAccountId, 0m, -bankNet, desc));
        if (counterpartNet != 0)
            adjLines.Add(counterpartNet > 0
                ? new AdjustmentLineInput(request.CounterpartAccountId, counterpartNet, 0m, desc)
                : new AdjustmentLineInput(request.CounterpartAccountId, 0m, -counterpartNet, desc));

        if (adjLines.Count == 0) throw new DomainException("รายการที่เลือกมียอดสุทธิเป็น 0");

        var command = new CreateAdjustmentEntryCommand(
            ClientCompanyId: request.ClientCompanyId,
            FiscalYear: request.FiscalYear,
            EntryDate: request.EntryDate ?? new DateTime(request.FiscalYear, 12, 31),
            SourceType: AdjustmentSourceType.BankReconciliation,
            Reference: $"{import.BankCode} {import.PeriodStart:yyyy-MM-dd}..{import.PeriodEnd:yyyy-MM-dd}",
            Reason: $"ปรับปรุงจากการกระทบยอดธนาคาร ({lines.Count} รายการ: ดอกเบี้ย/ค่าธรรมเนียมที่สมุดยังไม่ลง)",
            AttachmentPath: null,
            Lines: adjLines);

        return await mediator.Send(command, ct);
    }
}
