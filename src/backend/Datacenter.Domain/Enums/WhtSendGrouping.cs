namespace Datacenter.Domain.Enums;

/// <summary>รูปแบบการจัดกลุ่มผู้รับอีเมลหนังสือรับรองหัก ณ ที่จ่าย</summary>
public enum WhtSendGrouping
{
    /// <summary>รวมตามผู้ถูกหัก — 1 อีเมล/ผู้ถูกหัก (หลายฉบับของคนเดียวรวมส่งครั้งเดียว)</summary>
    ByPayee = 0,

    /// <summary>รวมส่งเมลเดียว — ทุกฉบับที่เลือกแนบรวมในอีเมลเดียว ส่งไปยังอีเมลเดียวที่ระบุ</summary>
    Single = 1,
}
