using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Import.DTOs;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Import.Services;

/// <summary>
/// ยกข้อมูลจาก staging (StagingAccount + StagingTrialBalance ชุด CUR) ไปยังตารางจริง
/// (Account + JournalEntry/JournalEntryLine) ใช้ร่วมกันทั้ง manual post และ auto-post ตอน import
///
/// ผู้เรียกต้องรับผิดชอบ: ตรวจสิทธิ์, เช็ก batch.Status == Success, audit และ SaveChanges รอบสุดท้าย
/// (เมธอดนี้ทำ SaveChanges ภายในเฉพาะหลัง upsert บัญชี เพื่อให้บัญชีใหม่ได้ Id สำหรับอ้างใน line)
/// </summary>
public static class ExpressPostingService
{
    public static async Task<PostImportResultDto> PostAsync(
        IApplicationDbContext db, ImportBatch batch, string username, CancellationToken ct)
    {
        var companyId = batch.ClientCompanyId;
        var fy = batch.FiscalYear;

        // ── 1) Upsert chart of accounts จาก staging ───────────────────────────────
        var stagingAccounts = await db.StagingAccounts
            .Where(s => s.ImportBatchId == batch.Id && s.IsValid)
            .ToListAsync(ct);

        var existingAccounts = await db.Accounts
            .Where(a => a.ClientCompanyId == companyId)
            .ToListAsync(ct);
        var byCode = existingAccounts.ToDictionary(a => a.AccountCode);

        foreach (var s in stagingAccounts)
        {
            var accountType = MapAccountType(s.Group);
            bool isPostable = s.AccountType == 0; // 0=detail (ลงรายการได้), 1=header

            if (byCode.TryGetValue(s.AccountCode, out var acc))
            {
                acc.AccountName = s.AccountName;
                acc.AccountName2 = s.AccountName2;
                acc.AccountType = accountType;
                acc.Level = s.Level;
                acc.ParentCode = s.ParentCode;
                acc.IsPostable = isPostable;
                acc.IsActive = true;
                acc.ModifiedAt = DateTime.UtcNow;
                acc.ModifiedBy = username;
            }
            else
            {
                acc = new Account
                {
                    ClientCompanyId = companyId,
                    AccountCode = s.AccountCode,
                    AccountName = s.AccountName,
                    AccountName2 = s.AccountName2,
                    AccountType = accountType,
                    Level = s.Level,
                    ParentCode = s.ParentCode,
                    IsPostable = isPostable,
                    IsActive = true,
                    CreatedBy = username,
                };
                db.Accounts.Add(acc);
                byCode[s.AccountCode] = acc;
            }
        }

        // บันทึกก่อน เพื่อให้บัญชีที่สร้างใหม่ได้ Id สำหรับอ้างใน JournalEntryLine
        await db.SaveChangesAsync(ct);

        // ── 2) ลบ posting เดิมของปีนี้ (idempotent: post ซ้ำ = replace) ──────────────
        var openDoc = $"OPEN-{fy}";
        var moveDoc = $"MOVE-{fy}";
        var oldEntries = await db.JournalEntries
            .Include(j => j.Lines)
            .Where(j => j.ClientCompanyId == companyId
                     && (j.DocumentNo == openDoc || j.DocumentNo == moveDoc))
            .ToListAsync(ct);
        if (oldEntries.Count > 0)
            db.JournalEntries.RemoveRange(oldEntries); // ลบ Lines ตาม cascade

        // ── 3) สร้าง JournalEntry ยอดยกมา + ยอดเคลื่อนไหว จาก staging TB ของปีที่ post ──
        // เลือกด้วย FiscalYear (ที่ tag ตาม slot LY/CUR/NY ตอน import) ไม่ใช่ PeriodSet คงที่ "CUR"
        // เพื่อให้ post ปี NY (ปีที่กำลังทำงาน) อ่านยอด slot NY จริง ไม่ใช่ CUR ปีก่อน
        var yearRows = await db.StagingTrialBalances
            .Where(t => t.ImportBatchId == batch.Id && t.IsValid && t.FiscalYear == fy)
            .ToListAsync(ct);

        // ── 3.1) ด่านกัน batch เก่า (import ก่อนมี FiscalYear ต่อ slot) ────────────────
        // batch เก่า tag ทุก slot (LY/CUR/NY) เป็นปีเดียวกัน → กรองด้วย FiscalYear จะได้ 3 ระเบียน
        // ต่อบัญชี = ยอดเบิ้ล 3 เท่า. ถ้าเจอแบบนั้นให้ถอยไปใช้พฤติกรรมเดิม (slot CUR เท่านั้น)
        // ซึ่งเป็นยอดเดียวกับที่ batch นั้นเคย post ไว้ + เตือนให้นำเข้าใหม่เพื่อรองรับหลายปี.
        string legacyWarning = "";
        var slots = yearRows.Select(r => r.PeriodSet).Distinct().ToList();
        var curRows = yearRows;
        if (slots.Count > 1)
        {
            curRows = yearRows.Where(r => r.PeriodSet == "CUR").ToList();
            legacyWarning =
                " (คำเตือน: batch นี้นำเข้าด้วยรุ่นก่อนที่จะแยกปีงบต่อ slot LY/CUR/NY — post จากยอด slot CUR ตามเดิม; " +
                "หากต้องการยอดปีอื่นในไฟล์เดียวกัน ให้นำเข้าใหม่)";
        }
        else if (curRows.Count == 0)
        {
            legacyWarning =
                $" (คำเตือน: ไม่พบยอดของปี {fy} ใน batch นี้ — ไฟล์ Express ที่นำเข้าอาจไม่มี slot ของปีนี้ ให้นำเข้าใหม่โดยเลือกปีให้ตรง)";
        }

        var openingEntry = new JournalEntry
        {
            ClientCompanyId = companyId,
            DocumentNo = openDoc,
            FiscalYear = fy,
            JournalDate = new DateTime(fy - 1, 12, 31),
            Description = $"ยอดยกมาต้นปี {fy} (นำเข้าจาก Express)",
            SourceModule = "OpeningBalance",
            ImportBatchId = batch.Id,
            CreatedBy = username,
        };
        var movementEntry = new JournalEntry
        {
            ClientCompanyId = companyId,
            DocumentNo = moveDoc,
            FiscalYear = fy,
            JournalDate = new DateTime(fy, 12, 31),
            Description = $"ยอดเคลื่อนไหวสะสมปี {fy} (นำเข้าจาก Express)",
            SourceModule = "ImportBalance",
            ImportBatchId = batch.Id,
            CreatedBy = username,
        };

        int openingLines = 0;
        int movementLines = 0;

        foreach (var r in curRows)
        {
            // ลงรายการเฉพาะบัญชี detail (postable) ที่มีอยู่จริง — ข้ามบัญชี header/ไม่พบ
            if (!byCode.TryGetValue(r.AccountCode, out var acc) || !acc.IsPostable)
                continue;

            // ยอดยกมา: BeginBalance เป็นค่า signed (debit เป็นบวก, credit เป็นลบ)
            if (r.BeginBalance != 0)
            {
                openingEntry.Lines.Add(new JournalEntryLine
                {
                    AccountId = acc.Id,
                    DebitAmount = r.BeginBalance > 0 ? r.BeginBalance : 0,
                    CreditAmount = r.BeginBalance < 0 ? -r.BeginBalance : 0,
                    Description = "ยอดยกมา",
                });
                openingLines++;
            }

            // ยอดเคลื่อนไหว: รวม closing adjustment เพื่อให้ยอดสิ้นงวดตรงกับ Express
            decimal debit = r.TotalDebit + r.ClosingDebit;
            decimal credit = r.TotalCredit + r.ClosingCredit;
            if (debit != 0 || credit != 0)
            {
                movementEntry.Lines.Add(new JournalEntryLine
                {
                    AccountId = acc.Id,
                    DebitAmount = debit,
                    CreditAmount = credit,
                    Description = "เคลื่อนไหวระหว่างปี",
                });
                movementLines++;
            }
        }

        if (openingEntry.Lines.Count > 0) db.JournalEntries.Add(openingEntry);
        if (movementEntry.Lines.Count > 0) db.JournalEntries.Add(movementEntry);

        // ── 4) ตั้งสถานะ posted (ผู้เรียก audit + SaveChanges รอบสุดท้ายเอง) ──────────
        batch.IsPosted = true;
        batch.PostedAt = DateTime.UtcNow;

        return new PostImportResultDto(
            batch.Id, fy, stagingAccounts.Count, openingLines, movementLines,
            $"Post สำเร็จ: บัญชี {stagingAccounts.Count} รายการ, ยอดยกมา {openingLines} บรรทัด, ยอดเคลื่อนไหว {movementLines} บรรทัด{legacyWarning}");
    }

    private static AccountType MapAccountType(int group) => group switch
    {
        1 => AccountType.Asset,
        2 => AccountType.Liability,
        3 => AccountType.Equity,
        4 => AccountType.Income,
        5 => AccountType.Expense,
        _ => AccountType.Asset,
    };
}
