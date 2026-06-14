using Datacenter.Application.Features.CorporateTax.DTOs;
using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.CorporateTax.Services;

/// <summary>
/// คำนวณภาษีเงินได้นิติบุคคล (ภ.ง.ด.50) แบบ pure — ไม่แตะ DB (docs/16 §1).
///
/// ลำดับการคำนวณ:
/// 1. กำไรสุทธิทางบัญชีก่อนภาษี (จากงบกำไรขาดทุน)
/// 2. + บวกกลับ (B) − หักออก (C) = กำไรสุทธิทางภาษีก่อนหักขาดทุนสะสม (adjustedProfit)
/// 3. − ผลขาดทุนสะสมยกมา (หักได้ไม่เกินกำไร, ไม่ทำให้ติดลบ) = เงินได้สุทธิเพื่อเสียภาษี
/// 4. ภาษี = อัตราตาม RateScheme (SME ขั้นบันได / 20% / กำหนดเอง)
/// 5. − ภาษีจ่ายล่วงหน้า (WHT) = ภาษีค้างชำระ (>0) หรือ ภาษีชำระเกินขอคืน (<0)
/// 6. ผลขาดทุนสะสมยกไป: E = prev − ที่ใช้ไป (กำไร) หรือ prev + ขาดทุนปีนี้ (ขาดทุน)
/// </summary>
public static class CorporateTaxEngine
{
    // อัตราขั้นบันได SME (พ.ร.ฎ. 530 — นิติบุคคลขนาดเล็ก)
    private const decimal SmeExemptUpTo = 300_000m;       // 0%
    private const decimal SmeMidUpTo    = 3_000_000m;     // 15% ของส่วน 300,001–3,000,000
    private const decimal SmeMidRate    = 15m;
    private const decimal SmeTopRate    = 20m;            // 20% ของส่วนเกิน 3,000,000

    public static TaxComputationResult Compute(
        decimal netProfitBeforeTax,
        decimal addBackTotal,
        decimal deductionTotal,
        decimal lossBroughtForward,
        decimal whtCredit,
        TaxRateScheme scheme,
        decimal? customRatePct)
    {
        var adjustedProfit = Math.Round(netProfitBeforeTax + addBackTotal - deductionTotal, 2);

        // หักขาดทุนสะสมยกมา — ได้ไม่เกินกำไร และไม่หักถ้าปีนี้ขาดทุน
        var lossAvailable = Math.Max(0m, lossBroughtForward);
        var lossUsed = adjustedProfit > 0m ? Math.Min(adjustedProfit, lossAvailable) : 0m;
        var netTaxableIncome = Math.Max(0m, adjustedProfit - lossUsed);

        var brackets = BuildBrackets(netTaxableIncome, scheme, customRatePct);
        var taxAmount = Math.Round(brackets.Sum(b => b.Tax), 2);

        var netPayable = Math.Round(taxAmount - whtCredit, 2);

        // ผลขาดทุนสะสมยกไปปีถัดไป
        decimal lossCarriedForward;
        if (adjustedProfit < 0m)
            lossCarriedForward = Math.Round(lossAvailable + (-adjustedProfit), 2); // เพิ่มขาดทุนปีนี้
        else
            lossCarriedForward = Math.Round(lossAvailable - lossUsed, 2);          // ใช้ไปบางส่วน

        return new TaxComputationResult(
            NetProfitBeforeTax: Math.Round(netProfitBeforeTax, 2),
            AddBackTotal: Math.Round(addBackTotal, 2),
            DeductionTotal: Math.Round(deductionTotal, 2),
            AdjustedProfit: adjustedProfit,
            LossBroughtForward: Math.Round(lossAvailable, 2),
            LossUsed: Math.Round(lossUsed, 2),
            NetTaxableIncome: netTaxableIncome,
            Brackets: brackets,
            TaxAmount: taxAmount,
            WhtCredit: Math.Round(whtCredit, 2),
            NetPayable: netPayable,
            LossCarriedForward: lossCarriedForward);
    }

    private static IReadOnlyList<TaxBracketDto> BuildBrackets(
        decimal income, TaxRateScheme scheme, decimal? customRatePct)
    {
        if (income <= 0m)
            return new List<TaxBracketDto>();

        switch (scheme)
        {
            case TaxRateScheme.Flat20:
                return new List<TaxBracketDto>
                {
                    new("กำไรสุทธิทั้งจำนวน", income, 20m, Math.Round(income * 0.20m, 2)),
                };

            case TaxRateScheme.Custom:
                var rate = customRatePct ?? 0m;
                return new List<TaxBracketDto>
                {
                    new($"กำไรสุทธิทั้งจำนวน (อัตรา {rate:0.##}%)", income, rate,
                        Math.Round(income * rate / 100m, 2)),
                };

            case TaxRateScheme.SmeTiered:
            default:
                var brackets = new List<TaxBracketDto>();

                // ขั้น 1: 0–300,000 ยกเว้น
                var b1 = Math.Min(income, SmeExemptUpTo);
                brackets.Add(new("0 – 300,000 (ยกเว้น)", b1, 0m, 0m));

                // ขั้น 2: 300,001–3,000,000 = 15%
                if (income > SmeExemptUpTo)
                {
                    var b2 = Math.Min(income, SmeMidUpTo) - SmeExemptUpTo;
                    brackets.Add(new("300,001 – 3,000,000", b2, SmeMidRate,
                        Math.Round(b2 * SmeMidRate / 100m, 2)));
                }

                // ขั้น 3: ส่วนเกิน 3,000,000 = 20%
                if (income > SmeMidUpTo)
                {
                    var b3 = income - SmeMidUpTo;
                    brackets.Add(new("ส่วนเกิน 3,000,000", b3, SmeTopRate,
                        Math.Round(b3 * SmeTopRate / 100m, 2)));
                }

                return brackets;
        }
    }
}
