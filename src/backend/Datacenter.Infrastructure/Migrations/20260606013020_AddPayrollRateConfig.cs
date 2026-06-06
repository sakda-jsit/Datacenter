using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollRateConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayrollRateConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientCompanyId = table.Column<int>(type: "int", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SsoEmployeePct = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    SsoEmployerPct = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    SsoWageFloor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SsoWageCap = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WcfRatePct = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    WcfWageCapPerYear = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRateConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRateConfigs_ClientCompanyId_EffectiveFrom",
                table: "PayrollRateConfigs",
                columns: new[] { "ClientCompanyId", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollRateConfigs");
        }
    }
}
