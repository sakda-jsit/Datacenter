using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Payroll.Services;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Commands;

// ── ติดตามสถานะยื่น สปส.1-10 + ใบเสร็จ (SsoMonthlyFiling) ────────────────────────
internal static class SsoFilingTotals
{
    /// <summary>ยอดงวดปัจจุบัน: count ผู้ประกันตน, รวมค่าจ้าง, ลูกจ้าง, นายจ้าง(เท่าลูกจ้าง), รวมส่ง</summary>
    public static (int Count, decimal Wage, decimal Emp, decimal Er, decimal Grand) Compute(PayrollRun run)
    {
        var insured = run.Items.Where(i => i.SsoWageBase > 0).ToList();
        var wage = PayrollCalculator.Round2(insured.Sum(i => i.SsoWageBase));
        var emp = PayrollCalculator.Round2(insured.Sum(i => i.SsoEmployee));
        return (insured.Count, wage, emp, emp, PayrollCalculator.Round2(emp * 2));
    }
}

/// <summary>บันทึก/แก้สถานะการยื่น + ข้อมูลใบเสร็จ (ไม่รวมไฟล์แนบ)</summary>
public record UpsertSsoFilingStatusCommand(
    int ClientCompanyId, int RunId,
    DateTime? SubmittedDate, DateTime? ReceiptDate, decimal? ReceiptAmount,
    string? ReceiptNo, string? Note) : IRequest<int>, IRequireCompanyAccess;

public class UpsertSsoFilingStatusCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpsertSsoFilingStatusCommand, int>
{
    public async Task<int> Handle(UpsertSsoFilingStatusCommand req, CancellationToken ct)
    {
        var run = await db.PayrollRuns.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == req.RunId && r.ClientCompanyId == req.ClientCompanyId, ct)
            ?? throw new NotFoundException("PayrollRun", req.RunId);

        var filing = await db.SsoMonthlyFilings.FirstOrDefaultAsync(f => f.PayrollRunId == req.RunId, ct);
        if (filing is null)
        {
            filing = new SsoMonthlyFiling
            {
                PayrollRunId = run.Id, ClientCompanyId = run.ClientCompanyId, Year = run.Year, Month = run.Month,
                CreatedBy = currentUser.Username,
            };
            db.SsoMonthlyFilings.Add(filing);
        }
        else
        {
            filing.ModifiedBy = currentUser.Username;
            filing.ModifiedAt = DateTime.UtcNow;
        }

        // snapshot ยอด ณ ครั้งแรกที่ระบุวันยื่น (สะท้อน "ยอดที่ยื่นจริง")
        bool firstSubmit = req.SubmittedDate.HasValue && filing.SubmittedDate is null;
        if (firstSubmit)
        {
            var t = SsoFilingTotals.Compute(run);
            filing.SnapshotEmployeeCount = t.Count;
            filing.SnapshotTotalWage = t.Wage;
            filing.SnapshotEmployeeContribution = t.Emp;
            filing.SnapshotEmployerContribution = t.Er;
            filing.SnapshotGrandTotal = t.Grand;
        }

        filing.SubmittedDate = req.SubmittedDate;
        filing.ReceiptDate = req.ReceiptDate;
        filing.ReceiptAmount = req.ReceiptAmount;
        filing.ReceiptNo = req.ReceiptNo;
        filing.Note = req.Note;
        filing.Status =
            (req.ReceiptDate.HasValue || req.ReceiptAmount.HasValue) ? SsoFilingStatus.ReceiptReceived
            : req.SubmittedDate.HasValue ? SsoFilingStatus.Filed
            : SsoFilingStatus.NotFiled;

        await db.SaveChangesAsync(ct);
        return filing.Id;
    }
}

/// <summary>อัปโหลดไฟล์แนบ (kind = form|receipt) เก็บเป็น blob</summary>
public record UploadSsoFilingDocumentCommand(
    int ClientCompanyId, int RunId, string Kind,
    string FileName, string ContentType, byte[] Content) : IRequest<int>, IRequireCompanyAccess;

public class UploadSsoFilingDocumentCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UploadSsoFilingDocumentCommand, int>
{
    public async Task<int> Handle(UploadSsoFilingDocumentCommand req, CancellationToken ct)
    {
        if (req.Content is null || req.Content.Length == 0)
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("file", "ไม่พบไฟล์") });
        var kind = (req.Kind ?? "").ToLowerInvariant();
        if (kind is not ("form" or "receipt"))
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("kind", "kind ต้องเป็น form หรือ receipt") });

        var run = await db.PayrollRuns
            .FirstOrDefaultAsync(r => r.Id == req.RunId && r.ClientCompanyId == req.ClientCompanyId, ct)
            ?? throw new NotFoundException("PayrollRun", req.RunId);

        var filing = await db.SsoMonthlyFilings.FirstOrDefaultAsync(f => f.PayrollRunId == req.RunId, ct);
        if (filing is null)
        {
            filing = new SsoMonthlyFiling
            {
                PayrollRunId = run.Id, ClientCompanyId = run.ClientCompanyId, Year = run.Year, Month = run.Month,
                CreatedBy = currentUser.Username,
            };
            db.SsoMonthlyFilings.Add(filing);
        }
        else { filing.ModifiedBy = currentUser.Username; filing.ModifiedAt = DateTime.UtcNow; }

        if (kind == "form")
        {
            filing.FormFileName = req.FileName;
            filing.FormContentType = req.ContentType;
            filing.FormContent = req.Content;
        }
        else
        {
            filing.ReceiptFileName = req.FileName;
            filing.ReceiptContentType = req.ContentType;
            filing.ReceiptContent = req.Content;
        }

        await db.SaveChangesAsync(ct);
        return filing.Id;
    }
}

public record SsoFilingDocumentResult(string FileName, string ContentType, byte[] Content);

/// <summary>ดาวน์โหลดไฟล์แนบ (kind = form|receipt)</summary>
public record GetSsoFilingDocumentQuery(int ClientCompanyId, int RunId, string Kind)
    : IRequest<SsoFilingDocumentResult>, IRequireCompanyAccess;

public class GetSsoFilingDocumentQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetSsoFilingDocumentQuery, SsoFilingDocumentResult>
{
    public async Task<SsoFilingDocumentResult> Handle(GetSsoFilingDocumentQuery req, CancellationToken ct)
    {
        var filing = await db.SsoMonthlyFilings.AsNoTracking()
            .FirstOrDefaultAsync(f => f.PayrollRunId == req.RunId && f.ClientCompanyId == req.ClientCompanyId, ct)
            ?? throw new NotFoundException("SsoMonthlyFiling", req.RunId);

        var kind = (req.Kind ?? "").ToLowerInvariant();
        var (name, type, content) = kind == "receipt"
            ? (filing.ReceiptFileName, filing.ReceiptContentType, filing.ReceiptContent)
            : (filing.FormFileName, filing.FormContentType, filing.FormContent);

        if (content is null || content.Length == 0)
            throw new NotFoundException("SsoFilingDocument", $"{req.RunId}/{kind}");

        return new SsoFilingDocumentResult(name ?? "document", type ?? "application/octet-stream", content);
    }
}
