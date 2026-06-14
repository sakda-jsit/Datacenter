using Datacenter.Application.Common.Auditing;
using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Import.Services;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Import.Commands;

public class StartExpressImportCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IExpressDbfAdapter dbfAdapter,
    IImportStorageService storage,
    IImportSnapshotService snapshotService,
    IAuditService audit)
    : IRequestHandler<StartExpressImportCommand, int>
{
    public async Task<int> Handle(StartExpressImportCommand request, CancellationToken ct)
    {
        // การนำเข้าจาก Express = sync ข้อมูล ไม่ใช่ user edit → ปิด field-level audit ตลอด flow
        using var auditSuppression = AuditScope.Suppress();

        var client = await db.ClientCompanies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ClientCompanyId && x.IsActive, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        if (!storage.ExpressFolderExists(client.Code))
            throw new Application.Common.Exceptions.ValidationException(
                new[] { new FluentValidation.Results.ValidationFailure(
                    "Path", $"ไม่พบโฟลเดอร์ Express DBF สำหรับรหัส '{client.Code}' กรุณาตรวจสอบการตั้งค่า Import:ExpressBasePath") });

        var folderPath = storage.GetExpressFolderPath(client.Code);

        if (!await dbfAdapter.FolderIsValidAsync(folderPath, ct))
            throw new Application.Common.Exceptions.ValidationException(
                new[] { new FluentValidation.Results.ValidationFailure(
                    "Path", $"ไม่พบไฟล์ ISINFO.DBF ในโฟลเดอร์ '{folderPath}'") });

        // refresh ข้อมูลบริษัทจาก ISINFO (sync ชื่อ Express/ที่อยู่/ชื่ออังกฤษ — คง LegalName ที่แก้เอง)
        await RefreshCompanyProfileAsync(request.ClientCompanyId, folderPath, ct);

        // นิยามรอบบัญชี (ISPRD) เป็น source of truth — ปีที่นำเข้าต้องอยู่ในนิยามนี้เท่านั้น
        IReadOnlyList<Features.Import.DTOs.ExpressAccountingPeriodDto> periods;
        try
        {
            periods = await dbfAdapter.ReadAccountingPeriodsAsync(folderPath, ct);
        }
        catch (Exception ex)
        {
            throw new Application.Common.Exceptions.ValidationException(
                new[] { new FluentValidation.Results.ValidationFailure(
                    "Path", $"อ่านนิยามรอบบัญชี (ISPRD) ไม่สำเร็จ: {ex.Message}") });
        }

        var definedYears = periods.Select(p => p.EndDate.Year).Distinct().OrderBy(y => y).ToList();
        if (definedYears.Count == 0)
            throw new Application.Common.Exceptions.ValidationException(
                new[] { new FluentValidation.Results.ValidationFailure(
                    "Path", "ไม่พบนิยามรอบบัญชีใน ISPRD") });

        if (!definedYears.Contains(request.FiscalYear))
            throw new Application.Common.Exceptions.ValidationException(
                new[] { new FluentValidation.Results.ValidationFailure(
                    "FiscalYear", $"ปี {request.FiscalYear} ไม่อยู่ในรอบบัญชีของข้อมูลนี้ (รอบบัญชีที่กำหนด: {string.Join(", ", definedYears)})") });

        var batch = new ImportBatch
        {
            ClientCompanyId = request.ClientCompanyId,
            SourceType      = ImportSourceType.ExpressDbf,
            ImportType      = "TrialBalance",
            FiscalYear      = request.FiscalYear,
            Status          = ImportStatus.Running,
            CreatedBy       = currentUser.Username,
        };
        db.ImportBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        // บันทึก audit ทันทีหลังสร้าง batch เพื่อให้ตรวจสอบย้อนกลับได้แม้การประมวลผลจะล้มเหลว
        await audit.LogAsync(
            action: "StartImport",
            entityName: "ImportBatch",
            entityId: batch.Id.ToString(),
            afterValue: $"{client.Code}/{request.FiscalYear}",
            companyId: request.ClientCompanyId,
            cancellationToken: ct);
        await db.SaveChangesAsync(ct);

        try
        {
            var accounts        = await dbfAdapter.ReadAccountsAsync(folderPath, ct);
            var trialBalanceRows = await dbfAdapter.ReadTrialBalanceAsync(folderPath, ct);

            var stagingAccounts = accounts.Select(a => new StagingAccount
            {
                ImportBatchId = batch.Id,
                ClientCompanyId = request.ClientCompanyId,
                AccountCode  = a.AccountCode,
                AccountName  = a.AccountName,
                AccountName2 = a.AccountName2,
                Level        = a.Level,
                ParentCode   = a.ParentCode,
                Group        = a.Group,
                AccountType  = a.AccountType,
            }).ToList();
            db.StagingAccounts.AddRange(stagingAccounts);

            var stagingTb = trialBalanceRows.Select(r => new StagingTrialBalance
            {
                ImportBatchId   = batch.Id,
                ClientCompanyId = request.ClientCompanyId,
                AccountCode     = r.AccountCode,
                PeriodSet       = r.PeriodSet,
                FiscalYear      = request.FiscalYear,
                BeginBalance    = r.BeginBalance,
                TotalDebit      = r.TotalDebit,
                TotalCredit     = r.TotalCredit,
                ClosingDebit    = r.ClosingDebit,
                ClosingCredit   = r.ClosingCredit,
                EndBalance      = r.EndBalance,
            }).ToList();
            db.StagingTrialBalances.AddRange(stagingTb);

            var details = ImportValidationService.ValidateAndBuildDetails(batch.Id, stagingAccounts, stagingTb);
            db.ImportBatchDetails.AddRange(details);

            int errorCount      = details.Count(d => !d.IsValid);
            batch.TotalRows     = stagingAccounts.Count + stagingTb.Count;
            batch.SuccessRows   = batch.TotalRows - errorCount;
            batch.ErrorRows     = errorCount;
            batch.Status        = errorCount == 0 ? ImportStatus.Success : ImportStatus.Failed;
            batch.FinishedAt    = DateTime.UtcNow;
            batch.Message       = errorCount == 0
                ? $"นำเข้าข้อมูลสำเร็จ: บัญชี {stagingAccounts.Count} รายการ, ยอดคงเหลือ {stagingTb.Count} รายการ"
                : $"พบข้อผิดพลาด {errorCount} รายการ กรุณาตรวจสอบผลการตรวจสอบ";

            // แทนที่อัตโนมัติ: เมื่อนำเข้าสำเร็จ ลบ batch เดิมของบริษัท+ปีเดียวกันทิ้ง (1 บริษัท/ปี เหลือ batch เดียว)
            // ทำเฉพาะตอน Success เพื่อไม่ให้ข้อมูลเดิมหายหากนำเข้าใหม่ผิดพลาด
            if (batch.Status == ImportStatus.Success)
            {
                int replaced = await RemoveSupersededBatchesAsync(
                    request.ClientCompanyId, request.FiscalYear, batch.Id, ct);
                if (replaced > 0)
                    batch.Message += $" (แทนที่ข้อมูลเดิม {replaced} batch)";

                // บันทึกนิยามรอบบัญชี + seed สถานะปิดงวดตาม LOCK ของ Express
                await PersistAccountingPeriodsAsync(request.ClientCompanyId, periods, ct);
            }

            await db.SaveChangesAsync(ct);

            // Auto-post: เมื่อนำเข้าสำเร็จ ยกข้อมูลเข้า production ให้อัตโนมัติ เพื่อให้รายงานแสดงทันที
            // ห่อด้วย try/catch แยก เพื่อไม่ให้ความล้มเหลวของ post ทำให้ batch กลายเป็น Failed
            if (batch.Status == ImportStatus.Success)
            {
                try
                {
                    var postResult = await ExpressPostingService.PostAsync(db, batch, currentUser.Username, ct);
                    await audit.LogAsync(
                        action: "AutoPostImport",
                        entityName: "ImportBatch",
                        entityId: batch.Id.ToString(),
                        afterValue: $"auto-posted FY{request.FiscalYear}: opening={postResult.OpeningLines}, movement={postResult.MovementLines}",
                        companyId: request.ClientCompanyId,
                        cancellationToken: ct);
                    await db.SaveChangesAsync(ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    batch.IsPosted = false;
                    batch.Message += $" (post อัตโนมัติไม่สำเร็จ: {ex.Message} — กรุณากดปุ่ม Post เข้าบัญชี)";
                    await db.SaveChangesAsync(CancellationToken.None);
                }

                // นำเข้าทะเบียนสินทรัพย์ถาวรจาก FAMAS.DBF พร้อมกัน (ดึงข้อมูลจาก Express ครบในครั้งเดียว)
                // แยก try/catch เพื่อไม่ให้ความล้มเหลวของ FA ทำให้ batch หลักเสียหาย
                try
                {
                    var faResult = await FixedAssets.Services.FixedAssetImporter.ImportAsync(
                        db, dbfAdapter, folderPath, request.ClientCompanyId, request.FiscalYear, currentUser.Username, ct);
                    if (faResult.Read > 0)
                    {
                        batch.Message += $" · {faResult.Message}";
                        await audit.LogAsync(
                            action: "ImportFixedAssets",
                            entityName: "ImportBatch",
                            entityId: batch.Id.ToString(),
                            afterValue: faResult.Message,
                            companyId: request.ClientCompanyId,
                            cancellationToken: ct);
                        await db.SaveChangesAsync(ct);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    batch.Message += $" (นำเข้าสินทรัพย์ไม่สำเร็จ: {ex.Message})";
                    await db.SaveChangesAsync(CancellationToken.None);
                }

                // นำเข้ารายงานภาษีมูลค่าเพิ่ม (ภาษีซื้อ/ขาย) จาก ISVAT.DBF พร้อมกัน
                // แยก try/catch เพื่อไม่ให้ความล้มเหลวของ VAT ทำให้ batch หลักเสียหาย
                try
                {
                    var vatResult = await Vat.Services.VatEntryImporter.ImportAsync(
                        db, dbfAdapter, folderPath, request.ClientCompanyId, batch.Id, currentUser.Username, ct);
                    if (vatResult.Read > 0)
                    {
                        batch.Message += $" · {vatResult.Message}";
                        await audit.LogAsync(
                            action: "ImportVat",
                            entityName: "ImportBatch",
                            entityId: batch.Id.ToString(),
                            afterValue: vatResult.Message,
                            companyId: request.ClientCompanyId,
                            cancellationToken: ct);
                        await db.SaveChangesAsync(ct);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    batch.Message += $" (นำเข้าภาษีมูลค่าเพิ่มไม่สำเร็จ: {ex.Message})";
                    await db.SaveChangesAsync(CancellationToken.None);
                }

                // นำเข้ารายการภาษีหัก ณ ที่จ่าย (ภ.ง.ด.3/53) จาก ISTAX.DBF พร้อมกัน
                try
                {
                    var whtResult = await Wht.Services.WhtEntryImporter.ImportAsync(
                        db, dbfAdapter, folderPath, request.ClientCompanyId, batch.Id, currentUser.Username, ct);
                    if (whtResult.Read > 0)
                    {
                        batch.Message += $" · {whtResult.Message}";
                        await audit.LogAsync(
                            action: "ImportWht",
                            entityName: "ImportBatch",
                            entityId: batch.Id.ToString(),
                            afterValue: whtResult.Message,
                            companyId: request.ClientCompanyId,
                            cancellationToken: ct);
                        await db.SaveChangesAsync(ct);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    batch.Message += $" (นำเข้าภาษีหัก ณ ที่จ่ายไม่สำเร็จ: {ex.Message})";
                    await db.SaveChangesAsync(CancellationToken.None);
                }

                // นำเข้าลูกค้า + ใบแจ้งหนี้ลูกหนี้ (ARMAS/ARTRN) พร้อมกัน
                try
                {
                    var arResult = await Ar.Services.ArImporter.ImportAsync(
                        db, dbfAdapter, folderPath, request.ClientCompanyId, batch.Id, currentUser.Username, ct);
                    if (arResult.Customers > 0 || arResult.Invoices > 0)
                    {
                        batch.Message += $" · {arResult.Message}";
                        await audit.LogAsync(
                            action: "ImportAr",
                            entityName: "ImportBatch",
                            entityId: batch.Id.ToString(),
                            afterValue: arResult.Message,
                            companyId: request.ClientCompanyId,
                            cancellationToken: ct);
                        await db.SaveChangesAsync(ct);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    batch.Message += $" (นำเข้าลูกหนี้ไม่สำเร็จ: {ex.Message})";
                    await db.SaveChangesAsync(CancellationToken.None);
                }

                // นำเข้าผู้ขาย + ใบตั้งหนี้เจ้าหนี้ (APMAS/APTRN) พร้อมกัน
                try
                {
                    var apResult = await Ap.Services.ApImporter.ImportAsync(
                        db, dbfAdapter, folderPath, request.ClientCompanyId, batch.Id, currentUser.Username, ct);
                    if (apResult.Suppliers > 0 || apResult.Invoices > 0)
                    {
                        batch.Message += $" · {apResult.Message}";
                        await audit.LogAsync(
                            action: "ImportAp",
                            entityName: "ImportBatch",
                            entityId: batch.Id.ToString(),
                            afterValue: apResult.Message,
                            companyId: request.ClientCompanyId,
                            cancellationToken: ct);
                        await db.SaveChangesAsync(ct);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    batch.Message += $" (นำเข้าเจ้าหนี้ไม่สำเร็จ: {ex.Message})";
                    await db.SaveChangesAsync(CancellationToken.None);
                }

                // นำเข้าสินค้าคงคลัง (STMAS) พร้อมกัน
                try
                {
                    var stResult = await Stock.Services.StockImporter.ImportAsync(
                        db, dbfAdapter, folderPath, request.ClientCompanyId, batch.Id, currentUser.Username, ct);
                    if (stResult.Read > 0)
                    {
                        batch.Message += $" · {stResult.Message}";
                        await audit.LogAsync(
                            action: "ImportStock",
                            entityName: "ImportBatch",
                            entityId: batch.Id.ToString(),
                            afterValue: stResult.Message,
                            companyId: request.ClientCompanyId,
                            cancellationToken: ct);
                        await db.SaveChangesAsync(ct);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    batch.Message += $" (นำเข้าสินค้าคงคลังไม่สำเร็จ: {ex.Message})";
                    await db.SaveChangesAsync(CancellationToken.None);
                }

                // นำเข้าบัญชีธนาคาร + รายการเดินบัญชี (BKMAS/BKTRN) พร้อมกัน
                try
                {
                    var bkResult = await Bank.Services.BankImporter.ImportAsync(
                        db, dbfAdapter, folderPath, request.ClientCompanyId, batch.Id, currentUser.Username, ct);
                    if (bkResult.Accounts > 0 || bkResult.Transactions > 0)
                    {
                        batch.Message += $" · {bkResult.Message}";
                        await audit.LogAsync(
                            action: "ImportBank",
                            entityName: "ImportBatch",
                            entityId: batch.Id.ToString(),
                            afterValue: bkResult.Message,
                            companyId: request.ClientCompanyId,
                            cancellationToken: ct);
                        await db.SaveChangesAsync(ct);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    batch.Message += $" (นำเข้าธนาคารไม่สำเร็จ: {ex.Message})";
                    await db.SaveChangesAsync(CancellationToken.None);
                }

                // นำเข้าทะเบียนพนักงานจาก Express (APMAS ที่ผูกบัญชีเงินเดือนตาม PayrollAccountMapping)
                try
                {
                    var empCount = await Payroll.Services.PayrollEmployeeImporter.ImportAsync(
                        db, dbfAdapter, folderPath, request.ClientCompanyId, currentUser.Username, ct);
                    if (empCount > 0)
                    {
                        batch.Message += $" · พนักงาน {empCount}";
                        await audit.LogAsync(
                            action: "ImportEmployees",
                            entityName: "ImportBatch",
                            entityId: batch.Id.ToString(),
                            afterValue: $"นำเข้าพนักงานจาก Express {empCount} คน",
                            companyId: request.ClientCompanyId,
                            cancellationToken: ct);
                        await db.SaveChangesAsync(ct);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    batch.Message += $" (นำเข้าพนักงานไม่สำเร็จ: {ex.Message})";
                    await db.SaveChangesAsync(CancellationToken.None);
                }

                // เก็บ snapshot ไฟล์ DBF ต้นฉบับเป็นหลักฐานเก็บถาวร 10 ปี (docs/20)
                // — เพื่อให้ยอดที่ปิดงบแล้วตรวจสอบย้อนกลับได้แม้ Express จะถูกแก้ภายหลัง
                try
                {
                    await CaptureSnapshotAsync(batch, client.Code, folderPath, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    batch.Message += $" (เก็บหลักฐานไฟล์ต้นฉบับไม่สำเร็จ: {ex.Message})";
                    await db.SaveChangesAsync(CancellationToken.None);
                }
            }

            return batch.Id;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            batch.Status     = ImportStatus.Failed;
            batch.FinishedAt = DateTime.UtcNow;
            batch.Message    = $"เกิดข้อผิดพลาด: {ex.Message}";
            // CancellationToken.None is intentional: the original token may already be cancelled,
            // but we must still persist the failure status so the batch record is not left as "Running".
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// sync ข้อมูลบริษัทจาก ISINFO (upsert profile): อัปเดต Name(Express)/Address/EnglishName ถ้าเปลี่ยน
    /// แต่ **ไม่แตะ LegalName** (ชื่อทางการที่แก้เอง) — ถ้า LegalName ว่างจึง backfill = ชื่อ Express.
    /// ไม่ทำให้ import ล้มถ้าอ่าน ISINFO ไม่ได้.
    /// </summary>
    private async Task RefreshCompanyProfileAsync(int companyId, string folderPath, CancellationToken ct)
    {
        try
        {
            var info = await dbfAdapter.ReadCompanyInfoAsync(folderPath, ct);
            var company = await db.ClientCompanies.FirstOrDefaultAsync(c => c.Id == companyId, ct);
            if (company is null) return;

            var expressName = !string.IsNullOrWhiteSpace(info.ThaiName) ? info.ThaiName
                            : !string.IsNullOrWhiteSpace(info.EngName) ? info.EngName
                            : company.Name;
            var engName = string.IsNullOrWhiteSpace(info.EngName) ? null : info.EngName;

            var changed = company.Name != expressName
                       || company.Address != info.Address
                       || company.EnglishName != engName
                       || (!string.IsNullOrWhiteSpace(info.TaxId) && company.TaxId != info.TaxId);

            if (changed)
            {
                var before = $"{company.Name} / {company.TaxId} / {company.Address}";
                company.Name = expressName;
                company.EnglishName = engName;
                if (info.Address is not null) company.Address = info.Address;
                if (!string.IsNullOrWhiteSpace(info.TaxId)) company.TaxId = info.TaxId;
                company.ModifiedBy = currentUser.Username;
                company.ModifiedAt = DateTime.UtcNow;

                await audit.LogAsync("SyncProfile", "ClientCompany",
                    entityId: company.Id.ToString(),
                    beforeValue: before,
                    afterValue: $"{company.Name} / {company.TaxId} / {company.Address}",
                    companyId: companyId, cancellationToken: ct);
            }

            // backfill LegalName ถ้ายังว่าง (เช่น ข้อมูลเก่าก่อนมี column)
            if (string.IsNullOrWhiteSpace(company.LegalName))
                company.LegalName = expressName;

            // เติมที่อยู่แยกช่องจาก Address (ครั้งแรกที่ยังว่าง — ไม่ทับที่แก้เอง)
            FillStructuredAddressIfEmpty(company);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // อ่าน ISINFO ไม่ได้ — ข้าม ไม่ให้ import ล้ม
        }
    }

    /// <summary>แยกที่อยู่ flat → ช่อง Addr* เมื่อยังไม่มีค่า (ผู้ใช้แก้เองแล้วจะไม่ถูกทับ)</summary>
    internal static void FillStructuredAddressIfEmpty(Domain.Entities.ClientCompany company)
    {
        bool hasStructured = new[] { company.AddrHouseNo, company.AddrMoo, company.AddrRoad,
            company.AddrSubDistrict, company.AddrDistrict, company.AddrProvince }
            .Any(v => !string.IsNullOrWhiteSpace(v));
        if (hasStructured || string.IsNullOrWhiteSpace(company.Address)) return;

        var a = CorporateTax.Services.ThaiAddressParser.Parse(company.Address);
        company.AddrHouseNo = a.HouseNo;
        company.AddrMoo = a.Moo;
        company.AddrSoi = a.Soi;
        company.AddrRoad = a.Road;
        company.AddrSubDistrict = a.SubDistrict;
        company.AddrDistrict = a.District;
        company.AddrProvince = a.Province;
        if (string.IsNullOrWhiteSpace(company.PostalCode) && !string.IsNullOrWhiteSpace(a.PostalCode))
            company.PostalCode = a.PostalCode;
    }

    /// <summary>
    /// ลบ batch เดิมของบริษัท+ปีเดียวกัน (ยกเว้น batch ปัจจุบัน) พร้อมข้อมูลที่เกี่ยวข้องทั้งหมด:
    /// posted JournalEntry (lines ลบตาม cascade), staging accounts/TB และ validation details
    /// คืนจำนวน batch ที่ถูกแทนที่ ยังไม่เรียก SaveChanges (รวมไว้กับการบันทึก batch ใหม่)
    /// </summary>
    private async Task<int> RemoveSupersededBatchesAsync(
        int companyId, int fiscalYear, int keepBatchId, CancellationToken ct)
    {
        var oldBatches = await db.ImportBatches
            .Where(b => b.ClientCompanyId == companyId && b.FiscalYear == fiscalYear && b.Id != keepBatchId)
            .ToListAsync(ct);
        if (oldBatches.Count == 0) return 0;

        var oldIds = oldBatches.Select(b => b.Id).ToList();

        // ลบไฟล์ zip หลักฐานของ batch เก่า (row ลบตาม cascade) — re-import แทนที่ของเดิมทั้งชุด
        var oldSnapshots = await db.ImportSnapshots
            .Where(s => oldIds.Contains(s.ImportBatchId))
            .ToListAsync(ct);
        foreach (var s in oldSnapshots)
            snapshotService.DeleteArchive(s.ArchiveRelativePath);
        if (oldSnapshots.Count > 0) db.ImportSnapshots.RemoveRange(oldSnapshots);

        var postedEntries = await db.JournalEntries
            .Include(j => j.Lines)
            .Where(j => j.ImportBatchId != null && oldIds.Contains(j.ImportBatchId.Value))
            .ToListAsync(ct);
        if (postedEntries.Count > 0) db.JournalEntries.RemoveRange(postedEntries);

        var details = await db.ImportBatchDetails.Where(d => oldIds.Contains(d.ImportBatchId)).ToListAsync(ct);
        if (details.Count > 0) db.ImportBatchDetails.RemoveRange(details);

        var stagingAccounts = await db.StagingAccounts.Where(s => oldIds.Contains(s.ImportBatchId)).ToListAsync(ct);
        if (stagingAccounts.Count > 0) db.StagingAccounts.RemoveRange(stagingAccounts);

        var stagingTb = await db.StagingTrialBalances.Where(s => oldIds.Contains(s.ImportBatchId)).ToListAsync(ct);
        if (stagingTb.Count > 0) db.StagingTrialBalances.RemoveRange(stagingTb);

        db.ImportBatches.RemoveRange(oldBatches);
        return oldBatches.Count;
    }

    /// <summary>
    /// เก็บ snapshot ไฟล์ DBF ต้นฉบับของ Express เป็น zip + metadata (Import Evidence) เก็บถาวร 10 ปี.
    /// เพิ่มสรุปจำนวนไฟล์ไว้ใน batch.Message และลง audit. ทำหลังนำเข้าสำเร็จ (1 snapshot/batch).
    /// </summary>
    private async Task CaptureSnapshotAsync(ImportBatch batch, string clientCode, string folderPath, CancellationToken ct)
    {
        var result = await snapshotService.CaptureAsync(folderPath, clientCode, batch.FiscalYear, ct);

        var status = result.FileCount == 0
            ? Domain.Enums.ImportSnapshotStatus.Failed
            : result.Partial
                ? Domain.Enums.ImportSnapshotStatus.Partial
                : Domain.Enums.ImportSnapshotStatus.Captured;

        var capturedAt = DateTime.UtcNow;
        var snapshot = new ImportSnapshot
        {
            ImportBatchId       = batch.Id,
            ClientCompanyId     = batch.ClientCompanyId,
            FiscalYear          = batch.FiscalYear,
            CapturedAt          = capturedAt,
            SourceFolderPath    = folderPath,
            ArchiveRelativePath = result.ArchiveRelativePath,
            ArchiveFileName     = result.ArchiveFileName,
            ArchiveByteSize     = result.ArchiveByteSize,
            ArchiveSha256       = result.ArchiveSha256,
            FileCount           = result.FileCount,
            TotalSourceBytes    = result.TotalSourceBytes,
            Status              = status,
            Note                = result.Note,
            RetainUntil         = capturedAt.AddYears(10),
            CreatedBy           = currentUser.Username,
            Files = result.Files.Select(f => new ImportSnapshotFile
            {
                TableName        = f.TableName,
                FileName         = f.FileName,
                ByteSize         = f.ByteSize,
                Sha256           = f.Sha256,
                RowCount         = f.RowCount,
                SourceModifiedAt = f.SourceModifiedAt,
                CreatedBy        = currentUser.Username,
            }).ToList(),
        };
        db.ImportSnapshots.Add(snapshot);

        batch.Message += $" · เก็บหลักฐานไฟล์ต้นฉบับ {result.FileCount} ไฟล์";
        await audit.LogAsync(
            action: "CaptureImportSnapshot",
            entityName: "ImportBatch",
            entityId: batch.Id.ToString(),
            afterValue: $"snapshot {result.FileCount} ไฟล์ ({result.ArchiveByteSize:N0} ไบต์), sha256={result.ArchiveSha256}",
            companyId: batch.ClientCompanyId,
            cancellationToken: ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// upsert นิยามรอบบัญชีของบริษัทตามปีที่ ISPRD ระบุ (ไม่ลบทิ้ง — แก้ของเดิม, เพิ่มของใหม่) และ seed สถานะปิดงวด:
    /// งวดที่ Express ระบุ LOCK='Y' จะตั้ง ClosingPeriod เป็น Closed (ถ้ายังเปิดอยู่)
    /// ยังไม่เรียก SaveChanges (รวมไว้กับการบันทึก batch)
    /// </summary>
    private async Task PersistAccountingPeriodsAsync(
        int companyId,
        IReadOnlyList<Features.Import.DTOs.ExpressAccountingPeriodDto> periods,
        CancellationToken ct)
    {
        if (periods.Count == 0) return;

        var years = periods.Select(p => p.EndDate.Year).Distinct().ToList();

        // upsert by (Year, PeriodNo) — ไม่ทำ wholesale delete เพื่อให้ Id เสถียรและไม่กระทบประวัติ
        var existingPeriods = await db.AccountingPeriods
            .Where(p => p.ClientCompanyId == companyId)
            .ToListAsync(ct);
        var periodByKey = existingPeriods.ToDictionary(p => (p.Year, p.PeriodNo));

        // ClosingPeriod เดิมของปีเหล่านั้น (ไว้ upsert สถานะตาม LOCK)
        var closingRows = await db.ClosingPeriods
            .Where(c => c.ClientCompanyId == companyId && years.Contains(c.Year))
            .ToListAsync(ct);
        var closingByKey = closingRows.ToDictionary(c => (c.Year, c.Month));

        foreach (var p in periods)
        {
            int year = p.EndDate.Year;
            int periodNo = p.EndDate.Month; // ระบบใช้ Year+Month (1-12) — งวดอิงเดือนของวันสิ้นงวด

            if (periodByKey.TryGetValue((year, periodNo), out var existingPeriod))
            {
                existingPeriod.BeginDate = p.BeginDate;
                existingPeriod.EndDate = p.EndDate;
                existingPeriod.SourceLocked = p.Locked;
                existingPeriod.ModifiedBy = currentUser.Username;
                existingPeriod.ModifiedAt = DateTime.UtcNow;
            }
            else
            {
                db.AccountingPeriods.Add(new Domain.Entities.AccountingPeriod
                {
                    ClientCompanyId = companyId,
                    Year = year,
                    PeriodNo = periodNo,
                    BeginDate = p.BeginDate,
                    EndDate = p.EndDate,
                    SourceLocked = p.Locked,
                    CreatedBy = currentUser.Username,
                });
            }

            // seed: งวดที่ล็อกใน Express → ตั้งเป็น Closed (ไม่ downgrade งวดที่ปิด/ล็อกไว้แล้ว)
            if (p.Locked)
            {
                if (closingByKey.TryGetValue((year, periodNo), out var existing))
                {
                    if (existing.Status == PeriodStatus.Open)
                    {
                        existing.Status = PeriodStatus.Closed;
                        existing.ClosedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    db.ClosingPeriods.Add(new Domain.Entities.ClosingPeriod
                    {
                        ClientCompanyId = companyId,
                        Year = year,
                        Month = periodNo,
                        Status = PeriodStatus.Closed,
                        ClosedAt = DateTime.UtcNow,
                    });
                }
            }
        }
    }
}
