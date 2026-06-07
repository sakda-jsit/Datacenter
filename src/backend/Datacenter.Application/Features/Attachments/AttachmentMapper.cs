using Datacenter.Application.Features.Attachments.DTOs;
using Datacenter.Domain.Entities;

namespace Datacenter.Application.Features.Attachments;

public static class AttachmentMapper
{
    public static AttachmentDto ToDto(Attachment a) => new(
        a.Id,
        a.ClientCompanyId,
        a.Category,
        a.FiscalYear,
        a.ModuleName,
        a.RecordId,
        a.RecordRef,
        a.Title,
        a.FileName,
        a.ContentType,
        a.ByteSize,
        a.Sha256,
        a.DocumentDate,
        a.VerificationStatus,
        a.VerifiedBy,
        a.VerifiedAt,
        a.Note,
        a.CreatedBy,
        a.CreatedAt);
}
