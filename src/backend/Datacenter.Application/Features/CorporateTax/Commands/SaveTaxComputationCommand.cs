using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.CorporateTax.DTOs;
using Datacenter.Application.Features.CorporateTax.Queries;
using Datacenter.Application.Features.CorporateTax.Services;
using Datacenter.Application.Features.FinancialStatement.Commands;
using Datacenter.Application.Features.FinancialStatement.DTOs;
using Datacenter.Application.Features.FinancialStatement.Queries;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.CorporateTax.Commands;

/// <summary>
/// บันทึกกระดาษทำการคำนวณภาษี (upsert header + replace รายการปรับปรุง) แล้ว mirror
/// ภาษีที่คำนวณได้ → FsExternalInput X4 + ภาษีจ่ายล่วงหน้า → WHT เพื่อให้งบดุลลง counterpart (TXP/TXR).
/// </summary>
public record SaveTaxComputationCommand(int ClientCompanyId, int FiscalYear, TaxComputationInput Data)
    : IRequest<TaxComputationDto>, IRequireCompanyAccess;

public class SaveTaxComputationCommandHandler(
    IApplicationDbContext db, ISender sender, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<SaveTaxComputationCommand, TaxComputationDto>
{
    public async Task<TaxComputationDto> Handle(SaveTaxComputationCommand req, CancellationToken ct)
    {
        var data = req.Data;

        var entity = await db.TaxComputations
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.ClientCompanyId == req.ClientCompanyId
                                   && x.FiscalYear == req.FiscalYear, ct);

        bool isNew = entity is null;
        if (entity is null)
        {
            entity = new TaxComputation
            {
                ClientCompanyId = req.ClientCompanyId,
                FiscalYear = req.FiscalYear,
                CreatedBy = currentUser.Username,
            };
            db.TaxComputations.Add(entity);
        }
        else
        {
            entity.ModifiedBy = currentUser.Username;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.Lines.Clear();
        }

        entity.RateScheme = data.RateScheme;
        entity.CustomRatePct = data.RateScheme == TaxRateScheme.Custom ? data.CustomRatePct : null;
        entity.LossBroughtForward = data.LossBroughtForward;
        entity.WhtCredit = data.WhtCredit;
        entity.Note = data.Note;

        var sort = 0;
        foreach (var l in data.Lines)
        {
            entity.Lines.Add(new TaxAdjustmentLine
            {
                Kind = l.Kind,
                Description = l.Description.Trim(),
                Amount = l.Amount,
                SortOrder = l.SortOrder == 0 ? sort++ : l.SortOrder,
                CreatedBy = currentUser.Username,
            });
        }

        await db.SaveChangesAsync(ct);

        // ── คำนวณภาษีด้วยกำไรปัจจุบัน แล้ว mirror → FsExternalInput (X4 + WHT) ──
        decimal profitBeforeTax = 0m;
        try
        {
            ProfitLossDto pl = await sender.Send(
                new GetProfitLossQuery(req.ClientCompanyId, req.FiscalYear), ct);
            profitBeforeTax = pl.ProfitBeforeTax;
        }
        catch { /* ไม่มีงบ → ภาษี 0, ลง X4=0 */ }

        var addBack = data.Lines.Where(l => l.Kind == TaxAdjustmentKind.AddBack).Sum(l => l.Amount);
        var deduction = data.Lines.Where(l => l.Kind == TaxAdjustmentKind.Deduction).Sum(l => l.Amount);

        var result = CorporateTaxEngine.Compute(
            profitBeforeTax, addBack, deduction,
            data.LossBroughtForward, data.WhtCredit, data.RateScheme, data.CustomRatePct);

        await sender.Send(new UpsertExternalInputCommand(
            req.ClientCompanyId, req.FiscalYear, "X4", result.TaxAmount,
            "คำนวณจากกระดาษทำการ ภ.ง.ด.50"), ct);
        await sender.Send(new UpsertExternalInputCommand(
            req.ClientCompanyId, req.FiscalYear, "WHT", data.WhtCredit, null), ct);

        await audit.LogAsync(isNew ? "Create" : "Update", "TaxComputation",
            entityId: $"{req.ClientCompanyId}:{req.FiscalYear}",
            afterValue: $"ภาษี {result.TaxAmount:N2} / เงินได้สุทธิ {result.NetTaxableIncome:N2} / {data.RateScheme}",
            companyId: req.ClientCompanyId, cancellationToken: ct);

        return await sender.Send(new GetTaxComputationQuery(req.ClientCompanyId, req.FiscalYear), ct);
    }
}

public class TaxComputationInputValidator : AbstractValidator<TaxComputationInput>
{
    public TaxComputationInputValidator()
    {
        RuleFor(x => x.RateScheme).IsInEnum();
        RuleFor(x => x.CustomRatePct)
            .InclusiveBetween(0m, 100m).When(x => x.CustomRatePct.HasValue)
            .WithMessage("อัตราภาษีต้องอยู่ระหว่าง 0–100%");
        RuleFor(x => x.CustomRatePct)
            .NotNull().GreaterThan(0m)
            .When(x => x.RateScheme == TaxRateScheme.Custom)
            .WithMessage("เลือกอัตรากำหนดเองต้องระบุอัตราภาษี (%)");
        RuleFor(x => x.LossBroughtForward).GreaterThanOrEqualTo(0m)
            .WithMessage("ผลขาดทุนสะสมยกมาต้องไม่ติดลบ");
        RuleFor(x => x.WhtCredit).GreaterThanOrEqualTo(0m)
            .WithMessage("ภาษีจ่ายล่วงหน้าต้องไม่ติดลบ");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.Kind).IsInEnum();
            l.RuleFor(x => x.Description).NotEmpty().MaximumLength(300)
                .WithMessage("ต้องระบุคำอธิบายรายการปรับปรุง");
            l.RuleFor(x => x.Amount).GreaterThan(0m)
                .WithMessage("จำนวนเงินรายการปรับปรุงต้องมากกว่า 0");
        });
    }
}

public class SaveTaxComputationCommandValidator : AbstractValidator<SaveTaxComputationCommand>
{
    public SaveTaxComputationCommandValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0);
        RuleFor(x => x.FiscalYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Data).NotNull().SetValidator(new TaxComputationInputValidator());
    }
}
