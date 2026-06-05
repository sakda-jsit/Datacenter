using System.Globalization;
using System.Text;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Import.DTOs;
using Datacenter.Infrastructure.Services.Dbf;

namespace Datacenter.Infrastructure.Services;

/// <summary>
/// อ่านไฟล์ DBF ของ Express Accounting ด้วย encoding cp874 (TIS-620 ไทย)
/// โครงสร้างโฟลเดอร์: {BasePath}\{ClientCode}\  เช่น D:\ExpressI\JSIT2016\
/// </summary>
public class ExpressDbfAdapter : IExpressDbfAdapter
{
    private static readonly Encoding ThaiEncoding;

    static ExpressDbfAdapter()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        ThaiEncoding = Encoding.GetEncoding(874);
    }

    // ─── helpers ──────────────────────────────────────────────────────────────

    private static string FindDbf(string folder, string name)
    {
        foreach (var ext in new[] { ".DBF", ".dbf" })
        foreach (var nm  in new[] { name.ToUpper(), name.ToLower() })
        {
            var path = Path.Combine(folder, nm + ext);
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException($"ไม่พบตาราง {name} ใน {folder}");
    }

    private static List<DbfRow> ReadDbf(string folder, string name)
    {
        var path = FindDbf(folder, name);
        return DbfReader.Read(path, ThaiEncoding);
    }

    private static string Str(DbfRow rec, string field)
        => Normalize(rec[field]?.ToString() ?? "");

    /// <summary>Express ใส่ non-breaking space (0xA0) แทนช่องว่างในชื่อ/ที่อยู่ — แปลงเป็นช่องว่างปกติ</summary>
    private static string Normalize(string s)
        => s.Replace((char)0x00A0, ' ').Trim();

    private static decimal Dec(DbfRow rec, string field)
        => rec[field] switch
        {
            decimal d => d,
            double dbl => (decimal)dbl,
            int i => i,
            long l => l,
            // Express เก็บฟิลด์ตัวเลขบางตัว (เช่น GROUP/ACCTYP ใน GLACC, VATRAT/YEARTHAI ใน ISINFO)
            // เป็นชนิด Character ('C') จึงต้อง parse จาก string ด้วย มิฉะนั้นจะได้ 0 ทั้งหมด
            string s when decimal.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) => v,
            _ => 0m,
        };

    private static decimal MonthSum(DbfRow rec, string prefix, string suffix)
        => Enumerable.Range(1, 12).Sum(m => Dec(rec, $"{prefix}{m}{suffix}"));

    private static DateTime? Date(DbfRow rec, string field)
        => rec[field] as DateTime?;

    private static bool IsLocked(DbfRow rec, string field)
        => Str(rec, field).Equals("Y", StringComparison.OrdinalIgnoreCase);

    // ─── interface ────────────────────────────────────────────────────────────

    public Task<bool> FolderIsValidAsync(string companyFolderPath, CancellationToken ct = default)
    {
        var valid = Directory.GetFiles(companyFolderPath, "ISINFO.*", SearchOption.TopDirectoryOnly)
            .Any(f => f.EndsWith(".DBF", StringComparison.OrdinalIgnoreCase)
                   || f.EndsWith(".dbf", StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(valid);
    }

    public Task<IReadOnlyList<ExpressDatasetDto>> ReadCompanyRegistryAsync(string basePath, CancellationToken ct = default)
    {
        var path = Path.Combine(basePath, "secure", "sccomp.dbf");
        if (!File.Exists(path))
            throw new FileNotFoundException($"ไม่พบทะเบียนข้อมูล Express: {path}");

        var rows = DbfReader.Read(path, ThaiEncoding);

        var list = rows
            .Select(r => new ExpressDatasetDto(
                CompName: Str(r, "COMPNAM"),
                Path:     Str(r, "PATH"),
                Candel:   Str(r, "CANDEL")))
            .Where(d => !string.IsNullOrWhiteSpace(d.Path))
            .ToList();

        return Task.FromResult<IReadOnlyList<ExpressDatasetDto>>(list);
    }

    public Task<ExpressCompanyInfoDto> ReadCompanyInfoAsync(string companyFolderPath, CancellationToken ct = default)
    {
        var records = ReadDbf(companyFolderPath, "ISINFO");
        var r       = records.FirstOrDefault()
                      ?? throw new InvalidOperationException("ISINFO ไม่มีข้อมูล");

        // ที่อยู่: ADDR02 (ไทย) เป็นหลัก, fallback ADDR01
        var addr2 = Str(r, "ADDR02");
        var addr1 = Str(r, "ADDR01");
        var address = !string.IsNullOrWhiteSpace(addr2) ? addr2
                    : !string.IsNullOrWhiteSpace(addr1) ? addr1 : null;

        var dto = new ExpressCompanyInfoDto(
            ThaiName : Str(r, "THINAM"),
            EngName  : Str(r, "ENGNAM"),
            TaxId    : Str(r, "TAXID"),
            VatRate  : Dec(r, "VATRAT"),
            YearThai : (int)Dec(r, "YEARTHAI"),
            Address  : address);

        return Task.FromResult(dto);
    }

    public Task<IReadOnlyList<ExpressAccountRowDto>> ReadAccountsAsync(string companyFolderPath, CancellationToken ct = default)
    {
        var records = ReadDbf(companyFolderPath, "GLACC");

        var result = records
            .Select(r => new ExpressAccountRowDto(
                AccountCode  : Str(r, "ACCNUM"),
                AccountName  : Str(r, "ACCNAM"),
                AccountName2 : Str(r, "ACCNAM2"),
                Level        : (int)Dec(r, "LEVEL"),
                ParentCode   : Str(r, "PARENT"),
                Group        : (int)Dec(r, "GROUP"),
                AccountType  : (int)Dec(r, "ACCTYP")))
            .Where(x => !string.IsNullOrWhiteSpace(x.AccountCode))
            .ToList();

        return Task.FromResult<IReadOnlyList<ExpressAccountRowDto>>(result);
    }

    public Task<IReadOnlyList<ExpressTrialBalanceRowDto>> ReadTrialBalanceAsync(string companyFolderPath, CancellationToken ct = default)
    {
        var records = ReadDbf(companyFolderPath, "GLBAL");
        var result  = new List<ExpressTrialBalanceRowDto>();

        foreach (var r in records)
        {
            var acc = Str(r, "ACCNUM");
            if (string.IsNullOrWhiteSpace(acc)) continue;

            var begLy  = Dec(r, "BEGLY");
            var lyDeb  = MonthSum(r, "DEBIT",  "LY");
            var lyCrd  = MonthSum(r, "CREDIT", "LY");
            var lyEnd  = begLy + lyDeb - lyCrd;

            var clsDeb = Dec(r, "DEBITCLS");
            var clsCrd = Dec(r, "CREDITCLS");
            var curDeb = MonthSum(r, "DEBIT",  "");
            var curCrd = MonthSum(r, "CREDIT", "");
            var curEnd = lyEnd + (clsDeb - clsCrd) + curDeb - curCrd;

            var nyDeb  = MonthSum(r, "DEBIT",  "NY");
            var nyCrd  = MonthSum(r, "CREDIT", "NY");
            var nyEnd  = curEnd + nyDeb - nyCrd;

            result.Add(new ExpressTrialBalanceRowDto(acc, "LY",  Math.Round(begLy, 2), Math.Round(lyDeb,  2), Math.Round(lyCrd,  2), 0, 0, Math.Round(lyEnd,  2)));
            result.Add(new ExpressTrialBalanceRowDto(acc, "CUR", Math.Round(lyEnd, 2), Math.Round(curDeb, 2), Math.Round(curCrd, 2), Math.Round(clsDeb, 2), Math.Round(clsCrd, 2), Math.Round(curEnd, 2)));
            result.Add(new ExpressTrialBalanceRowDto(acc, "NY",  Math.Round(curEnd,2), Math.Round(nyDeb,  2), Math.Round(nyCrd,  2), 0, 0, Math.Round(nyEnd,  2)));
        }

        return Task.FromResult<IReadOnlyList<ExpressTrialBalanceRowDto>>(result);
    }

    public Task<IReadOnlyList<ExpressAccountingPeriodDto>> ReadAccountingPeriodsAsync(string companyFolderPath, CancellationToken ct = default)
    {
        var records = ReadDbf(companyFolderPath, "ISPRD");
        var r = records.FirstOrDefault()
                ?? throw new InvalidOperationException("ISPRD ไม่มีข้อมูลนิยามรอบบัญชี");

        var result = new List<ExpressAccountingPeriodDto>();

        // งวดปัจจุบัน 12 งวด: BEG1..END12 / LOCK1..LOCK12
        // ปีถัดไป 12 งวด: BEG{n}NY..END{n}NY / LOCK{n}NY
        foreach (var suffix in new[] { "", "NY" })
        foreach (var n in Enumerable.Range(1, 12))
        {
            var beg = Date(r, $"BEG{n}{suffix}");
            var end = Date(r, $"END{n}{suffix}");
            if (beg is null || end is null) continue; // งวดที่ไม่ได้กำหนดวันที่ — ข้าม

            result.Add(new ExpressAccountingPeriodDto(
                BeginDate: beg.Value,
                EndDate:   end.Value,
                Locked:    IsLocked(r, $"LOCK{n}{suffix}")));
        }

        return Task.FromResult<IReadOnlyList<ExpressAccountingPeriodDto>>(result);
    }

    public Task<IReadOnlyList<ExpressFixedAssetDto>> ReadFixedAssetsAsync(string companyFolderPath, CancellationToken ct = default)
    {
        // FAMAS เป็นไฟล์ทางเลือก — บางบริษัทไม่ใช้โมดูลสินทรัพย์ → คืนว่าง
        List<DbfRow> records;
        try { records = ReadDbf(companyFolderPath, "FAMAS"); }
        catch (FileNotFoundException) { return Task.FromResult<IReadOnlyList<ExpressFixedAssetDto>>([]); }

        var result = new List<ExpressFixedAssetDto>();
        foreach (var r in records)
        {
            var code = Str(r, "FASCOD");
            var cost = Dec(r, "COSVAL");
            if (string.IsNullOrWhiteSpace(code) || cost <= 0) continue;

            // ชื่อ: รวม FASDES + FASDES2 (บรรทัดต่อ)
            var name = Str(r, "FASDES");
            var name2 = Str(r, "FASDES2");
            if (!string.IsNullOrWhiteSpace(name2)) name = $"{name} {name2}".Trim();

            // วันเริ่มคิดค่าเสื่อม (STRDAT) เป็นหลัก, fallback วันซื้อ (PURDAT)
            var acquire = Date(r, "STRDAT") ?? Date(r, "PURDAT");
            var saleDate = Date(r, "SALDAT");

            result.Add(new ExpressFixedAssetDto(
                AssetCode: code,
                AssetName: string.IsNullOrWhiteSpace(name) ? code : name,
                GroupCode: Str(r, "FASGRP"),
                CategoryCode: Str(r, "ACCCOD"),
                AcquireDate: acquire,
                Cost: Math.Round(cost, 2),
                Salvage: Math.Round(Dec(r, "SALVAG"), 2),
                RatePct: Math.Round(Dec(r, "RATE"), 2),
                LifeYears: (int)Dec(r, "LIFE"),
                Method: Str(r, "METHOD"),
                AccumulatedBroughtForward: Math.Round(Dec(r, "ACCMBF"), 2),
                SaleDate: saleDate,
                SaleAmount: Math.Round(Dec(r, "SALAMT"), 2),
                Status: Str(r, "STATUS")));
        }

        return Task.FromResult<IReadOnlyList<ExpressFixedAssetDto>>(result);
    }

    public Task<IReadOnlyList<ExpressVatEntryDto>> ReadVatEntriesAsync(string companyFolderPath, CancellationToken ct = default)
    {
        // ISVAT เป็นไฟล์ทางเลือก — บริษัทที่ไม่ได้จด VAT จะไม่มี → คืนว่าง
        List<DbfRow> records;
        try { records = ReadDbf(companyFolderPath, "ISVAT"); }
        catch (FileNotFoundException) { return Task.FromResult<IReadOnlyList<ExpressVatEntryDto>>([]); }

        var result = new List<ExpressVatEntryDto>();
        foreach (var r in records)
        {
            var rec = Str(r, "VATREC").ToUpperInvariant();
            if (rec != "S" && rec != "P") continue;   // เฉพาะภาษีขาย(S)/ภาษีซื้อ(P)

            var period = Date(r, "VATPRD");
            if (period is null) continue;              // ไม่มีเดือนภาษี — ข้าม

            var baseAmt = Dec(r, "AMT01") + Dec(r, "AMT02");
            var vatAmt  = Dec(r, "VAT01") + Dec(r, "VAT02");
            var zero    = Dec(r, "AMTRAT0");

            result.Add(new ExpressVatEntryDto(
                VatRecType:        rec,
                TaxPeriod:         period,
                DocumentDate:      Date(r, "DOCDAT"),
                VatDate:           Date(r, "VATDAT"),
                DocumentNo:        Str(r, "DOCNUM"),
                ReferenceNo:       Str(r, "REFNUM"),
                Description:       Str(r, "DESCRP"),
                CounterpartyTaxId: Str(r, "TAXID"),
                CounterpartyPrefix: Str(r, "PRENAM"),
                BaseAmount:        Math.Round(baseAmt, 2),
                VatAmount:         Math.Round(vatAmt, 2),
                ZeroRatedAmount:   Math.Round(zero, 2),
                IsLate:            IsLocked(r, "LATE"),
                RecordType:        Str(r, "RECTYP")));
        }

        return Task.FromResult<IReadOnlyList<ExpressVatEntryDto>>(result);
    }

    public Task<IReadOnlyList<ExpressWhtEntryDto>> ReadWhtEntriesAsync(string companyFolderPath, CancellationToken ct = default)
    {
        // ISTAX เป็นไฟล์ทางเลือก — บริษัทที่ไม่มีการหักภาษี ณ ที่จ่ายจะไม่มี → คืนว่าง
        List<DbfRow> records;
        try { records = ReadDbf(companyFolderPath, "ISTAX"); }
        catch (FileNotFoundException) { return Task.FromResult<IReadOnlyList<ExpressWhtEntryDto>>([]); }

        var result = new List<ExpressWhtEntryDto>();
        var ordinal = 0;
        foreach (var r in records)
        {
            ordinal++;
            var formCode = Str(r, "TAXTYP").ToUpperInvariant();
            if (formCode != "S03" && formCode != "S53") continue;   // เฉพาะ ภ.ง.ด.3/53

            var period = Date(r, "TAXPRD");
            if (period is null) continue;

            var docNo  = Str(r, "TAXNUM");
            var refNo  = Str(r, "REFNUM");
            var name   = Str(r, "NAME");
            var prefix = Str(r, "PRENAM");
            var taxId  = Str(r, "TAXID");
            var addr   = Str(r, "ADDR");
            var withhold = Date(r, "TAXDAT");
            var late   = IsLocked(r, "LATE");
            var cond   = Str(r, "TAXCOND");

            // base ของ SourceKey: TAXNUM → REFNUM → ลำดับแถว (เสถียรต่อ re-import ของงวดที่ปิดแล้ว)
            var keyBase = !string.IsNullOrWhiteSpace(docNo) ? docNo
                        : !string.IsNullOrWhiteSpace(refNo) ? refNo
                        : $"R{ordinal}";

            // ชุดเงินได้หลัก (line 0)
            var amt1 = Dec(r, "AMOUNT");
            var tax1 = Dec(r, "TAXAMT");
            if (amt1 > 0 || tax1 > 0)
                result.Add(new ExpressWhtEntryDto(
                    SourceKey:    $"{keyBase}#0",
                    FormTypeCode: formCode,
                    TaxPeriod:    period,
                    WithholdDate: withhold,
                    DocumentNo:   docNo,
                    ReferenceNo:  refNo,
                    PayeeName:    name,
                    PayeePrefix:  prefix,
                    PayeeTaxId:   taxId,
                    PayeeAddress: addr,
                    IncomeType:   Str(r, "TAXDES"),
                    BaseAmount:   Math.Round(amt1, 2),
                    TaxRate:      Math.Round(Dec(r, "TAXRAT"), 4),
                    TaxAmount:    Math.Round(tax1, 2),
                    Condition:    cond,
                    IsLate:       late));

            // ชุดเงินได้ที่ 2 (line 1) — บางหนังสือรับรองมีหลายประเภทเงินได้
            var amt2 = Dec(r, "AMOUNT2");
            var tax2 = Dec(r, "TAXAMT2");
            if (amt2 > 0 || tax2 > 0)
                result.Add(new ExpressWhtEntryDto(
                    SourceKey:    $"{keyBase}#1",
                    FormTypeCode: formCode,
                    TaxPeriod:    period,
                    WithholdDate: Date(r, "TAXDAT2") ?? withhold,
                    DocumentNo:   docNo,
                    ReferenceNo:  refNo,
                    PayeeName:    name,
                    PayeePrefix:  prefix,
                    PayeeTaxId:   taxId,
                    PayeeAddress: addr,
                    IncomeType:   Str(r, "TAXDES2"),
                    BaseAmount:   Math.Round(amt2, 2),
                    TaxRate:      Math.Round(Dec(r, "TAXRAT2"), 4),
                    TaxAmount:    Math.Round(tax2, 2),
                    Condition:    Str(r, "TAXCOND2"),
                    IsLate:       late));
        }

        return Task.FromResult<IReadOnlyList<ExpressWhtEntryDto>>(result);
    }

    public Task<IReadOnlyList<ExpressCustomerDto>> ReadCustomersAsync(string companyFolderPath, CancellationToken ct = default)
    {
        List<DbfRow> records;
        try { records = ReadDbf(companyFolderPath, "ARMAS"); }
        catch (FileNotFoundException) { return Task.FromResult<IReadOnlyList<ExpressCustomerDto>>([]); }

        var result = new List<ExpressCustomerDto>();
        foreach (var r in records)
        {
            var code = Str(r, "CUSCOD");
            if (string.IsNullOrWhiteSpace(code)) continue;

            var addr = string.Join(" ", new[] { Str(r, "ADDR01"), Str(r, "ADDR02"), Str(r, "ADDR03"), Str(r, "ZIPCOD") }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            var remark = Str(r, "REMARK");

            result.Add(new ExpressCustomerDto(
                CustomerCode:      code,
                Prefix:            Str(r, "PRENAM"),
                Name:              Str(r, "CUSNAM"),
                TaxId:             Str(r, "TAXID"),
                Address:           addr,
                Phone:             Str(r, "TELNUM"),
                Contact:           Str(r, "CONTACT"),
                Email:             ExtractEmail(remark),
                PaymentTermDays:   (int)Dec(r, "PAYTRM"),
                PaymentCondition:  Str(r, "PAYCOND"),
                GlAccountCode:     Str(r, "ACCNUM"),
                Remark:            remark,
                IsActive:          Str(r, "STATUS").Equals("A", StringComparison.OrdinalIgnoreCase)));
        }

        return Task.FromResult<IReadOnlyList<ExpressCustomerDto>>(result);
    }

    public Task<IReadOnlyList<ExpressArInvoiceDto>> ReadArInvoicesAsync(string companyFolderPath, CancellationToken ct = default)
    {
        List<DbfRow> records;
        try { records = ReadDbf(companyFolderPath, "ARTRN"); }
        catch (FileNotFoundException) { return Task.FromResult<IReadOnlyList<ExpressArInvoiceDto>>([]); }

        var result = new List<ExpressArInvoiceDto>();
        foreach (var r in records)
        {
            if (Str(r, "RECTYP") != "3") continue;   // เฉพาะใบแจ้งหนี้ (IV); RECTYP 9 = ใบเสร็จ (RE)
            var docNum = Str(r, "DOCNUM");
            var docDat = Date(r, "DOCDAT");
            if (string.IsNullOrWhiteSpace(docNum) || docDat is null) continue;

            result.Add(new ExpressArInvoiceDto(
                DocumentNo:         docNum,
                DocumentDate:       docDat.Value,
                DueDate:            Date(r, "DUEDAT"),
                CustomerCode:       Str(r, "CUSCOD"),
                Amount:             Math.Round(Dec(r, "AFTDISC"), 2),
                VatRate:            Math.Round(Dec(r, "VATRAT"), 4),
                VatAmount:          Math.Round(Dec(r, "VATAMT"), 2),
                NetAmount:          Math.Round(Dec(r, "NETAMT"), 2),
                ReceivedAmount:     Math.Round(Dec(r, "RCVAMT"), 2),
                OutstandingAmount:  Math.Round(Dec(r, "REMAMT"), 2),
                IsCompleted:        Str(r, "CMPLAPP").Equals("Y", StringComparison.OrdinalIgnoreCase),
                VatPeriod:          Date(r, "VATPRD"),
                Reference:          Str(r, "YOUREF")));
        }

        return Task.FromResult<IReadOnlyList<ExpressArInvoiceDto>>(result);
    }

    public Task<IReadOnlyList<ExpressSupplierDto>> ReadSuppliersAsync(string companyFolderPath, CancellationToken ct = default)
    {
        List<DbfRow> records;
        try { records = ReadDbf(companyFolderPath, "APMAS"); }
        catch (FileNotFoundException) { return Task.FromResult<IReadOnlyList<ExpressSupplierDto>>([]); }

        var result = new List<ExpressSupplierDto>();
        foreach (var r in records)
        {
            var code = Str(r, "SUPCOD");
            if (string.IsNullOrWhiteSpace(code)) continue;

            var addr = string.Join(" ", new[] { Str(r, "ADDR01"), Str(r, "ADDR02"), Str(r, "ADDR03"), Str(r, "ZIPCOD") }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            var remark = Str(r, "REMARK");

            result.Add(new ExpressSupplierDto(
                SupplierCode:      code,
                Prefix:            Str(r, "PRENAM"),
                Name:              Str(r, "SUPNAM"),
                TaxId:             Str(r, "TAXID"),
                Address:           addr,
                Phone:             Str(r, "TELNUM"),
                Contact:           Str(r, "CONTACT"),
                Email:             ExtractEmail(remark),
                PaymentTermDays:   (int)Dec(r, "PAYTRM"),
                PaymentCondition:  Str(r, "PAYCOND"),
                GlAccountCode:     Str(r, "ACCNUM"),
                Remark:            remark,
                IsActive:          Str(r, "STATUS") != "0"));   // AP ใช้รหัสสถานะผสม ('2'/'A'/''); ถือว่า active เว้นแต่ '0'
        }

        return Task.FromResult<IReadOnlyList<ExpressSupplierDto>>(result);
    }

    public Task<IReadOnlyList<ExpressApInvoiceDto>> ReadApInvoicesAsync(string companyFolderPath, CancellationToken ct = default)
    {
        List<DbfRow> records;
        try { records = ReadDbf(companyFolderPath, "APTRN"); }
        catch (FileNotFoundException) { return Task.FromResult<IReadOnlyList<ExpressApInvoiceDto>>([]); }

        var result = new List<ExpressApInvoiceDto>();
        foreach (var r in records)
        {
            if (Str(r, "RECTYP") != "3") continue;   // เฉพาะใบตั้งหนี้ซื้อ (RR); 9=จ่าย(PS), 7=OE, 1=HP
            var docNum = Str(r, "DOCNUM");
            var docDat = Date(r, "DOCDAT");
            if (string.IsNullOrWhiteSpace(docNum) || docDat is null) continue;

            var yourRef = Str(r, "YOUREF");
            result.Add(new ExpressApInvoiceDto(
                DocumentNo:         docNum,
                DocumentDate:       docDat.Value,
                DueDate:            Date(r, "DUEDAT"),
                SupplierCode:       Str(r, "SUPCOD"),
                Amount:             Math.Round(Dec(r, "AFTDISC"), 2),
                VatRate:            Math.Round(Dec(r, "VATRAT"), 4),
                VatAmount:          Math.Round(Dec(r, "VATAMT"), 2),
                NetAmount:          Math.Round(Dec(r, "NETAMT"), 2),
                PaidAmount:         Math.Round(Dec(r, "PAYAMT"), 2),
                OutstandingAmount:  Math.Round(Dec(r, "REMAMT"), 2),
                IsCompleted:        Str(r, "CMPLAPP").Equals("Y", StringComparison.OrdinalIgnoreCase),
                VatPeriod:          Date(r, "VATPRD"),
                Reference:          string.IsNullOrWhiteSpace(yourRef) ? Str(r, "REFNUM") : yourRef));
        }

        return Task.FromResult<IReadOnlyList<ExpressApInvoiceDto>>(result);
    }

    /// <summary>ดึงอีเมลจากข้อความ (เช่น REMARK "E-MAIL:foo@bar.com") — คืน null ถ้าไม่พบ</summary>
    private static string? ExtractEmail(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var m = System.Text.RegularExpressions.Regex.Match(text, @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}");
        return m.Success ? m.Value : null;
    }
}
