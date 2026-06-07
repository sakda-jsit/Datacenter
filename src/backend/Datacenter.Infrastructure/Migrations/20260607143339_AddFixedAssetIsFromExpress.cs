using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFixedAssetIsFromExpress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFromExpress",
                table: "FixedAssets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // backfill: สินทรัพย์ที่นำเข้าจาก Express เดิม (importer ตั้ง BroughtForwardYear > 0 เสมอ)
            // → ล็อกเป็น read-only ทันที (สินทรัพย์ป้อนเองมัก BroughtForwardYear = 0)
            migrationBuilder.Sql("UPDATE [FixedAssets] SET [IsFromExpress] = 1 WHERE [BroughtForwardYear] > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFromExpress",
                table: "FixedAssets");
        }
    }
}
