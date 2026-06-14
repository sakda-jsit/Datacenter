using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Vat.Services;

/// <summary>
/// นำเข้ารายงานภาษีมูลค่าเพิ่มจาก Express ISVAT.DBF — ใช้ร่วมใน pipeline นำเข้ากลาง (StartExpressImport).
/// VAT เป็นข้อมูล transactional ที่ดึงจาก Express 100% (ไม่แก้มือ) → แทนที่ทั้งชุดต่อบริษัท (sync ใหม่ทุกครั้ง)
/// คล้ายกับการ replace JournalEntry ต่อ batch. ไม่เรียก SaveChanges — ผู้เรียกบันทึกรวมกับ batch.
/// </summary>
public static class VatEntryImporter
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
        var rows = await dbfAdapter.ReadVatEntriesAsync(folderPath, ct);
        if (rows.Count == 0)
            return (0, string.Empty);

        // แทนที่ทั้งชุด: ลบรายการ VAT เดิมของบริษัท แล้วโหลดใหม่จาก Express (sync ตรงต้นทาง)
        var existing = await db.VatEntries
            .Where(v => v.ClientCompanyId == clientCompanyId)
            .ToListAsync(ct);
        if (existing.Count > 0) db.VatEntries.RemoveRange(existing);

        foreach (var row in rows)
        {
            db.VatEntries.Add(new VatEntry
            {
                ClientCompanyId    = clientCompanyId,
                VatType            = row.VatRecType == "P" ? VatEntryType.Input : VatEntryType.Output,
                TaxPeriod          = row.TaxPeriod!.Value,
                DocumentDate       = row.DocumentDate,
                VatDate            = row.VatDate,
                DocumentNo         = row.DocumentNo,
                ReferenceNo        = string.IsNullOrWhiteSpace(row.ReferenceNo) ? null : row.ReferenceNo,
                Description        = string.IsNullOrWhiteSpace(row.Description) ? null : row.Description,
                CounterpartyTaxId  = string.IsNullOrWhiteSpace(row.CounterpartyTaxId) ? null : row.CounterpartyTaxId,
                CounterpartyPrefix = string.IsNullOrWhiteSpace(row.CounterpartyPrefix) ? null : row.CounterpartyPrefix,
                BaseAmount         = row.BaseAmount,
                VatAmount          = row.VatAmount,
                ZeroRatedAmount    = row.ZeroRatedAmount,
                IsLate             = row.IsLate,
                RecordType         = string.IsNullOrWhiteSpace(row.RecordType) ? null : row.RecordType,
                DepartmentCode     = string.IsNullOrWhiteSpace(row.DepartmentCode) ? null : row.DepartmentCode!.Trim(),
                ImportBatchId      = importBatchId,
                CreatedBy          = username,
            });
        }

        int output = rows.Count(r => r.VatRecType != "P");
        int input  = rows.Count - output;
        return (rows.Count, $"ภาษีมูลค่าเพิ่ม {rows.Count} รายการ (ขาย {output}, ซื้อ {input})");
    }
}
