using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Ar.Services;

/// <summary>
/// นำเข้าลูกค้า (ARMAS, upsert master by รหัส) + ใบแจ้งหนี้ลูกหนี้ (ARTRN RECTYP=3, replace ต่อบริษัท)
/// จาก Express — เรียกใน pipeline นำเข้ากลาง (StartExpressImport). ไม่เรียก SaveChanges.
/// </summary>
public static class ArImporter
{
    public static async Task<(int Customers, int Invoices, string Message)> ImportAsync(
        IApplicationDbContext db,
        IExpressDbfAdapter dbfAdapter,
        string folderPath,
        int clientCompanyId,
        int importBatchId,
        string username,
        CancellationToken ct)
    {
        var customers = await dbfAdapter.ReadCustomersAsync(folderPath, ct);
        var invoices = await dbfAdapter.ReadArInvoicesAsync(folderPath, ct);
        if (customers.Count == 0 && invoices.Count == 0)
            return (0, 0, string.Empty);

        // ── ลูกค้า: upsert by (บริษัท, รหัสลูกค้า) ──
        var existing = await db.Customers
            .Where(c => c.ClientCompanyId == clientCompanyId)
            .ToListAsync(ct);
        var byCode = existing.ToDictionary(c => c.CustomerCode, StringComparer.Ordinal);

        foreach (var row in customers)
        {
            if (!byCode.TryGetValue(row.CustomerCode, out var cust))
            {
                cust = new Customer { ClientCompanyId = clientCompanyId, CustomerCode = row.CustomerCode, CreatedBy = username };
                db.Customers.Add(cust);
            }
            else
            {
                cust.ModifiedBy = username;
                cust.ModifiedAt = DateTime.UtcNow;
            }
            cust.Prefix = Norm(row.Prefix);
            cust.Name = string.IsNullOrWhiteSpace(row.Name) ? row.CustomerCode : row.Name;
            cust.TaxId = Norm(row.TaxId);
            cust.Address = Norm(row.Address);
            cust.Phone = Norm(row.Phone);
            cust.Contact = Norm(row.Contact);
            cust.Email = Norm(row.Email);
            cust.PaymentTermDays = row.PaymentTermDays;
            cust.PaymentCondition = Norm(row.PaymentCondition);
            cust.GlAccountCode = Norm(row.GlAccountCode);
            cust.Remark = Norm(row.Remark);
            cust.IsActive = row.IsActive;
        }

        // ── ใบแจ้งหนี้: replace ทั้งชุดต่อบริษัท (transactional, sync จาก Express) ──
        var oldInvoices = await db.ArInvoices.Where(i => i.ClientCompanyId == clientCompanyId).ToListAsync(ct);
        if (oldInvoices.Count > 0) db.ArInvoices.RemoveRange(oldInvoices);

        var nameByCode = customers.ToDictionary(c => c.CustomerCode, c => c.Name, StringComparer.Ordinal);
        foreach (var row in invoices)
        {
            nameByCode.TryGetValue(row.CustomerCode, out var custName);
            db.ArInvoices.Add(new ArInvoice
            {
                ClientCompanyId   = clientCompanyId,
                DocumentNo        = row.DocumentNo,
                DocumentDate      = row.DocumentDate,
                DueDate           = row.DueDate,
                CustomerCode      = row.CustomerCode,
                CustomerName      = Norm(custName),
                Amount            = row.Amount,
                VatRate           = row.VatRate,
                VatAmount         = row.VatAmount,
                NetAmount         = row.NetAmount,
                ReceivedAmount    = row.ReceivedAmount,
                OutstandingAmount = row.OutstandingAmount,
                IsCompleted       = row.IsCompleted,
                VatPeriod         = row.VatPeriod,
                Reference         = Norm(row.Reference),
                ImportBatchId     = importBatchId,
                CreatedBy         = username,
            });
        }

        return (customers.Count, invoices.Count, $"ลูกค้า {customers.Count} ราย, ใบแจ้งหนี้ลูกหนี้ {invoices.Count} ใบ");
    }

    private static string? Norm(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
