using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApSuppliersAndInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientCompanyId = table.Column<int>(type: "int", nullable: false),
                    DocumentNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SupplierCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    VatPeriod = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ImportBatchId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApInvoices_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientCompanyId = table.Column<int>(type: "int", nullable: false),
                    SupplierCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TaxId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Contact = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: false),
                    PaymentCondition = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    GlAccountCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suppliers_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApInvoices_ClientCompanyId_DocumentDate",
                table: "ApInvoices",
                columns: new[] { "ClientCompanyId", "DocumentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ApInvoices_ClientCompanyId_SupplierCode",
                table: "ApInvoices",
                columns: new[] { "ClientCompanyId", "SupplierCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_ClientCompanyId_SupplierCode",
                table: "Suppliers",
                columns: new[] { "ClientCompanyId", "SupplierCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApInvoices");

            migrationBuilder.DropTable(
                name: "Suppliers");
        }
    }
}
