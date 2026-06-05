using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Adjustments.DTOs;
using MediatR;

namespace Datacenter.Application.Features.FixedAssets.Commands;

/// <summary>
/// สร้างรายการตัดจำหน่าย/ขายสินทรัพย์ออกจากบัญชี (derecognition) เข้า TB:
///   Dr ค่าเสื่อมราคาสะสม [accum ณ วันจำหน่าย] + Dr เงินรับ/เงินสด [ราคาขาย, ถ้ามี] + Dr ขาดทุน [ถ้าขาดทุน]
///   Cr ราคาทุนสินทรัพย์ [cost] + Cr กำไรจากการจำหน่าย [ถ้ากำไร]
/// คิดเฉพาะสินทรัพย์ที่จำหน่าย/ขาย/ตัดจำหน่ายในปีงบนั้น.
/// </summary>
public record GenerateDisposalAdjustmentCommand(
    int ClientCompanyId,
    int FiscalYear,
    IReadOnlyList<int> AssetIds,
    int GainAccountId,
    int LossAccountId,
    int? ProceedsAccountId,
    DateTime? EntryDate)
    : IRequest<AdjustmentEntryDto>, IRequireCompanyAccess;
