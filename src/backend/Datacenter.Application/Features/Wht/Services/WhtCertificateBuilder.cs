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
            IssueDate:    e.WithholdDate);
    }
}
