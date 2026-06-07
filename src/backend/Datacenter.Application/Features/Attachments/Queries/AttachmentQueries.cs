using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Attachments.DTOs;
using Datacenter.Application.Features.Attachments.Services;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Attachments.Queries;

// ── รายการเอกสารแนบ (ตามตัวกรอง) ───────────────────────────────────────────────
public record GetAttachmentsQuery(
    int ClientCompanyId,
    int? FiscalYear = null,
    AttachmentCategory? Category = null,
    string? ModuleName = null,
    int? RecordId = null,
    AttachmentVerificationStatus? VerificationStatus = null,
    string? Search = null) : IRequest<List<AttachmentDto>>, IRequireCompanyAccess;

public class GetAttachmentsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAttachmentsQuery, List<AttachmentDto>>
{
    public async Task<List<AttachmentDto>> Handle(GetAttachmentsQuery request, CancellationToken ct)
    {
        var q = db.Attachments.AsNoTracking()
            .Where(a => a.ClientCompanyId == request.ClientCompanyId);

        if (request.FiscalYear is { } fy) q = q.Where(a => a.FiscalYear == fy);
        if (request.Category is { } cat) q = q.Where(a => a.Category == cat);
        if (request.VerificationStatus is { } vs) q = q.Where(a => a.VerificationStatus == vs);
        if (!string.IsNullOrWhiteSpace(request.ModuleName)) q = q.Where(a => a.ModuleName == request.ModuleName);
        if (request.RecordId is { } rid) q = q.Where(a => a.RecordId == rid);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            q = q.Where(a => a.Title.Contains(s) || a.FileName.Contains(s)
                || (a.RecordRef != null && a.RecordRef.Contains(s)));
        }

        var rows = await q.OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
        return rows.Select(AttachmentMapper.ToDto).ToList();
    }
}

// ── ดาวน์โหลดเนื้อไฟล์ (audit ทุกการเข้าถึง) ─────────────────────────────────────
public record GetAttachmentContentQuery(int ClientCompanyId, int Id)
    : IRequest<AttachmentContentDto>, IRequireCompanyAccess;

public class GetAttachmentContentQueryHandler(IApplicationDbContext db, IAuditService audit)
    : IRequestHandler<GetAttachmentContentQuery, AttachmentContentDto>
{
    public async Task<AttachmentContentDto> Handle(GetAttachmentContentQuery request, CancellationToken ct)
    {
        var att = await db.Attachments
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("Attachment", request.Id);

        await audit.LogAsync("DownloadAttachment", "Attachment", entityId: att.Id.ToString(),
            afterValue: $"{att.Category} / {att.FileName}",
            companyId: request.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);

        return new AttachmentContentDto(att.FileName, att.ContentType, att.Content);
    }
}

// ── ตรวจความครบถ้วนของหลักฐานต่อบริษัท+ปีบัญชี ──────────────────────────────────
public record GetEvidenceCompletenessQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<EvidenceCompletenessDto>, IRequireCompanyAccess;

public class GetEvidenceCompletenessQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetEvidenceCompletenessQuery, EvidenceCompletenessDto>
{
    public async Task<EvidenceCompletenessDto> Handle(GetEvidenceCompletenessQuery request, CancellationToken ct)
    {
        // นับเอกสารต่อหมวดของปีนั้น (รวมเอกสารที่ไม่ผูกปี FiscalYear = null ด้วย ถือว่าใช้ได้ทุกปี)
        var rows = await db.Attachments.AsNoTracking()
            .Where(a => a.ClientCompanyId == request.ClientCompanyId
                && (a.FiscalYear == request.FiscalYear || a.FiscalYear == null))
            .Select(a => new { a.Category, a.VerificationStatus })
            .ToListAsync(ct);

        var byCategory = rows.GroupBy(r => r.Category).ToDictionary(
            g => g.Key,
            g => (Count: g.Count(), Verified: g.Count(x => x.VerificationStatus == AttachmentVerificationStatus.Verified)));

        var items = EvidenceChecklist.Items.Select(item =>
        {
            byCategory.TryGetValue(item.Category, out var c);
            return new EvidenceCompletenessItemDto(
                item.Category, item.Label, item.Required, c.Count, c.Verified, c.Count > 0);
        }).ToList();

        var requiredMissing = items.Count(i => i.Required && !i.Present);

        return new EvidenceCompletenessDto(
            FiscalYear: request.FiscalYear,
            IsComplete: requiredMissing == 0,
            TotalAttachments: rows.Count,
            RequiredMissingCount: requiredMissing,
            Items: items);
    }
}
