using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientCompanySsoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "ClientCompanies",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "ClientCompanies",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SsoAccountNo",
                table: "ClientCompanies",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SsoBranchCode",
                table: "ClientCompanies",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Phone",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "SsoAccountNo",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "SsoBranchCode",
                table: "ClientCompanies");
        }
    }
}
