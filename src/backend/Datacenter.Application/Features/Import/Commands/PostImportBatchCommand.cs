using Datacenter.Application.Features.Import.DTOs;
using MediatR;

namespace Datacenter.Application.Features.Import.Commands;

/// <summary>
/// ยกข้อมูลจาก staging (StagingAccount + StagingTrialBalance ชุด CUR) ของ batch ที่นำเข้าสำเร็จ
/// ไปยังตารางจริง (Account + JournalEntry/JournalEntryLine) เพื่อให้ Trial Balance / GL /
/// Financial Statement / Closing ใช้งานได้จริง — สามารถ post ซ้ำได้ (replace ของเดิมในปีนั้น)
/// </summary>
public record PostImportBatchCommand(int ImportBatchId)
    : IRequest<PostImportResultDto>;
