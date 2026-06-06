using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStatutoryFiling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StatutoryFilings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientCompanyId = table.Column<int>(type: "int", nullable: false),
                    FilingType = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SnapshotBase = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SnapshotAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SnapshotCount = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_StatutoryFilings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatutoryFilings_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StatutoryFilings_ClientCompanyId_FilingType_Year_Month",
                table: "StatutoryFilings",
                columns: new[] { "ClientCompanyId", "FilingType", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StatutoryFilings");
        }
    }
}
