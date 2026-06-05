using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWhtEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WhtEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientCompanyId = table.Column<int>(type: "int", nullable: false),
                    FormType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TaxPeriod = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WithholdDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DocumentNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReferenceNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PayeeName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PayeePrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PayeeTaxId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IncomeType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    BaseAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    IsLate = table.Column<bool>(type: "bit", nullable: false),
                    ImportBatchId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhtEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhtEntries_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WhtEntries_ClientCompanyId_TaxPeriod_FormType",
                table: "WhtEntries",
                columns: new[] { "ClientCompanyId", "TaxPeriod", "FormType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhtEntries");
        }
    }
}
