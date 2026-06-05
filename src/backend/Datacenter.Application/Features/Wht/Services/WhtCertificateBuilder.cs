using Datacenter.Application.Common;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.Wht.Services;

/// <summary>สร้าง <see cref="WhtCertificateModel"/> จาก WhtEntry + บริษัทผู้หักภาษี (ใช้ทั้ง preview และส่งเมล)</summary>
public static class WhtCertificateBuilder
{
    public static WhtCertificateModel Build(WhtEntry e, ClientCompany payer)
    {
        var formLabel = e.FormType == WhtFormType.Pnd3 ? "ภ.ง.ด.3" : "ภ.ง.ด.53";
        var payerName = !string.IsNullOrWhiteSpace(payer.LegalName) ? payer.LegalName : payer.Name;

        var seqNo = !string.IsNullOrWhiteSpace(e.DocumentNo) ? e.DocumentNo
                  : (e.ReferenceNo ?? "");

        return new WhtCertificateModel(
            FormLabel:    formLabel,
            SequenceNo:   seqNo,
            PayerName:    payerName,
            PayerTaxId:   payer.TaxId ?? "",
            PayerAddress: payer.Address,
            PayeeName:    string.IsNullOrWhiteSpace(e.PayeePrefix) ? (e.PayeeName ?? "") : $"{e.PayeePrefix} {e.PayeeName}".Trim(),
            PayeeTaxId:   e.PayeeTaxId ?? "",
            PayeeAddress: e.PayeeAddress,
            IncomeType:   string.IsNullOrWhiteSpace(e.IncomeType) ? "อื่น ๆ" : e.IncomeType!,
            PayDate:      e.WithholdDate,
            Amount:       e.BaseAmount,
            TaxAmount:    e.TaxAmount,
            TaxRate:      e.TaxRate,
            AmountInWords: ThaiBahtText.Convert(e.TaxAmount),
            IssueDate:    e.WithholdDate,
            IncomeCategory: ClassifyIncome(e.IncomeType),
            ConditionType:  ClassifyCondition(e.Condition),
            PayerBranchCode: payer.BranchCode,
            PayerSignature:  payer.SignatureImage);
    }

    /// <summary>
    /// จัดหมวดประเภทเงินได้ตามแบบ 50 ทวิ จากข้อความ IncomeType (best-effort ด้วยคีย์เวิร์ด):
    /// 1=40(1) เงินเดือน, 2=40(2) ค่าธรรมเนียม/นายหน้า/วิชาชีพ, 3=40(3) ลิขสิทธิ์,
    /// 41=40(4)(ก) ดอกเบี้ย, 42=40(4)(ข) เงินปันผล, 5=มาตรา 3 เตรส (ค่าจ้างทำของ/บริการ/ขนส่ง/เช่า/โฆษณา/ประกันฯ),
    /// 6=อื่นๆ. จัดไม่ได้ → 6 (ใส่ชื่อประเภทในช่องระบุ).
    /// </summary>
    private static int ClassifyIncome(string? incomeType)
    {
        var s = (incomeType ?? "").Trim();
        if (s.Length == 0) return 6;
        bool Has(params string[] kw) => kw.Any(k => s.Contains(k));

        if (Has("เงินเดือน", "ค่าจ้างแรงงาน", "เบี้ยเลี้ยง", "โบนัส")) return 1;
        if (Has("ลิขสิทธิ์", "กู๊ดวิลล์", "สิทธิบัตร")) return 3;
        if (Has("ดอกเบี้ย")) return 41;
        if (Has("ปันผล", "ส่วนแบ่งกำไร", "ส่วนแบ่งของกำไร")) return 42;
        if (Has("ค่าธรรมเนียม", "นายหน้า", "ค่าสอบบัญชี", "ที่ปรึกษา", "วิชาชีพ", "ค่าตอบแทน")) return 2;
        // มาตรา 3 เตรส (ข้อ 5): ค่าจ้างทำของ/ขนส่ง/โฆษณา/ประกันวินาศภัย/รางวัล-ส่งเสริมการขาย —
        // ตรงกับการจัดของ Express (ค่าจ้างทำของ→5). บริการทั่วไป/อื่นๆ ลงข้อ 6 (ใส่ชื่อในช่องระบุ)
        if (Has("ค่าจ้างทำของ", "ค่าขนส่ง", "ค่าโฆษณา", "เบี้ยประกันวินาศภัย",
                "ส่งเสริมการขาย", "รางวัล", "เตรส")) return 5;
        return 6;
    }

    /// <summary>เงื่อนไขการออกภาษีจาก Condition (1=หัก ณ ที่จ่าย default, 2=ออกให้ตลอดไป, 3=ออกครั้งเดียว)</summary>
    private static int ClassifyCondition(string? condition)
    {
        var s = (condition ?? "").Trim();
        if (s.Contains("ตลอด") || s is "2") return 2;
        if (s.Contains("ครั้งเดียว") || s is "3") return 3;
        return 1;
    }
}
