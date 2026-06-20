using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJournalEntryFiscalYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FiscalYear",
                table: "JournalEntries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Backfill: ปัจจุบันทุกแถวเป็น OPEN-{y}/MOVE-{y} (มีแต่ ExpressPostingService ที่เขียน
            // JournalEntry) → parse ปีจากเลขหลังขีดใน DocumentNo
            migrationBuilder.Sql(@"
                UPDATE JournalEntries
                SET FiscalYear = TRY_CONVERT(int, SUBSTRING(DocumentNo, CHARINDEX('-', DocumentNo) + 1, 50))
                WHERE (DocumentNo LIKE 'OPEN-%' OR DocumentNo LIKE 'MOVE-%')
                  AND TRY_CONVERT(int, SUBSTRING(DocumentNo, CHARINDEX('-', DocumentNo) + 1, 50)) IS NOT NULL;");

            // Fallback กันแถวที่ไม่ตรง pattern (ปัจจุบันไม่มี) — ใช้ตรรกะเดิม: OPEN ลงวันที่ (Y-1)-12-31
            // → ปี = YEAR+1, อื่น ๆ = YEAR(JournalDate)
            migrationBuilder.Sql(@"
                UPDATE JournalEntries
                SET FiscalYear = CASE WHEN SourceModule = 'OpeningBalance'
                                      THEN YEAR(JournalDate) + 1 ELSE YEAR(JournalDate) END
                WHERE FiscalYear = 0;");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_ClientCompanyId_FiscalYear",
                table: "JournalEntries",
                columns: new[] { "ClientCompanyId", "FiscalYear" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_ClientCompanyId_FiscalYear",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "FiscalYear",
                table: "JournalEntries");
        }
    }
}
