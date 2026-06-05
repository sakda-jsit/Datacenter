using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Bank.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Bank.Queries;

/// <summary>สมุดเงินฝากธนาคาร (รายการเดินบัญชี + ยอดคงเหลือสะสม) ของบัญชีหนึ่ง ในปีที่ระบุ</summary>
public record GetBankBookQuery(int ClientCompanyId, string BankAccountCode, int Year)
    : IRequest<BankBookDto>, IRequireCompanyAccess;

public class GetBankBookQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBankBookQuery, BankBookDto>
{
    public async Task<BankBookDto> Handle(GetBankBookQuery request, CancellationToken ct)
    {
        var clientName = await db.ClientCompanies
            .AsNoTracking().Where(c => c.Id == request.ClientCompanyId)
            .Select(c => c.LegalName).FirstOrDefaultAsync(ct) ?? "";

        var account = await db.BankAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.ClientCompanyId == request.ClientCompanyId && b.BankAccountCode == request.BankAccountCode, ct)
            ?? throw new NotFoundException("BankAccount", request.BankAccountCode);

        var all = await db.BankTransactions
            .AsNoTracking()
            .Where(t => t.ClientCompanyId == request.ClientCompanyId && t.BankAccountCode == request.BankAccountCode)
            .OrderBy(t => t.TransactionDate).ThenBy(t => t.Id)
            .ToListAsync(ct);

        var yearStart = new DateTime(request.Year, 1, 1);

        // ยอดยกมาต้นปี = ยอดยกมาบัญชี + เคลื่อนไหวสุทธิก่อนปีที่เลือก
        decimal opening = account.BalanceForward
            + all.Where(t => t.TransactionDate < yearStart).Sum(t => t.IsDeposit ? t.Amount : -t.Amount);
        opening = Math.Round(opening, 2);

        var rows = new List<BankBookRowDto>();
        var running = opening;
        foreach (var t in all.Where(t => t.TransactionDate.Year == request.Year))
        {
            var deposit = t.IsDeposit ? t.Amount : 0m;
            var withdrawal = t.IsDeposit ? 0m : t.Amount;
            running = Math.Round(running + deposit - withdrawal, 2);
            rows.Add(new BankBookRowDto(
                t.Id, t.TransactionDate, t.TransactionType, t.ChequeNo, t.CounterpartyName, t.Remark,
                deposit, withdrawal, running));
        }

        // ความสดของข้อมูล: เวลานำเข้ารายการเดินบัญชีล่าสุด (ทั้งบริษัท)
        DateTime? dataAsOf = await db.BankTransactions
            .AsNoTracking()
            .Where(t => t.ClientCompanyId == request.ClientCompanyId)
            .MaxAsync(t => (DateTime?)t.CreatedAt, ct);

        return new BankBookDto(
            request.ClientCompanyId, clientName, account.BankAccountCode, account.BankName, account.AccountNumber,
            request.Year, opening, rows, dataAsOf);
    }
}
