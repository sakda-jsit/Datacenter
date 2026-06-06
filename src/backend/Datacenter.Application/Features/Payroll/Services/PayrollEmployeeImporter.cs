using System.Text.RegularExpressions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Services;

/// <summary>
/// นำเข้าทะเบียนพนักงานจาก Express (APMAS ที่ผูกบัญชีเงินเดือนตาม PayrollAccountMapping)
/// — upsert ตาม SUPCOD/เลขผู้เสียภาษี ไม่ลบพนักงานที่กรอกมือ. เรียกใน StartExpressImport.
/// </summary>
public static class PayrollEmployeeImporter
{
    public static async Task<int> ImportAsync(
        IApplicationDbContext db, IExpressDbfAdapter adapter, string folderPath,
        int companyId, string username, CancellationToken ct)
    {
        var map = await db.PayrollAccountMappings.AsNoTracking()
            .Where(m => m.ClientCompanyId == companyId && m.Department != null
                && (m.Role == PayrollPostingRole.SalaryExpense || m.Role == PayrollPostingRole.DailyWageExpense))
            .ToDictionaryAsync(m => m.AccountCode.Trim(), m => m.Department!, ct);
        if (map.Count == 0) return 0; // ยังไม่ได้แมพบัญชีเงินเดือน → ข้าม

        var emps = await adapter.ReadPayrollEmployeesAsync(folderPath, map, ct);
        if (emps.Count == 0) return 0;

        var existing = await db.Employees.Where(e => e.ClientCompanyId == companyId).ToListAsync(ct);
        var bySupplier = existing.Where(e => !string.IsNullOrEmpty(e.SourceSupplierCode))
            .ToDictionary(e => e.SourceSupplierCode!, e => e);
        var byNationalId = existing.Where(e => !string.IsNullOrEmpty(e.NationalId))
            .GroupBy(e => Digits(e.NationalId)).ToDictionary(g => g.Key, g => g.First());

        int count = 0;
        foreach (var x in emps)
        {
            var tax = Digits(x.TaxId ?? "");
            var e = (bySupplier.TryGetValue(x.SupplierCode, out var es) ? es : null)
                    ?? (tax.Length > 0 && byNationalId.TryGetValue(tax, out var en) ? en : null);

            if (e is null)
            {
                e = new Employee
                {
                    ClientCompanyId = companyId,
                    EmployeeCode = x.SupplierCode,
                    EmploymentStatus = EmploymentStatus.Active,
                    SsoStatus = SsoMemberStatus.NotEnrolled,
                    StartDate = DateTime.UtcNow.Date,
                    CreatedBy = username,
                };
                db.Employees.Add(e);
            }
            else
            {
                e.ModifiedBy = username;
                e.ModifiedAt = DateTime.UtcNow;
            }

            // อัปเดตข้อมูลจาก Express (รหัส/ชื่อ/ที่อยู่/เลขผู้เสียภาษี/ฝ่าย) — คงข้อมูลที่กรอกมือ (เงินเดือน/ปกส.)
            e.EmployeeCode = x.SupplierCode;   // รหัสพนักงาน = รหัสเจ้าหนี้ Express (SUPCOD) ทุกคน
            e.SourceSupplierCode = x.SupplierCode;
            ApplyName(x.Prefix, x.Name, e);
            if (tax.Length > 0) e.NationalId = tax;
            if (!string.IsNullOrWhiteSpace(x.Address)) e.Address = x.Address;
            e.Department = x.Department;
            count++;
        }

        await db.SaveChangesAsync(ct);
        return count;
    }

    private static string Digits(string s) => Regex.Replace(s ?? "", @"\D", "");

    private static readonly string[] Prefixes = ["นางสาว", "นาง", "นาย"];

    /// <summary>แยกคำนำหน้า/ชื่อ/สกุล — APMAS มักฝังคำนำหน้าใน SUPNAM (PRENAM ว่าง) กันคำนำหน้าซ้ำ</summary>
    private static void ApplyName(string? prenam, string? supnam, Employee e)
    {
        var name = (supnam ?? "").Trim();
        if (name.Length == 0) return;
        var prefix = (prenam ?? "").Trim();
        foreach (var p in Prefixes)
            if (name.StartsWith(p)) { if (prefix.Length == 0) prefix = p; name = name[p.Length..].Trim(); break; }
        var parts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (prefix.Length > 0) e.Prefix = prefix;
        e.FirstName = parts.Length > 0 ? parts[0] : name;
        e.LastName = parts.Length > 1 ? parts[1] : "";
    }
}
