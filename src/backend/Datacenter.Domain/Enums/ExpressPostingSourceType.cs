namespace Datacenter.Domain.Enums;

/// <summary>ประเภทรายการที่ต้องคีย์ลง Express (ปลายทางบัญชี)</summary>
public enum ExpressPostingSourceType
{
    PayrollExpense = 1,  // ค่าใช้จ่ายเงินเดือน (รายเดือน)
    SsoRemittance = 2,   // นำส่งเงินสมทบ ปกส. (รายเดือน)
    WcfInvoice = 3,      // ใบแจ้งหนี้กองทุนเงินทดแทน (รายปี)
    WcfRemittance = 4,   // นำส่งเงินสมทบกองทุนเงินทดแทน (รายปี)
}
