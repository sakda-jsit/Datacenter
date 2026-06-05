namespace Datacenter.Application.Common.Interfaces;

/// <summary>ประมวลผลรูปลายเซ็น — ตัดขอบ/ช่องว่าง (โปร่งใสหรือพื้นขาว) ที่ไม่ใช่ตัวลายเซ็นออก</summary>
public interface ISignatureImageProcessor
{
    /// <summary>ตัดพื้นที่ว่างรอบลายเซ็นออก คืน PNG ที่ครอปแล้ว (ถ้าประมวลผลไม่ได้คืนรูปเดิม)</summary>
    byte[] TrimWhitespace(byte[] image);
}
