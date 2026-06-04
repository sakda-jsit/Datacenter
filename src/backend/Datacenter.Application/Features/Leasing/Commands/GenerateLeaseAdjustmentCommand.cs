using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Adjustments.DTOs;
using MediatR;

namespace Datacenter.Application.Features.Leasing.Commands;

/// <summary>
/// สร้างรายการปรับปรุง (AdjustmentEntry) จากดอกเบี้ยที่รับรู้ในปีงบของสัญญาที่เลือก:
/// - เช่าซื้อ: Dr ดอกเบี้ยจ่าย / Cr ดอกเบี้ยเช่าซื้อรอตัดบัญชี
/// - เงินกู้:  Dr ดอกเบี้ยจ่าย / Cr หนี้สินเงินกู้
/// </summary>
public record GenerateLeaseAdjustmentCommand(
    int ClientCompanyId,
    int FiscalYear,
    IReadOnlyList<int> ContractIds,
    DateTime? EntryDate)
    : IRequest<AdjustmentEntryDto>, IRequireCompanyAccess;
