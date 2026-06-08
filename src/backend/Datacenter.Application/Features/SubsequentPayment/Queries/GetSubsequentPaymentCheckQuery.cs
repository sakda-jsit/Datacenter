using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.SubsequentPayment.DTOs;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.SubsequentPayment.Queries;

/// <summary>
/// RPT-019 Subsequent Payment Check — ตรวจว่ารายการค้างจ่าย ณ สิ้นปีปิดงบ (บัญชีหนี้สิน)
/// ถูกจ่ายชำระจริงในปีถัดไปหรือยัง โดยอ่านสมุดรายวันระดับรายการ (GLJNLIT) ของปีถัดไป "สด" จาก Express
/// (ปีถัดไปยังเป็นรอบปัจจุบันใน Express → detail ยังอยู่). ข้อมูลปีถัดไปเป็นหลักฐานประกอบเท่านั้น
/// ไม่ถูกนำมารวมกับยอดปีปิดงบ (ตาม docs/17).
/// </summary>
public record GetSubsequentPaymentCheckQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<SubsequentPaymentReportDto>, IRequireCompanyAccess;

public class GetSubsequentPaymentCheckQueryHandler(
    IApplicationDbContext db,
    IImportStorageService storage,
    IExpressDbfAdapter dbfAdapter)
    : IRequestHandler<GetSubsequentPaymentCheckQuery, SubsequentPaymentReportDto>
{
    private const decimal Tolerance = 0.01m;

    public async Task<SubsequentPaymentReportDto> Handle(GetSubsequentPaymentCheckQuery request, CancellationToken ct)
    {
        var client = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ClientCompanyId && x.IsActive, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var fy = request.FiscalYear;
        var subsequentYear = fy + 1;

        // 1) บัญชีหนี้สินที่ลงรายการได้ (= ที่อาจมียอดค้างจ่าย)
        var liabilityAccounts = await db.Accounts.AsNoTracking()
            .Where(a => a.ClientCompanyId == request.ClientCompanyId
                     && a.AccountType == AccountType.Liability
                     && a.IsPostable && a.IsActive)
            .Select(a => new { a.Id, a.AccountCode, a.AccountName })
            .ToListAsync(ct);

        if (liabilityAccounts.Count == 0)
            return Empty(client, fy, subsequentYear);

        var liabIds = liabilityAccounts.Select(a => a.Id).ToList();

        // 2) ยอดค้างจ่าย ณ สิ้นปีปิดงบ (เครดิต-เป็นบวก) จาก GL ที่นำเข้าแล้ว — สะสมถึงสิ้นปีงบ
        var yearEndExclusive = new DateTime(fy, 12, 31).AddDays(1);
        var glNet = await db.JournalEntryLines.AsNoTracking()
            .Where(l => l.JournalEntry.ClientCompanyId == request.ClientCompanyId
                     && l.JournalEntry.JournalDate < yearEndExclusive
                     && liabIds.Contains(l.AccountId))
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.DebitAmount), Credit = g.Sum(x => x.CreditAmount) })
            .ToDictionaryAsync(x => x.AccountId, ct);

        var payables = liabilityAccounts
            .Select(a => new
            {
                a.Id,
                a.AccountCode,
                a.AccountName,
                Closing = Math.Round((glNet.GetValueOrDefault(a.Id)?.Credit ?? 0m)
                                   - (glNet.GetValueOrDefault(a.Id)?.Debit ?? 0m), 2),
            })
            .Where(a => a.Closing > Tolerance)
            .OrderBy(a => a.AccountCode)
            .ToList();

        if (payables.Count == 0)
            return Empty(client, fy, subsequentYear);

        // 3) อ่านการจ่ายชำระ (เดบิต) ในปีถัดไป "สด" จาก GLJNLIT ของ Express
        var payableCodes = payables.Select(p => p.AccountCode).ToHashSet(StringComparer.Ordinal);
        var paymentsByCode = new Dictionary<string, List<SubsequentPaymentDetailDto>>(StringComparer.Ordinal);
        var expressAvailable = false;

        if (storage.ExpressFolderExists(client.Code))
        {
            try
            {
                var folder = storage.GetExpressFolderPath(client.Code);
                var lines = await dbfAdapter.ReadGlJournalLinesAsync(
                    folder, payableCodes,
                    new DateTime(subsequentYear, 1, 1),
                    new DateTime(subsequentYear + 1, 1, 1), ct);
                expressAvailable = true;

                foreach (var ln in lines)
                {
                    // เดบิตบัญชีหนี้สิน = การจ่ายชำระ/ตัดยอดค้าง (เครดิต = ตั้งหนี้ใหม่ ไม่นับ)
                    if (ln.Debit <= 0m) continue;
                    if (!paymentsByCode.TryGetValue(ln.AccountCode, out var list))
                        paymentsByCode[ln.AccountCode] = list = [];
                    list.Add(new SubsequentPaymentDetailDto(ln.Voucher, ln.Date, ln.Description, ln.Debit));
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // อ่าน GLJNLIT ปีถัดไปไม่ได้ (Express ออฟไลน์/ไฟล์ล็อก) → ทุกแถวเป็น unmatched
                expressAvailable = false;
            }
        }

        // 4) จัดประเภทสถานะต่อบัญชี
        var rows = new List<SubsequentPaymentRowDto>(payables.Count);
        foreach (var p in payables)
        {
            var payments = paymentsByCode.GetValueOrDefault(p.AccountCode) ?? [];
            var paid = Math.Round(payments.Sum(x => x.Amount), 2);
            var remaining = Math.Round(Math.Max(p.Closing - paid, 0m), 2);

            var status = !expressAvailable ? "unmatched"
                : paid <= Tolerance ? "unpaid"
                : paid + Tolerance >= p.Closing ? "paid"
                : "partial";

            rows.Add(new SubsequentPaymentRowDto(
                p.Id, p.AccountCode, p.AccountName,
                p.Closing, paid, remaining, status,
                payments.OrderBy(x => x.Date).ThenBy(x => x.Voucher).ToList()));
        }

        return new SubsequentPaymentReportDto(
            client.Id, client.Code, client.LegalName, fy, subsequentYear,
            expressAvailable, DateTime.UtcNow, rows);
    }

    private static SubsequentPaymentReportDto Empty(Datacenter.Domain.Entities.ClientCompany client, int fy, int subsequentYear)
        => new(client.Id, client.Code, client.LegalName, fy, subsequentYear, false, DateTime.UtcNow, []);
}
