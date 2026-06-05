namespace Datacenter.Domain.Enums;

/// <summary>
/// ประเภทรายการภาษีมูลค่าเพิ่ม (จาก Express ISVAT.VATREC)
/// - Output: ภาษีขาย (VATREC='S') — ภาษีที่เรียกเก็บจากลูกค้า
/// - Input:  ภาษีซื้อ (VATREC='P') — ภาษีที่จ่ายให้ผู้ขาย มีสิทธิ์ขอคืน/เครดิต
/// </summary>
public enum VatEntryType
{
    Output = 1,
    Input = 2,
}
