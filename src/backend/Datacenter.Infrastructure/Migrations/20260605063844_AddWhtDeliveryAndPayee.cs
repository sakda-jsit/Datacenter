using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWhtDeliveryAndPayee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailError",
                table: "WhtEntries",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailRecipient",
                table: "WhtEntries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailSentAt",
                table: "WhtEntries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailSentBy",
                table: "WhtEntries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailStatus",
                table: "WhtEntries",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PayeeAddress",
                table: "WhtEntries",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceKey",
                table: "WhtEntries",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "WhtPayees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientCompanyId = table.Column<int>(type: "int", nullable: false),
                    TaxId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhtPayees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhtPayees_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // ล้าง WhtEntries เดิม (import ก่อนมี SourceKey → SourceKey ว่างทุกแถว ชน unique index)
            // ข้อมูลนี้ดึงจาก Express ISTAX 100% → re-import ครั้งถัดไปจะเติม SourceKey ให้ครบ
            migrationBuilder.Sql("DELETE FROM [WhtEntries];");

            migrationBuilder.CreateIndex(
                name: "IX_WhtEntries_ClientCompanyId_SourceKey",
                table: "WhtEntries",
                columns: new[] { "ClientCompanyId", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WhtPayees_ClientCompanyId_TaxId",
                table: "WhtPayees",
                columns: new[] { "ClientCompanyId", "TaxId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhtPayees");

            migrationBuilder.DropIndex(
                name: "IX_WhtEntries_ClientCompanyId_SourceKey",
                table: "WhtEntries");

            migrationBuilder.DropColumn(
                name: "EmailError",
                table: "WhtEntries");

            migrationBuilder.DropColumn(
                name: "EmailRecipient",
                table: "WhtEntries");

            migrationBuilder.DropColumn(
                name: "EmailSentAt",
                table: "WhtEntries");

            migrationBuilder.DropColumn(
                name: "EmailSentBy",
                table: "WhtEntries");

            migrationBuilder.DropColumn(
                name: "EmailStatus",
                table: "WhtEntries");

            migrationBuilder.DropColumn(
                name: "PayeeAddress",
                table: "WhtEntries");

            migrationBuilder.DropColumn(
                name: "SourceKey",
                table: "WhtEntries");
        }
    }
}
