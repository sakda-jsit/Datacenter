using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Ap.Services;

/// <summary>
/// นำเข้าผู้ขาย (APMAS, upsert master by รหัส) + ใบตั้งหนี้เจ้าหนี้ (APTRN RECTYP=3, replace ต่อบริษัท)
/// จาก Express — เรียกใน pipeline นำเข้ากลาง (StartExpressImport). ไม่เรียก SaveChanges.
/// </summary>
public static class ApImporter
{
    public static async Task<(int Suppliers, int Invoices, string Message)> ImportAsync(
        IApplicationDbContext db,
        IExpressDbfAdapter dbfAdapter,
        string folderPath,
        int clientCompanyId,
        int importBatchId,
        string username,
        CancellationToken ct)
    {
        var suppliers = await dbfAdapter.ReadSuppliersAsync(folderPath, ct);
        var invoices = await dbfAdapter.ReadApInvoicesAsync(folderPath, ct);
        if (suppliers.Count == 0 && invoices.Count == 0)
            return (0, 0, string.Empty);

        // ── ผู้ขาย: upsert by (บริษัท, รหัส) ──
        var existing = await db.Suppliers
            .Where(s => s.ClientCompanyId == clientCompanyId)
            .ToListAsync(ct);
        var byCode = existing.ToDictionary(s => s.SupplierCode, StringComparer.Ordinal);

        foreach (var row in suppliers)
        {
            if (!byCode.TryGetValue(row.SupplierCode, out var sup))
            {
                sup = new Supplier { ClientCompanyId = clientCompanyId, SupplierCode = row.SupplierCode, CreatedBy = username };
                db.Suppliers.Add(sup);
            }
            else
            {
                sup.ModifiedBy = username;
                sup.ModifiedAt = DateTime.UtcNow;
            }
            sup.Prefix = Norm(row.Prefix);
            sup.Name = string.IsNullOrWhiteSpace(row.Name) ? row.SupplierCode : row.Name;
            sup.TaxId = Norm(row.TaxId);
            sup.Address = Norm(row.Address);
            sup.Phone = Norm(row.Phone);
            sup.Contact = Norm(row.Contact);
            sup.Email = Norm(row.Email);
            sup.PaymentTermDays = row.PaymentTermDays;
            sup.PaymentCondition = Norm(row.PaymentCondition);
            sup.GlAccountCode = Norm(row.GlAccountCode);
            sup.Remark = Norm(row.Remark);
            sup.IsActive = row.IsActive;
        }

        // ── ใบตั้งหนี้: replace ทั้งชุดต่อบริษัท ──
        var oldInvoices = await db.ApInvoices.Where(i => i.ClientCompanyId == clientCompanyId).ToListAsync(ct);
        if (oldInvoices.Count > 0) db.ApInvoices.RemoveRange(oldInvoices);

        var nameByCode = suppliers.ToDictionary(s => s.SupplierCode, s => s.Name, StringComparer.Ordinal);
        foreach (var row in invoices)
        {
            nameByCode.TryGetValue(row.SupplierCode, out var supName);
            db.ApInvoices.Add(new ApInvoice
            {
                ClientCompanyId   = clientCompanyId,
                DocumentNo        = row.DocumentNo,
                DocumentDate      = row.DocumentDate,
                DueDate           = row.DueDate,
                SupplierCode      = row.SupplierCode,
                SupplierName      = Norm(supName),
                Amount            = row.Amount,
                VatRate           = row.VatRate,
                VatAmount         = row.VatAmount,
                NetAmount         = row.NetAmount,
                PaidAmount        = row.PaidAmount,
                OutstandingAmount = row.OutstandingAmount,
                IsCompleted       = row.IsCompleted,
                VatPeriod         = row.VatPeriod,
                Reference         = Norm(row.Reference),
                ImportBatchId     = importBatchId,
                CreatedBy         = username,
            });
        }

        return (suppliers.Count, invoices.Count, $"ผู้ขาย {suppliers.Count} ราย, ใบตั้งหนี้เจ้าหนี้ {invoices.Count} ใบ");
    }

    private static string? Norm(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
