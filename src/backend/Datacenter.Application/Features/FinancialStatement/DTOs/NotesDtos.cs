namespace Datacenter.Application.Features.FinancialStatement.DTOs;

/// <summary>หมายเหตุประกอบงบการเงิน (NOTE2) — เอกสารรวม ส่วนข้อความ (แก้ได้) + ส่วนตัวเลข (auto).</summary>
public record NotesToFsDto(
    int ClientCompanyId,
    string ClientName,
    string TaxId,
    string? Address,
    int FiscalYear,
    int PriorYear,
    /// <summary>"สำหรับปีสิ้นสุดวันที่ 31 ธันวาคม 2568"</summary>
    string PeriodLabel,
    IReadOnlyList<NoteNarrativeDto> Narratives,
    IReadOnlyList<NoteScheduleDto> Schedules,
    IReadOnlyList<NoteMovementDto> Movements,
    NoteCostOfSalesDto? CostOfSales);

/// <summary>หมายเหตุข้อความบรรยาย (มาจาก template ที่แก้ได้ + แทน placeholder).</summary>
public record NoteNarrativeDto(
    string NoteNo,
    string Title,
    /// <summary>ย่อหน้าคั่นด้วย \n (แทน placeholder แล้ว)</summary>
    string Body,
    int SortOrder,
    /// <summary>ปีที่มีผลของ template ที่เลือกใช้ (ให้ UI แสดง)</summary>
    int EffectiveYear,
    /// <summary>true = มี template เฉพาะบริษัทนี้ (override default กลาง)</summary>
    bool IsCompanyOverride);

/// <summary>หนึ่งบรรทัดในตารางตัวเลข (บัญชีย่อย) ปีปัจจุบัน/ปีก่อน.</summary>
public record NoteRowDto(
    string Label,
    decimal CurrentYear,
    decimal PriorYear);

/// <summary>หมายเหตุแบบตาราง breakdown บัญชี (ดึงจาก TB อัตโนมัติ).</summary>
public record NoteScheduleDto(
    string NoteNo,
    string Title,
    int SortOrder,
    IReadOnlyList<NoteRowDto> Rows,
    decimal TotalCurrent,
    decimal TotalPrior);

/// <summary>หนึ่งบรรทัดตารางการเคลื่อนไหว (ราคาทุน หรือ ค่าเสื่อมสะสม) แยกตามประเภทสินทรัพย์.</summary>
public record NoteMovementRowDto(
    string Label,
    decimal Opening,
    decimal Additions,
    decimal Disposals,
    decimal Closing);

/// <summary>หมายเหตุที่ดิน อาคาร อุปกรณ์ / สินทรัพย์ไม่มีตัวตน — ตารางเคลื่อนไหว (จาก FA register).</summary>
public record NoteMovementDto(
    string NoteNo,
    string Title,
    int SortOrder,
    IReadOnlyList<NoteMovementRowDto> CostRows,
    NoteMovementRowDto CostTotal,
    IReadOnlyList<NoteMovementRowDto> AccumRows,
    NoteMovementRowDto AccumTotal,
    /// <summary>มูลค่าสุทธิยกมา (ต้นปี) = ทุนยกมา − ค่าเสื่อมสะสมยกมา</summary>
    decimal NetOpening,
    /// <summary>มูลค่าสุทธิคงเหลือสิ้นปี = ทุนสิ้นปี − ค่าเสื่อมสะสมสิ้นปี</summary>
    decimal NetClosing,
    /// <summary>ค่าเสื่อม/ค่าตัดจำหน่ายสำหรับปี (ปีปัจจุบัน)</summary>
    decimal ChargeForYear,
    /// <summary>ค่าเสื่อม/ค่าตัดจำหน่ายสำหรับปีก่อน</summary>
    decimal ChargeForYearPrior);

/// <summary>หมายเหตุต้นทุนขายหรือต้นทุนการให้บริการ (6.13) — schedule เฉพาะ.</summary>
public record NoteCostOfSalesDto(
    string NoteNo,
    string Title,
    int SortOrder,
    decimal OpeningInventoryCurrent,
    decimal OpeningInventoryPrior,
    /// <summary>องค์ประกอบต้นทุน (ซื้อสินค้า/ค่าขนส่ง/ค่าแรงทางตรง ฯลฯ) จากบัญชีกลุ่มต้นทุน</summary>
    IReadOnlyList<NoteRowDto> Components,
    decimal ClosingInventoryCurrent,
    decimal ClosingInventoryPrior,
    decimal TotalCurrent,
    decimal TotalPrior);

/// <summary>รายการ template ข้อความ (สำหรับหน้าแก้ไข).</summary>
public record NoteTemplateSectionDto(
    int Id,
    int? ClientCompanyId,
    int EffectiveYear,
    string NoteKey,
    string Title,
    string BodyText,
    int SortOrder);
