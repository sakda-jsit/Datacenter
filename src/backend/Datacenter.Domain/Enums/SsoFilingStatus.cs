namespace Datacenter.Domain.Enums;

/// <summary>สถานะการยื่น สปส.1-10 รายเดือน</summary>
public enum SsoFilingStatus
{
    NotFiled = 0,         // ยังไม่ยื่น
    Filed = 1,            // ยื่นแล้ว (มี SubmittedDate)
    ReceiptReceived = 2,  // ได้รับใบเสร็จแล้ว
}
