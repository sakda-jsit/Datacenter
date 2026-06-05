using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientLegalNameAndProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnglishName",
                table: "ClientCompanies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "ClientCompanies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            // backfill: ชื่อทางการเริ่มต้น = ชื่อจาก Express (Name) สำหรับข้อมูลเดิม
            migrationBuilder.Sql("UPDATE [ClientCompanies] SET [LegalName] = [Name] WHERE [LegalName] = '' OR [LegalName] IS NULL;");

            // dedup ก่อนสร้าง unique business key: ถ้ามี (TaxId,BranchCode) ซ้ำในรายการ active
            // → คงตัวที่มีข้อมูลมากสุด/ไม่ใช่สำเนา (COPY/X-/Z-)/Id น้อยสุด, ปิดใช้งานตัวที่เหลือ (soft, ไม่ลบ)
            migrationBuilder.Sql(@"
WITH dup AS (
  SELECT cc.Id,
         ROW_NUMBER() OVER (
           PARTITION BY cc.TaxId, cc.BranchCode
           ORDER BY (SELECT COUNT(*) FROM ImportBatches b WHERE b.ClientCompanyId = cc.Id) DESC,
                    CASE WHEN cc.Code LIKE 'COPY%' OR cc.Code LIKE 'X-%' OR cc.Code LIKE 'Z-%' THEN 1 ELSE 0 END ASC,
                    cc.Id ASC) AS rn
  FROM [ClientCompanies] cc
  WHERE cc.TaxId <> '' AND cc.IsActive = 1
)
UPDATE [ClientCompanies] SET [IsActive] = 0
WHERE [Id] IN (SELECT [Id] FROM dup WHERE rn > 1);");

            migrationBuilder.CreateIndex(
                name: "IX_ClientCompanies_TaxId_BranchCode",
                table: "ClientCompanies",
                columns: new[] { "TaxId", "BranchCode" },
                unique: true,
                filter: "[TaxId] <> '' AND [IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClientCompanies_TaxId_BranchCode",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "EnglishName",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "ClientCompanies");
        }
    }
}
