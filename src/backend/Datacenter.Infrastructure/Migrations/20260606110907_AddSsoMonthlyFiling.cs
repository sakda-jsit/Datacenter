using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSsoMonthlyFiling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SsoMonthlyFilings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollRunId = table.Column<int>(type: "int", nullable: false),
                    ClientCompanyId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SnapshotEmployeeCount = table.Column<int>(type: "int", nullable: false),
                    SnapshotTotalWage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SnapshotEmployeeContribution = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SnapshotEmployerContribution = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SnapshotGrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceiptDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceiptAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ReceiptNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FormFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    FormContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    FormContent = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ReceiptFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    ReceiptContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ReceiptContent = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SsoMonthlyFilings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SsoMonthlyFilings_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "PayrollRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SsoMonthlyFilings_ClientCompanyId_Year_Month",
                table: "SsoMonthlyFilings",
                columns: new[] { "ClientCompanyId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_SsoMonthlyFilings_PayrollRunId",
                table: "SsoMonthlyFilings",
                column: "PayrollRunId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SsoMonthlyFilings");
        }
    }
}
