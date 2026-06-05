using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FinancialStatement.DTOs;
using MediatR;

namespace Datacenter.Application.Features.FinancialStatement.Queries;

/// <summary>
/// หมายเหตุประกอบงบการเงิน (NOTE2) ฉบับเต็ม — ข้อความ template (แก้ได้) + ตารางตัวเลข (auto ปีปัจจุบัน/ปีก่อน).
/// </summary>
public record GetNotesToFsQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<NotesToFsDto>, IRequireCompanyAccess;
