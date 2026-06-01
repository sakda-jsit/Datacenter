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
        => (rec[field]?.ToString() ?? "").Trim();

    private static decimal Dec(DbfRow rec, string field)
        => rec[field] switch
        {
            decimal d => d,
            double dbl => (decimal)dbl,
            int i => i,
            long l => l,
            _ => 0m,
        };

    private static decimal MonthSum(DbfRow rec, string prefix, string suffix)
        => Enumerable.Range(1, 12).Sum(m => Dec(rec, $"{prefix}{m}{suffix}"));

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

        var dto = new ExpressCompanyInfoDto(
            ThaiName : Str(r, "THINAM"),
            EngName  : Str(r, "ENGNAM"),
            TaxId    : Str(r, "TAXID"),
            VatRate  : Dec(r, "VATRAT"),
            YearThai : (int)Dec(r, "YEARTHAI"));

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
}
