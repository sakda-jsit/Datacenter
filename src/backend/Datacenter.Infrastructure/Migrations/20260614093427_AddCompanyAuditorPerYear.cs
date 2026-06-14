using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyAuditorPerYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuditorLicenseNo",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "AuditorName",
                table: "ClientCompanies");

            migrationBuilder.CreateTable(
                name: "CompanyAuditors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientCompanyId = table.Column<int>(type: "int", nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    AuditorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AuditorLicenseNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AuditorTaxId = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                    SignDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyAuditors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyAuditors_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAuditors_ClientCompanyId_FiscalYear",
                table: "CompanyAuditors",
                columns: new[] { "ClientCompanyId", "FiscalYear" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyAuditors");

            migrationBuilder.AddColumn<string>(
                name: "AuditorLicenseNo",
                table: "ClientCompanies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuditorName",
                table: "ClientCompanies",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
