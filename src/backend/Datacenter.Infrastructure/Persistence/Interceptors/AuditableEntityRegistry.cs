using Datacenter.Domain.Entities;

namespace Datacenter.Infrastructure.Persistence.Interceptors;

/// <summary>
/// รายชื่อ entity ที่ผู้ใช้แก้ไขได้ และต้องเก็บ field-level audit (docs/18 §10-15).
/// จงใจ whitelist เฉพาะ master/working-paper/เอกสารที่ "ผู้ใช้แก้เอง" —
/// ไม่รวมข้อมูลที่มาจาก Express (Account/Customer/Supplier/AR/AP/VAT/WHT/Stock/Bank/Journal)
/// หรือ staging/snapshot/audit ที่เขียนเป็นชุดจำนวนมาก (จะ flood + ไม่ใช่ user edit).
/// </summary>
internal static class AuditableEntityRegistry
{
    private static readonly HashSet<Type> Audited =
    [
        typeof(AdjustmentEntry),
        typeof(LeaseContract),
        typeof(FixedAsset),
        typeof(AssetAccountMapping),
        typeof(AssetTypeMaster),
        typeof(PrepaidExpense),
        typeof(Attachment),
        typeof(ReportPackage),
        typeof(AccountStatementMapping),
        typeof(FsExternalInput),
        typeof(CashCount),
        typeof(InterestBearingLoan),
        typeof(WhtPayee),
        typeof(ClientCompany),
        typeof(Employee),
        typeof(PayrollAccountMapping),
        typeof(PayrollRateConfig),
        typeof(StatutoryFiling),
        typeof(ExpressPostingLink),
        typeof(SsoEnrollment),
        typeof(NoteTemplateSection),
        typeof(TaxComputation),    // กระดาษทำการ ภ.ง.ด.50 (อัตรา/ขาดทุนยกมา/WHT)
        typeof(VatBranchMapping),  // แมพเลขสาขา ภ.พ.30 (DEPCOD→RD)
        typeof(CompanyAuditor),    // ผู้สอบบัญชีต่อรอบปี (ชื่อ/ทะเบียน/เลขผู้เสียภาษี/วันลงนาม)
    ];

    /// <summary>property ที่ไม่ต้องลง audit (metadata ของ BaseEntity)</summary>
    public static readonly HashSet<string> IgnoredProperties =
    [
        "Id", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy",
    ];

    public static bool IsAudited(Type entityType) => Audited.Contains(entityType);
}
