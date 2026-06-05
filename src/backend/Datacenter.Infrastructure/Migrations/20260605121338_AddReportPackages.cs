using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientCompanyId = table.Column<int>(type: "int", nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SnapshotCompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SnapshotTaxId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SnapshotBranchCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SnapshotAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TotalAssets = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalLiabilities = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalEquity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalRevenue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NetProfit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalizedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportPackages_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportPackages_ClientCompanyId_FiscalYear_Version",
                table: "ReportPackages",
                columns: new[] { "ClientCompanyId", "FiscalYear", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportPackages");
        }
    }
}
