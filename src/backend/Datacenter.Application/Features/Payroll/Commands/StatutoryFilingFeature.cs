using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Payroll.DTOs;
using Datacenter.Application.Features.Payroll.Queries;
using Datacenter.Application.Features.Payroll.Services;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Commands;

// ── ติดตามสถานะยื่น ภ.ง.ด.1 (รายเดือน) / ภ.ง.ด.1ก / กท.20ก (รายปี) ───────────────
internal static class StatutoryFilingCurrent
{
    /// <summary>ยอดงวดปัจจุบันตามชนิดแบบ → (Base, Amount, Count). คืน 0 ถ้ายังไม่มีข้อมูล</summary>
    public static async Task<(decimal Base, decimal Amount, int Count)> ComputeAsync(
        ISender sender, IApplicationDbContext db, int companyId, StatutoryFilingType type, int year, int month, CancellationToken ct)
    {
        try
        {
            switch (type)
            {
                case StatutoryFilingType.Pnd1k:
                {
                    var d = await sender.Send(new GetPnd1kQuery(companyId, year), ct);
                    return (d.TotalIncome, d.TotalTax, d.PersonCount);
                }
                case StatutoryFilingType.Kt20:
                {
                    var d = await sender.Send(new GetKt20Query(companyId, year), ct);
                    return (d.TotalWage, d.Contribution, d.EmployeeCount);
                }
                case StatutoryFilingType.Pnd1:
                {
                    var run = await db.PayrollRuns.AsNoTracking().Include(r => r.Items)
                        .FirstOrDefaultAsync(r => r.ClientCompanyId == companyId && r.Year == year && r.Month == month, ct);
                    if (run is null) return (0, 0, 0);
                    var income = PayrollCalculator.Round2(run.Items.Sum(i => i.GrossIncome - i.Absence));
                    var tax = PayrollCalculator.Round2(run.Items.Sum(i => i.WithholdingTax));
                    return (income, tax, run.Items.Count(i => i.WithholdingTax > 0));
                }
                default: return (0, 0, 0);
            }
        }
        catch (NotFoundException) { return (0, 0, 0); }
    }
}

public record GetStatutoryFilingQuery(int ClientCompanyId, int FilingType, int Year, int Month)
    : IRequest<StatutoryFilingDto>, IRequireCompanyAccess;

public class GetStatutoryFilingQueryHandler(IApplicationDbContext db, ISender sender)
    : IRequestHandler<GetStatutoryFilingQuery, StatutoryFilingDto>
{
    public async Task<StatutoryFilingDto> Handle(GetStatutoryFilingQuery req, CancellationToken ct)
    {
        var type = (StatutoryFilingType)req.FilingType;
        var (curBase, curAmount, curCount) =
            await StatutoryFilingCurrent.ComputeAsync(sender, db, req.ClientCompanyId, type, req.Year, req.Month, ct);

        var f = await db.StatutoryFilings.AsNoTracking().FirstOrDefaultAsync(x =>
            x.ClientCompanyId == req.ClientCompanyId && x.FilingType == type &&
            x.Year == req.Year && x.Month == req.Month, ct);

        if (f is null)
            return new StatutoryFilingDto(req.FilingType, req.Year, req.Month, (int)FilingStatus.NotFiled,
                null, null, null, null, null, false, false, 0, 0, 0,
                curBase, curAmount, curCount, false, false);

        return new StatutoryFilingDto(
            req.FilingType, req.Year, req.Month, (int)f.Status,
            f.SubmittedDate, f.ReceiptDate, f.ReceiptAmount, f.ReceiptNo, f.Note,
            HasForm: f.FormContent is { Length: > 0 }, HasReceipt: f.ReceiptContent is { Length: > 0 },
            f.SnapshotBase, f.SnapshotAmount, f.SnapshotCount,
            curBase, curAmount, curCount,
            AmountMatch: f.SubmittedDate == null || Math.Abs(f.SnapshotAmount - curAmount) < 0.05m,
            ReceiptMatch: f.ReceiptAmount.HasValue && Math.Abs(f.ReceiptAmount.Value - curAmount) < 0.05m);
    }
}

public record UpsertStatutoryFilingStatusCommand(
    int ClientCompanyId, int FilingType, int Year, int Month,
    DateTime? SubmittedDate, DateTime? ReceiptDate, decimal? ReceiptAmount, string? ReceiptNo, string? Note)
    : IRequest<int>, IRequireCompanyAccess;

public class UpsertStatutoryFilingStatusCommandHandler(IApplicationDbContext db, ISender sender, ICurrentUserService currentUser)
    : IRequestHandler<UpsertStatutoryFilingStatusCommand, int>
{
    public async Task<int> Handle(UpsertStatutoryFilingStatusCommand req, CancellationToken ct)
    {
        _ = await db.ClientCompanies.FirstOrDefaultAsync(c => c.Id == req.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", req.ClientCompanyId);
        var type = (StatutoryFilingType)req.FilingType;

        var f = await db.StatutoryFilings.FirstOrDefaultAsync(x =>
            x.ClientCompanyId == req.ClientCompanyId && x.FilingType == type &&
            x.Year == req.Year && x.Month == req.Month, ct);
        if (f is null)
        {
            f = new StatutoryFiling
            {
                ClientCompanyId = req.ClientCompanyId, FilingType = type, Year = req.Year, Month = req.Month,
                CreatedBy = currentUser.Username,
            };
            db.StatutoryFilings.Add(f);
        }
        else { f.ModifiedBy = currentUser.Username; f.ModifiedAt = DateTime.UtcNow; }

        if (req.SubmittedDate.HasValue && f.SubmittedDate is null)
        {
            var (b, a, c) = await StatutoryFilingCurrent.ComputeAsync(sender, db, req.ClientCompanyId, type, req.Year, req.Month, ct);
            f.SnapshotBase = b; f.SnapshotAmount = a; f.SnapshotCount = c;
        }

        f.SubmittedDate = req.SubmittedDate;
        f.ReceiptDate = req.ReceiptDate;
        f.ReceiptAmount = req.ReceiptAmount;
        f.ReceiptNo = req.ReceiptNo;
        f.Note = req.Note;
        f.Status =
            (req.ReceiptDate.HasValue || req.ReceiptAmount.HasValue) ? FilingStatus.ReceiptReceived
            : req.SubmittedDate.HasValue ? FilingStatus.Filed
            : FilingStatus.NotFiled;

        await db.SaveChangesAsync(ct);
        return f.Id;
    }
}

public record UploadStatutoryFilingDocumentCommand(
    int ClientCompanyId, int FilingType, int Year, int Month, string Kind,
    string FileName, string ContentType, byte[] Content) : IRequest<int>, IRequireCompanyAccess;

public class UploadStatutoryFilingDocumentCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UploadStatutoryFilingDocumentCommand, int>
{
    public async Task<int> Handle(UploadStatutoryFilingDocumentCommand req, CancellationToken ct)
    {
        if (req.Content is null || req.Content.Length == 0)
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("file", "ไม่พบไฟล์") });
        var kind = (req.Kind ?? "").ToLowerInvariant();
        if (kind is not ("form" or "receipt"))
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("kind", "kind ต้องเป็น form หรือ receipt") });

        _ = await db.ClientCompanies.FirstOrDefaultAsync(c => c.Id == req.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", req.ClientCompanyId);
        var type = (StatutoryFilingType)req.FilingType;

        var f = await db.StatutoryFilings.FirstOrDefaultAsync(x =>
            x.ClientCompanyId == req.ClientCompanyId && x.FilingType == type &&
            x.Year == req.Year && x.Month == req.Month, ct);
        if (f is null)
        {
            f = new StatutoryFiling
            {
                ClientCompanyId = req.ClientCompanyId, FilingType = type, Year = req.Year, Month = req.Month,
                CreatedBy = currentUser.Username,
            };
            db.StatutoryFilings.Add(f);
        }
        else { f.ModifiedBy = currentUser.Username; f.ModifiedAt = DateTime.UtcNow; }

        if (kind == "form") { f.FormFileName = req.FileName; f.FormContentType = req.ContentType; f.FormContent = req.Content; }
        else { f.ReceiptFileName = req.FileName; f.ReceiptContentType = req.ContentType; f.ReceiptContent = req.Content; }

        await db.SaveChangesAsync(ct);
        return f.Id;
    }
}

public record GetStatutoryFilingDocumentQuery(int ClientCompanyId, int FilingType, int Year, int Month, string Kind)
    : IRequest<SsoFilingDocumentResult>, IRequireCompanyAccess;

public class GetStatutoryFilingDocumentQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetStatutoryFilingDocumentQuery, SsoFilingDocumentResult>
{
    public async Task<SsoFilingDocumentResult> Handle(GetStatutoryFilingDocumentQuery req, CancellationToken ct)
    {
        var type = (StatutoryFilingType)req.FilingType;
        var f = await db.StatutoryFilings.AsNoTracking().FirstOrDefaultAsync(x =>
            x.ClientCompanyId == req.ClientCompanyId && x.FilingType == type &&
            x.Year == req.Year && x.Month == req.Month, ct)
            ?? throw new NotFoundException("StatutoryFiling", $"{req.FilingType}/{req.Year}/{req.Month}");

        var kind = (req.Kind ?? "").ToLowerInvariant();
        var (name, ctype, content) = kind == "receipt"
            ? (f.ReceiptFileName, f.ReceiptContentType, f.ReceiptContent)
            : (f.FormFileName, f.FormContentType, f.FormContent);
        if (content is null || content.Length == 0)
            throw new NotFoundException("StatutoryFilingDocument", $"{req.FilingType}/{kind}");

        return new SsoFilingDocumentResult(name ?? "document", ctype ?? "application/octet-stream", content);
    }
}
