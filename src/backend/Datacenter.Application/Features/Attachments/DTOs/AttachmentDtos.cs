using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.Attachments.DTOs;

/// <summary>metadata ของเอกสารแนบหนึ่งไฟล์ (ไม่รวมเนื้อไฟล์).</summary>
public record AttachmentDto(
    int Id,
    int ClientCompanyId,
    AttachmentCategory Category,
    int? FiscalYear,
    string? ModuleName,
    int? RecordId,
    string? RecordRef,
    string Title,
    string FileName,
    string ContentType,
    long ByteSize,
    string Sha256,
    DateTime? DocumentDate,
    AttachmentVerificationStatus VerificationStatus,
    string? VerifiedBy,
    DateTime? VerifiedAt,
    string? Note,
    string CreatedBy,
    DateTime CreatedAt);

/// <summary>เนื้อไฟล์สำหรับดาวน์โหลด.</summary>
public record AttachmentContentDto(string FileName, string ContentType, byte[] Content);

/// <summary>ข้อมูลแก้ไข metadata (ไม่แตะเนื้อไฟล์).</summary>
public record AttachmentMetadataInput(
    AttachmentCategory Category,
    int? FiscalYear,
    string? RecordRef,
    string Title,
    DateTime? DocumentDate,
    string? Note);

/// <summary>ผลการตรวจความครบถ้วนของหลักฐานต่อบริษัท+ปีบัญชี (docs/18: completeness check ก่อนปิดงบ).</summary>
public record EvidenceCompletenessDto(
    int FiscalYear,
    bool IsComplete,
    int TotalAttachments,
    int RequiredMissingCount,
    IReadOnlyList<EvidenceCompletenessItemDto> Items);

/// <summary>หนึ่งหมวดใน checklist หลักฐาน.</summary>
public record EvidenceCompletenessItemDto(
    AttachmentCategory Category,
    string Label,
    bool Required,
    int Count,
    int VerifiedCount,
    bool Present);
