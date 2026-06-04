using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Adjustments.DTOs;
using Datacenter.Domain.Enums;
using MediatR;

namespace Datacenter.Application.Features.Adjustments.Commands;

/// <summary>แก้ไขรายการปรับปรุง (แทนที่ header + lines ทั้งหมด, ต้องสมดุล)</summary>
public record UpdateAdjustmentEntryCommand(
    int Id,
    int ClientCompanyId,
    DateTime EntryDate,
    AdjustmentSourceType SourceType,
    string? Reference,
    string Reason,
    string? AttachmentPath,
    IReadOnlyList<AdjustmentLineInput> Lines)
    : IRequest<AdjustmentEntryDto>, IRequireCompanyAccess;
