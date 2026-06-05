using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// แมพหมวดบัญชีสินทรัพย์ของ Express (FAMAS.ACCCOD เช่น VEH/EQU/TOL) → บัญชี GL 3 ตัว ต่อบริษัท.
/// ใช้ตอน import FAMAS เพื่อเติมบัญชีให้สินทรัพย์อัตโนมัติ (Express เก็บแค่หมวด ไม่เก็บเลขบัญชีจริง).
/// </summary>
public class AssetAccountMapping : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>หมวดจาก Express (ACCCOD)</summary>
    public string CategoryCode { get; set; } = string.Empty;

    /// <summary>คำอธิบายหมวด (กรอกเอง เช่น "ยานพาหนะ")</summary>
    public string? Description { get; set; }

    public int? AssetAccountId { get; set; }
    public int? AccumDepreciationAccountId { get; set; }
    public int? DepreciationExpenseAccountId { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
}
