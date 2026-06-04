using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Adjustments.DTOs;
using MediatR;

namespace Datacenter.Application.Features.Adjustments.Queries;

/// <summary>
/// งบทดลองหลังปรับปรุง (Adjusted TB) สำหรับปีงบ: ยอดยกมา / เคลื่อนไหว / ก่อนปรับ / ปรับปรุง / หลังปรับ.
/// คำนวณสด: ยอดนำเข้า (JournalEntry) + รายการปรับปรุงปัจจุบัน (AdjustmentEntry).
/// </summary>
public record GetAdjustedTrialBalanceQuery(
    int ClientCompanyId,
    int FiscalYear,
    bool IncludeZeroBalance = false)
    : IRequest<AdjustedTrialBalanceReportDto>, IRequireCompanyAccess;
