using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientCompanyAuditorIsic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<string>(
                name: "IsicCode",
                table: "ClientCompanies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuditorLicenseNo",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "AuditorName",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "IsicCode",
                table: "ClientCompanies");
        }
    }
}
