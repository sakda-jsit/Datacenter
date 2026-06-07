using System.Security.Cryptography;
using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Attachments.Commands;

// ── อัปโหลดเอกสารแนบ ───────────────────────────────────────────────────────────
public record UploadAttachmentCommand(
    int ClientCompanyId,
    AttachmentCategory Category,
    int? FiscalYear,
    string? ModuleName,
    int? RecordId,
    string? RecordRef,
    string Title,
    string FileName,
    string ContentType,
    byte[] Content,
    DateTime? DocumentDate,
    string? Note) : IRequest<int>, IRequireCompanyAccess;

public class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    public UploadAttachmentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("กรุณาระบุหัวข้อเอกสาร").MaximumLength(200);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.Content).Must(c => c is { Length: > 0 }).WithMessage("ไฟล์ว่าง");
        RuleFor(x => x.RecordRef).MaximumLength(100);
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

public class UploadAttachmentCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<UploadAttachmentCommand, int>
{
    public async Task<int> Handle(UploadAttachmentCommand request, CancellationToken ct)
    {
        var sha = Convert.ToHexString(SHA256.HashData(request.Content)).ToLowerInvariant();

        var att = new Attachment
        {
            ClientCompanyId = request.ClientCompanyId,
            Category = request.Category,
            FiscalYear = request.FiscalYear,
            ModuleName = string.IsNullOrWhiteSpace(request.ModuleName) ? null : request.ModuleName.Trim(),
            RecordId = request.RecordId,
            RecordRef = string.IsNullOrWhiteSpace(request.RecordRef) ? null : request.RecordRef.Trim(),
            Title = request.Title.Trim(),
            FileName = request.FileName,
            ContentType = string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType,
            Content = request.Content,
            ByteSize = request.Content.LongLength,
            Sha256 = sha,
            DocumentDate = request.DocumentDate,
            VerificationStatus = AttachmentVerificationStatus.Pending,
            Note = request.Note,
            CreatedBy = currentUser.Username,
        };
        db.Attachments.Add(att);

        await audit.LogAsync("UploadAttachment", "Attachment",
            entityId: $"{request.ModuleName ?? "company"}:{request.RecordId?.ToString() ?? "-"}",
            afterValue: $"{request.Category} / {request.Title} / {request.FileName}",
            companyId: request.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);
        return att.Id;
    }
}

// ── แก้ไข metadata (ไม่แตะเนื้อไฟล์) ────────────────────────────────────────────
public record UpdateAttachmentMetadataCommand(
    int ClientCompanyId,
    int Id,
    AttachmentCategory Category,
    int? FiscalYear,
    string? RecordRef,
    string Title,
    DateTime? DocumentDate,
    string? Note) : IRequest<Unit>, IRequireCompanyAccess;

public class UpdateAttachmentMetadataCommandValidator : AbstractValidator<UpdateAttachmentMetadataCommand>
{
    public UpdateAttachmentMetadataCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("กรุณาระบุหัวข้อเอกสาร").MaximumLength(200);
        RuleFor(x => x.RecordRef).MaximumLength(100);
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

public class UpdateAttachmentMetadataCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<UpdateAttachmentMetadataCommand, Unit>
{
    public async Task<Unit> Handle(UpdateAttachmentMetadataCommand request, CancellationToken ct)
    {
        var att = await Find(db, request.Id, request.ClientCompanyId, ct);
        var before = $"{att.Category} / {att.Title}";

        att.Category = request.Category;
        att.FiscalYear = request.FiscalYear;
        att.RecordRef = string.IsNullOrWhiteSpace(request.RecordRef) ? null : request.RecordRef.Trim();
        att.Title = request.Title.Trim();
        att.DocumentDate = request.DocumentDate;
        att.Note = request.Note;
        att.ModifiedAt = DateTime.UtcNow;
        att.ModifiedBy = currentUser.Username;

        await audit.LogAsync("UpdateAttachment", "Attachment", entityId: att.Id.ToString(),
            beforeValue: before, afterValue: $"{att.Category} / {att.Title}",
            companyId: request.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    internal static async Task<Attachment> Find(IApplicationDbContext db, int id, int companyId, CancellationToken ct)
        => await db.Attachments.FirstOrDefaultAsync(a => a.Id == id && a.ClientCompanyId == companyId, ct)
           ?? throw new NotFoundException("Attachment", id);
}

// ── ตั้งสถานะตรวจสอบ ───────────────────────────────────────────────────────────
public record SetAttachmentVerificationCommand(
    int ClientCompanyId, int Id, AttachmentVerificationStatus Status, string? Note)
    : IRequest<Unit>, IRequireCompanyAccess;

public class SetAttachmentVerificationCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<SetAttachmentVerificationCommand, Unit>
{
    public async Task<Unit> Handle(SetAttachmentVerificationCommand request, CancellationToken ct)
    {
        var att = await UpdateAttachmentMetadataCommandHandler.Find(db, request.Id, request.ClientCompanyId, ct);
        var before = att.VerificationStatus.ToString();

        att.VerificationStatus = request.Status;
        if (request.Status == AttachmentVerificationStatus.Pending)
        {
            att.VerifiedBy = null;
            att.VerifiedAt = null;
        }
        else
        {
            att.VerifiedBy = currentUser.Username;
            att.VerifiedAt = DateTime.UtcNow;
        }
        if (!string.IsNullOrWhiteSpace(request.Note)) att.Note = request.Note;
        att.ModifiedAt = DateTime.UtcNow;
        att.ModifiedBy = currentUser.Username;

        await audit.LogAsync("VerifyAttachment", "Attachment", entityId: att.Id.ToString(),
            beforeValue: before, afterValue: request.Status.ToString(),
            companyId: request.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ── ลบเอกสาร ───────────────────────────────────────────────────────────────────
public record DeleteAttachmentCommand(int ClientCompanyId, int Id) : IRequest<Unit>, IRequireCompanyAccess;

public class DeleteAttachmentCommandHandler(IApplicationDbContext db, IAuditService audit)
    : IRequestHandler<DeleteAttachmentCommand, Unit>
{
    public async Task<Unit> Handle(DeleteAttachmentCommand request, CancellationToken ct)
    {
        var att = await UpdateAttachmentMetadataCommandHandler.Find(db, request.Id, request.ClientCompanyId, ct);
        db.Attachments.Remove(att);

        await audit.LogAsync("DeleteAttachment", "Attachment", entityId: att.Id.ToString(),
            beforeValue: $"{att.Category} / {att.Title} / {att.FileName}",
            companyId: request.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
