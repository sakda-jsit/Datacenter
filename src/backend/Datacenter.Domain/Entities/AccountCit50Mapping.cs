using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// แมพบัญชีของบริษัท → บรรทัด schedule CIT50 (รายการ 4-11) — ต่อบริษัท (เหมือน AccountStatementMapping).
/// บัญชีที่ไม่ถูกแมพ → ลงบรรทัด "อื่นๆ" (catch-all) อัตโนมัติตอนเติม PDF.
/// </summary>
public class AccountCit50Mapping : BaseEntity
{
    public int ClientCompanyId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;

    /// <summary>รหัสบรรทัด CIT50 (Cit50ScheduleLine.Code)</summary>
    public string Cit50LineCode { get; set; } = string.Empty;

    public ClientCompany ClientCompany { get; set; } = null!;
}
