using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArCustomersAndInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientCompanyId = table.Column<int>(type: "int", nullable: false),
                    DocumentNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CustomerCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceivedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_ArInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArInvoices_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientCompanyId = table.Column<int>(type: "int", nullable: false),
                    CustomerCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArInvoices_ClientCompanyId_CustomerCode",
                table: "ArInvoices",
                columns: new[] { "ClientCompanyId", "CustomerCode" });

            migrationBuilder.CreateIndex(
                name: "IX_ArInvoices_ClientCompanyId_DocumentDate",
                table: "ArInvoices",
                columns: new[] { "ClientCompanyId", "DocumentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_ClientCompanyId_CustomerCode",
                table: "Customers",
                columns: new[] { "ClientCompanyId", "CustomerCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArInvoices");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
