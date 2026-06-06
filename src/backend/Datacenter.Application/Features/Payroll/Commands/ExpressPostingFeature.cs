using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Payroll.DTOs;
using Datacenter.Application.Features.Payroll.Queries;
using Datacenter.Application.Features.Payroll.Services;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Commands;

// ── ติดตามการคีย์รายการลง Express (ปลายทางบัญชี) + กระทบยอดกับยอดที่ควรลง ─────────
internal static class ExpressExpected
{
    /// <summary>ยอดที่ควรคีย์ลง Express ตาม source. คืน 0 ถ้ายังไม่มีข้อมูล</summary>
    public static async Task<decimal> ComputeAsync(
        ISender sender, IApplicationDbContext db, int companyId, ExpressPostingSourceType type, int year, int month, CancellationToken ct)
    {
        try
        {
            switch (type)
            {
                case ExpressPostingSourceType.PayrollExpense:
                {
                    var run = await db.PayrollRuns.AsNoTracking()
                        .FirstOrDefaultAsync(r => r.ClientCompanyId == companyId && r.Year == year && r.Month == month, ct);
                    if (run is null) return 0;
                    // ยอดค่าใช้จ่ายเงินเดือนที่ควรลง = ฝั่งเดบิตของใบสำคัญ (เงินเดือน+ค่าจ้าง+เบี้ยเลี้ยง+นายจ้างสมทบ)
                    var posting = await sender.Send(new GetPayrollPostingQuery(companyId, run.Id), ct);
                    return posting.TotalDebit;
                }
                case ExpressPostingSourceType.SsoRemittance:
                {
                    var run = await db.PayrollRuns.AsNoTracking().Include(r => r.Items)
                        .FirstOrDefaultAsync(r => r.ClientCompanyId == companyId && r.Year == year && r.Month == month, ct);
                    if (run is null) return 0;
                    var emp = PayrollCalculator.Round2(run.Items.Sum(i => i.SsoEmployee));
                    return PayrollCalculator.Round2(emp * 2); // ลูกจ้าง + นายจ้าง
                }
                case ExpressPostingSourceType.WcfInvoice:
                case ExpressPostingSourceType.WcfRemittance:
                {
                    var k = await sender.Send(new GetKt20Query(companyId, year), ct);
                    return k.Contribution;
                }
                default: return 0;
            }
        }
        catch (NotFoundException) { return 0; }
    }
}

public record GetExpressPostingLinkQuery(int ClientCompanyId, int SourceType, int Year, int Month)
    : IRequest<ExpressPostingLinkDto>, IRequireCompanyAccess;

public class GetExpressPostingLinkQueryHandler(IApplicationDbContext db, ISender sender)
    : IRequestHandler<GetExpressPostingLinkQuery, ExpressPostingLinkDto>
{
    public async Task<ExpressPostingLinkDto> Handle(GetExpressPostingLinkQuery req, CancellationToken ct)
    {
        var type = (ExpressPostingSourceType)req.SourceType;
        var expected = await ExpressExpected.ComputeAsync(sender, db, req.ClientCompanyId, type, req.Year, req.Month, ct);

        var link = await db.ExpressPostingLinks.AsNoTracking().FirstOrDefaultAsync(x =>
            x.ClientCompanyId == req.ClientCompanyId && x.SourceType == type &&
            x.Year == req.Year && x.Month == req.Month, ct);

        if (link is null)
            return new ExpressPostingLinkDto(req.SourceType, req.Year, req.Month,
                false, null, null, null, null, expected, false);

        var match = !link.PostedAmount.HasValue || Math.Abs(link.PostedAmount.Value - expected) < 0.05m;
        return new ExpressPostingLinkDto(
            req.SourceType, req.Year, req.Month,
            Posted: link.PostedDate != null, link.PostedDate, link.ExpressDocNo, link.PostedAmount, link.Note,
            expected, match);
    }
}

public record UpsertExpressPostingLinkCommand(
    int ClientCompanyId, int SourceType, int Year, int Month,
    DateTime? PostedDate, string? ExpressDocNo, decimal? PostedAmount, string? Note)
    : IRequest<int>, IRequireCompanyAccess;

public class UpsertExpressPostingLinkCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpsertExpressPostingLinkCommand, int>
{
    public async Task<int> Handle(UpsertExpressPostingLinkCommand req, CancellationToken ct)
    {
        _ = await db.ClientCompanies.FirstOrDefaultAsync(c => c.Id == req.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", req.ClientCompanyId);
        var type = (ExpressPostingSourceType)req.SourceType;

        var link = await db.ExpressPostingLinks.FirstOrDefaultAsync(x =>
            x.ClientCompanyId == req.ClientCompanyId && x.SourceType == type &&
            x.Year == req.Year && x.Month == req.Month, ct);
        if (link is null)
        {
            link = new ExpressPostingLink
            {
                ClientCompanyId = req.ClientCompanyId, SourceType = type, Year = req.Year, Month = req.Month,
                CreatedBy = currentUser.Username,
            };
            db.ExpressPostingLinks.Add(link);
        }
        else { link.ModifiedBy = currentUser.Username; link.ModifiedAt = DateTime.UtcNow; }

        link.PostedDate = req.PostedDate;
        link.ExpressDocNo = req.ExpressDocNo;
        link.PostedAmount = req.PostedAmount;
        link.Note = req.Note;

        await db.SaveChangesAsync(ct);
        return link.Id;
    }
}
