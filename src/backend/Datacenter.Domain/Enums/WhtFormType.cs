namespace Datacenter.Domain.Enums;

/// <summary>
/// แบบยื่นภาษีหัก ณ ที่จ่าย (จาก Express ISTAX.TAXTYP)
/// - Pnd3:  ภ.ง.ด.3 — ผู้ถูกหักเป็น **บุคคลธรรมดา** (TAXTYP='S03')
/// - Pnd53: ภ.ง.ด.53 — ผู้ถูกหักเป็น **นิติบุคคล** (TAXTYP='S53')
/// </summary>
public enum WhtFormType
{
    Pnd3 = 3,
    Pnd53 = 53,
}
