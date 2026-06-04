using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Adjustments.DTOs;
using Datacenter.Domain.Enums;
using MediatR;

namespace Datacenter.Application.Features.Adjustments.Commands;

/// <summary>สร้างรายการปรับปรุงปิดงบใหม่ (ต้องสมดุล Dr = Cr)</summary>
public record CreateAdjustmentEntryCommand(
    int ClientCompanyId,
    int FiscalYear,
    DateTime EntryDate,
    AdjustmentSourceType SourceType,
    string? Reference,
    string Reason,
    string? AttachmentPath,
    IReadOnlyList<AdjustmentLineInput> Lines)
    : IRequest<AdjustmentEntryDto>, IRequireCompanyAccess;
