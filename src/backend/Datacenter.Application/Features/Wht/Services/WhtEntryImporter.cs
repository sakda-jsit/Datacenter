using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Wht.Services;

/// <summary>
/// นำเข้ารายการภาษีหัก ณ ที่จ่ายจาก Express ISTAX.DBF — ใช้ร่วมใน pipeline นำเข้ากลาง (StartExpressImport).
/// **upsert ตาม SourceKey** (ไม่ลบทั้งชุด) เพื่อคงสถานะการส่งเมลไว้ข้าม re-import + upsert WhtPayee (อีเมลไม่ถูกทับ).
/// ไม่เรียก SaveChanges — ผู้เรียกบันทึกรวมกับ batch.
/// </summary>
public static class WhtEntryImporter
{
    public static async Task<(int Read, string Message)> ImportAsync(
        IApplicationDbContext db,
        IExpressDbfAdapter dbfAdapter,
        string folderPath,
        int clientCompanyId,
        int importBatchId,
        string username,
        CancellationToken ct)
    {
        var rows = await dbfAdapter.ReadWhtEntriesAsync(folderPath, ct);
        if (rows.Count == 0)
            return (0, string.Empty);

        var existing = await db.WhtEntries
            .Where(w => w.ClientCompanyId == clientCompanyId)
            .ToListAsync(ct);
        var byKey = existing.ToDictionary(w => w.SourceKey, StringComparer.Ordinal);

        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            seenKeys.Add(row.SourceKey);
            var formType = row.FormTypeCode == "S03" ? WhtFormType.Pnd3 : WhtFormType.Pnd53;

            if (!byKey.TryGetValue(row.SourceKey, out var entry))
            {
                entry = new WhtEntry
                {
                    ClientCompanyId = clientCompanyId,
                    SourceKey = row.SourceKey,
                    EmailStatus = WhtEmailStatus.NotSent,
                    CreatedBy = username,
                };
                db.WhtEntries.Add(entry);
            }
            else
            {
                entry.ModifiedBy = username;
                entry.ModifiedAt = DateTime.UtcNow;
                // ไม่แตะ EmailStatus/EmailRecipient/EmailSentAt/EmailSentBy/EmailError (คงสถานะส่งเมล)
            }

            // ฟิลด์จาก Express (sync ทุกครั้ง)
            entry.FormType           = formType;
            entry.TaxPeriod          = row.TaxPeriod!.Value;
            entry.WithholdDate       = row.WithholdDate;
            entry.DocumentNo         = row.DocumentNo;
            entry.ReferenceNo        = Norm(row.ReferenceNo);
            entry.PayeeName          = Norm(row.PayeeName);
            entry.PayeePrefix        = Norm(row.PayeePrefix);
            entry.PayeeTaxId         = Norm(row.PayeeTaxId);
            entry.PayeeAddress       = Norm(row.PayeeAddress);
            entry.IncomeType         = Norm(row.IncomeType);
            entry.BaseAmount         = row.BaseAmount;
            entry.TaxRate            = row.TaxRate;
            entry.TaxAmount          = row.TaxAmount;
            entry.Condition          = Norm(row.Condition);
            entry.IsLate             = row.IsLate;
            entry.ImportBatchId      = importBatchId;
        }

        // ลบ entry ที่ไม่มีใน Express แล้ว (ตาม SourceKey ที่หายไป)
        var stale = existing.Where(w => !seenKeys.Contains(w.SourceKey)).ToList();
        if (stale.Count > 0) db.WhtEntries.RemoveRange(stale);

        // upsert WhtPayee (เพิ่มรายใหม่/อัปเดตชื่อ — ไม่แตะ Email ที่เจ้าหน้าที่กรอก)
        await UpsertPayeesAsync(db, clientCompanyId, rows, username, ct);

        int pnd3  = rows.Count(r => r.FormTypeCode == "S03");
        int pnd53 = rows.Count - pnd3;
        return (rows.Count, $"ภาษีหัก ณ ที่จ่าย {rows.Count} รายการ (ภ.ง.ด.3: {pnd3}, ภ.ง.ด.53: {pnd53})");
    }

    private static async Task UpsertPayeesAsync(
        IApplicationDbContext db, int clientCompanyId,
        IReadOnlyList<Features.Import.DTOs.ExpressWhtEntryDto> rows, string username, CancellationToken ct)
    {
        var taxIds = rows
            .Select(r => r.PayeeTaxId)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (taxIds.Count == 0) return;

        var existingPayees = await db.WhtPayees
            .Where(p => p.ClientCompanyId == clientCompanyId)
            .ToListAsync(ct);
        var payeeByTax = existingPayees.ToDictionary(p => p.TaxId, StringComparer.Ordinal);

        foreach (var taxId in taxIds)
        {
            var name = rows.FirstOrDefault(r => string.Equals(r.PayeeTaxId?.Trim(), taxId, StringComparison.Ordinal))?.PayeeName;
            if (!payeeByTax.TryGetValue(taxId, out var payee))
            {
                db.WhtPayees.Add(new WhtPayee
                {
                    ClientCompanyId = clientCompanyId,
                    TaxId = taxId,
                    Name = Norm(name),
                    CreatedBy = username,
                });
            }
            else if (!string.IsNullOrWhiteSpace(name) && payee.Name != name)
            {
                payee.Name = Norm(name);  // sync ชื่อล่าสุด — ไม่แตะ Email
                payee.ModifiedBy = username;
                payee.ModifiedAt = DateTime.UtcNow;
            }
        }
    }

    private static string? Norm(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
