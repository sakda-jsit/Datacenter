using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaseContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaseContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientCompanyId = table.Column<int>(type: "int", nullable: false),
                    ContractType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContractNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssetName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssetCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Lessor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContractDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FirstInstallmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NumberOfPeriods = table.Column<int>(type: "int", nullable: false),
                    PaymentsPerYear = table.Column<int>(type: "int", nullable: false),
                    CashPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DownPayment = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FinancedPrincipal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InstallmentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VatPerPeriod = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LiabilityAccountId = table.Column<int>(type: "int", nullable: false),
                    DeferredInterestAccountId = table.Column<int>(type: "int", nullable: true),
                    InputVatUndueAccountId = table.Column<int>(type: "int", nullable: true),
                    InterestExpenseAccountId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttachmentPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaseContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaseContracts_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaseContracts_ClientCompanyId_ContractNo",
                table: "LeaseContracts",
                columns: new[] { "ClientCompanyId", "ContractNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaseContracts_ClientCompanyId_IsActive",
                table: "LeaseContracts",
                columns: new[] { "ClientCompanyId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaseContracts");
        }
    }
}
