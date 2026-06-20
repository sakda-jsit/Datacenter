namespace Datacenter.Application.Features.CorporateTax.Queries;

/// <summary>
/// ตารางอ้างอิงบรรทัดงบดุล ภ.ง.ด.50 (รายการที่ 9) สำหรับ Page7 builder:
/// แมพ field ใน <c>Pnd50Page7Data</c> ↔ รหัส BS_* (override ต่อบัญชี, ScheduleNo=9) และ
/// ↔ RefCode ผังงบ (ค่า default). ใช้ย้ายยอดการจัดประเภทต่อบัญชีโดยไม่กระทบยอดรวม.
/// </summary>
internal static class Pnd50BsLines
{
    /// <summary>รหัส BS_* (Cit50ScheduleLine ScheduleNo=9) → ชื่อ field ใน Pnd50Page7Data.</summary>
    public static readonly IReadOnlyDictionary<string, string> FieldByCode = new Dictionary<string, string>
    {
        ["BS_CASH"]        = "Cash",
        ["BS_AR"]          = "Ar",
        ["BS_INV"]         = "Inventory",
        ["BS_OTHER_CA"]    = "OtherCurrentAsset",
        ["BS_LOANS_REL"]   = "LoansToRelated",
        ["BS_LAND_BLDG"]   = "Ppe",
        ["BS_OTHER_ASSET"] = "OtherAssetNet",
        ["BS_OTHER_NCA"]   = "OtherNonCurrentAsset",
        ["BS_BANK_OD"]     = "BankOdShortLoan",
        ["BS_AP"]          = "Ap",
        ["BS_CUR_LOAN"]    = "CurrentLoan",
        ["BS_OTHER_CL"]    = "OtherCurrentLiab",
        ["BS_LT_LOAN"]     = "LongTermLoan",
        ["BS_OTHER_NCL"]   = "OtherNonCurrentLiab",
    };

    /// <summary>RefCode ผังงบ → field default ใน Pnd50Page7Data (ค่าเริ่มต้นก่อน override).</summary>
    public static readonly IReadOnlyDictionary<string, string> FieldByRefCode = new Dictionary<string, string>
    {
        ["A1"] = "Cash", ["A7"] = "Ar", ["A3"] = "Inventory",
        ["A2"] = "OtherCurrentAsset", ["A4"] = "OtherCurrentAsset", ["TXR"] = "OtherCurrentAsset",
        ["A8"] = "LoansToRelated", ["A5"] = "Ppe",
        ["A9"] = "OtherAssetNet", ["A10"] = "OtherAssetNet", ["A6"] = "OtherNonCurrentAsset",
        ["L3"] = "BankOdShortLoan", ["L1"] = "Ap", ["L5"] = "CurrentLoan",
        ["L2"] = "OtherCurrentLiab", ["TXP"] = "OtherCurrentLiab",
        ["L6"] = "LongTermLoan", ["L4"] = "OtherNonCurrentLiab",
    };

    private static readonly HashSet<string> AssetFields = new()
    {
        "Cash", "Ar", "Inventory", "OtherCurrentAsset", "LoansToRelated",
        "Ppe", "OtherAssetNet", "OtherNonCurrentAsset",
    };

    public static bool IsAssetField(string field) => AssetFields.Contains(field);
}
