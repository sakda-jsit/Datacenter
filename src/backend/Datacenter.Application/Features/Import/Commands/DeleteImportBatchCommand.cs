using MediatR;

namespace Datacenter.Application.Features.Import.Commands;

/// <summary>
/// ลบ import batch และข้อมูลที่เกี่ยวข้องทั้งหมด: staging (accounts/TB), validation details
/// และถ้า batch นั้น post แล้ว จะลบ JournalEntry ที่สร้างจาก batch นั้นด้วย (undo การ post)
/// ไม่ลบผังบัญชี (Account) เพราะอาจถูกใช้ร่วมกับ batch/รายการอื่น
/// </summary>
public record DeleteImportBatchCommand(int ImportBatchId) : IRequest;
