using System.Globalization;
using Datacenter.Application.Common.Auditing;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Datacenter.Infrastructure.Persistence.Interceptors;

/// <summary>
/// บันทึก field-level audit อัตโนมัติเมื่อ entity ที่ผู้ใช้แก้ได้ถูก "แก้ไข" (Modified):
/// 1 แถว/ฟิลด์ที่เปลี่ยน เก็บ old → new (docs/18 — แทน approval workflow).
/// create/delete ยังคงบันทึกเป็นรายการระดับ action ผ่าน IAuditService ใน command เดิม.
/// ปิดอัตโนมัติเมื่ออยู่ใน AuditScope.Suppress() (เช่น sync ข้อมูลจาก Express).
/// </summary>
public class FieldAuditSaveChangesInterceptor(ICurrentUserService currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Capture(DbContext? context)
    {
        if (context is null || AuditScope.IsSuppressed) return;

        // materialize ก่อน เพราะเราจะเพิ่ม AuditLog ใหม่เข้า ChangeTracker
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified
                     && AuditableEntityRegistry.IsAudited(e.Entity.GetType()))
            .ToList();
        if (entries.Count == 0) return;

        var logs = new List<AuditLog>();
        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType();
            var companyId = ResolveCompanyId(entry, entityType);
            var entityId = ResolvePrimaryKey(entry);

            foreach (var prop in entry.Properties)
            {
                if (!prop.IsModified) continue;
                var name = prop.Metadata.Name;
                if (AuditableEntityRegistry.IgnoredProperties.Contains(name)) continue;
                if (prop.Metadata.ClrType == typeof(byte[])) continue; // ไฟล์ blob ไม่ลง audit

                var before = Format(prop.OriginalValue);
                var after = Format(prop.CurrentValue);
                if (before == after) continue;

                logs.Add(new AuditLog
                {
                    ClientCompanyId = companyId,
                    UserId = currentUser.UserId,
                    Username = currentUser.Username,
                    Action = "Update",
                    EntityName = entityType.Name,
                    EntityId = entityId,
                    FieldName = name,
                    BeforeValue = before,
                    AfterValue = after,
                    CreatedAt = DateTime.UtcNow,
                });
            }
        }

        foreach (var log in logs)
            context.Add(log);
    }

    private static int? ResolveCompanyId(EntityEntry entry, Type entityType)
    {
        if (entry.Metadata.FindProperty("ClientCompanyId") is not null)
            return ToInt(entry.Property("ClientCompanyId").CurrentValue);
        if (entityType == typeof(ClientCompany))
            return ToInt(entry.Property("Id").CurrentValue);
        return null;
    }

    private static string? ResolvePrimaryKey(EntityEntry entry)
    {
        var pk = entry.Metadata.FindPrimaryKey();
        if (pk is null) return null;
        var values = pk.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString());
        return string.Join("-", values);
    }

    private static int? ToInt(object? value)
        => value is null ? null : Convert.ToInt32(value, CultureInfo.InvariantCulture);

    private static string? Format(object? value)
    {
        if (value is null) return null;
        var text = value switch
        {
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            decimal dec => dec.ToString(CultureInfo.InvariantCulture),
            double dbl => dbl.ToString(CultureInfo.InvariantCulture),
            float flt => flt.ToString(CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            _ => value.ToString() ?? string.Empty,
        };
        return text.Length > 1000 ? text[..1000] + "…" : text;
    }
}
