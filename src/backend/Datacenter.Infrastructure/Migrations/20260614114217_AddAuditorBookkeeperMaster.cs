using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditorBookkeeperMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AuditorName",
                table: "CompanyAuditors",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<int>(
                name: "AuditorId",
                table: "CompanyAuditors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BookkeeperId",
                table: "CompanyAuditors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultAuditorId",
                table: "ClientCompanies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultBookkeeperId",
                table: "ClientCompanies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Auditors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    LicenseNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TaxId = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                    AuditFirmName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AuditFirmTaxId = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bookkeepers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TaxId = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookkeepers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAuditors_AuditorId",
                table: "CompanyAuditors",
                column: "AuditorId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAuditors_BookkeeperId",
                table: "CompanyAuditors",
                column: "BookkeeperId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientCompanies_DefaultAuditorId",
                table: "ClientCompanies",
                column: "DefaultAuditorId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientCompanies_DefaultBookkeeperId",
                table: "ClientCompanies",
                column: "DefaultBookkeeperId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientCompanies_Auditors_DefaultAuditorId",
                table: "ClientCompanies",
                column: "DefaultAuditorId",
                principalTable: "Auditors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ClientCompanies_Bookkeepers_DefaultBookkeeperId",
                table: "ClientCompanies",
                column: "DefaultBookkeeperId",
                principalTable: "Bookkeepers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyAuditors_Auditors_AuditorId",
                table: "CompanyAuditors",
                column: "AuditorId",
                principalTable: "Auditors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyAuditors_Bookkeepers_BookkeeperId",
                table: "CompanyAuditors",
                column: "BookkeeperId",
                principalTable: "Bookkeepers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientCompanies_Auditors_DefaultAuditorId",
                table: "ClientCompanies");

            migrationBuilder.DropForeignKey(
                name: "FK_ClientCompanies_Bookkeepers_DefaultBookkeeperId",
                table: "ClientCompanies");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanyAuditors_Auditors_AuditorId",
                table: "CompanyAuditors");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanyAuditors_Bookkeepers_BookkeeperId",
                table: "CompanyAuditors");

            migrationBuilder.DropTable(
                name: "Auditors");

            migrationBuilder.DropTable(
                name: "Bookkeepers");

            migrationBuilder.DropIndex(
                name: "IX_CompanyAuditors_AuditorId",
                table: "CompanyAuditors");

            migrationBuilder.DropIndex(
                name: "IX_CompanyAuditors_BookkeeperId",
                table: "CompanyAuditors");

            migrationBuilder.DropIndex(
                name: "IX_ClientCompanies_DefaultAuditorId",
                table: "ClientCompanies");

            migrationBuilder.DropIndex(
                name: "IX_ClientCompanies_DefaultBookkeeperId",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "AuditorId",
                table: "CompanyAuditors");

            migrationBuilder.DropColumn(
                name: "BookkeeperId",
                table: "CompanyAuditors");

            migrationBuilder.DropColumn(
                name: "DefaultAuditorId",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "DefaultBookkeeperId",
                table: "ClientCompanies");

            migrationBuilder.AlterColumn<string>(
                name: "AuditorName",
                table: "CompanyAuditors",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
