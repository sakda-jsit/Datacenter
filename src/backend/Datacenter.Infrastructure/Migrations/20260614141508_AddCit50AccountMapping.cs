using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCit50AccountMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountCit50Mappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientCompanyId = table.Column<int>(type: "int", nullable: false),
                    AccountCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Cit50LineCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountCit50Mappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountCit50Mappings_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cit50ScheduleLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ScheduleNo = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    PdfPage = table.Column<int>(type: "int", nullable: false),
                    PdfX = table.Column<double>(type: "float", nullable: false),
                    PdfY = table.Column<double>(type: "float", nullable: false),
                    PdfW = table.Column<double>(type: "float", nullable: false),
                    IsTotal = table.Column<bool>(type: "bit", nullable: false),
                    IsCatchAll = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cit50ScheduleLines", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountCit50Mappings_ClientCompanyId_AccountCode",
                table: "AccountCit50Mappings",
                columns: new[] { "ClientCompanyId", "AccountCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cit50ScheduleLines_Code",
                table: "Cit50ScheduleLines",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountCit50Mappings");

            migrationBuilder.DropTable(
                name: "Cit50ScheduleLines");
        }
    }
}
