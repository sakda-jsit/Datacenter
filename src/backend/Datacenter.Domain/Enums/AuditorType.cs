namespace Datacenter.Domain.Enums;

/// <summary>ประเภทผู้สอบบัญชี (กำหนดขอบเขตงานตามกฎหมาย).</summary>
public enum AuditorType
{
    /// <summary>ผู้สอบบัญชีรับอนุญาต (CPA) — สภาวิชาชีพบัญชี; สอบได้ทุกนิติบุคคล</summary>
    Cpa = 1,

    /// <summary>ผู้สอบบัญชีภาษีอากร (TA) — กรมสรรพากร; สอบได้เฉพาะห้างหุ้นส่วนจดทะเบียนขนาดเล็ก</summary>
    TaxAuditor = 2,
}
