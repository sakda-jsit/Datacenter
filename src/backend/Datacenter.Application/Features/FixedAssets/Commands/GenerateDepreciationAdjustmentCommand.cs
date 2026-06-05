using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Adjustments.DTOs;
using Datacenter.Domain.Enums;
using MediatR;

namespace Datacenter.Application.Features.FixedAssets.Commands;

/// <summary>
/// สร้างรายการปรับปรุงค่าเสื่อมราคาที่รับรู้ในปี → Dr ค่าเสื่อมราคา / Cr ค่าเสื่อมราคาสะสม
/// (รวมยอดต่อบัญชีจากสินทรัพย์ที่เลือก) ผ่าน pipeline ของ AdjustmentEntry เดิม.
/// </summary>
public record GenerateDepreciationAdjustmentCommand(
    int ClientCompanyId,
    int FiscalYear,
    IReadOnlyList<int> AssetIds,
    DepreciationSet Set,
    DateTime? EntryDate)
    : IRequest<AdjustmentEntryDto>, IRequireCompanyAccess;
