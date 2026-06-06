namespace Datacenter.Domain.Enums;

/// <summary>ประเภทแบบยื่นภาษี/เงินสมทบที่ติดตามสถานะ (นอกเหนือจาก สปส.1-10 ที่มี entity แยก)</summary>
public enum StatutoryFilingType
{
    Pnd1 = 1,    // ภ.ง.ด.1 (หัก ณ ที่จ่ายเงินเดือน รายเดือน)
    Pnd1k = 2,   // ภ.ง.ด.1ก (สรุปรายปี)
    Kt20 = 3,    // กท.20ก (เงินค่าจ้างประจำปี กองทุนเงินทดแทน)
}

/// <summary>สถานะการยื่นแบบทั่วไป</summary>
public enum FilingStatus
{
    NotFiled = 0,         // ยังไม่ยื่น
    Filed = 1,            // ยื่นแล้ว
    ReceiptReceived = 2,  // ได้รับใบเสร็จ/ชำระแล้ว
}
