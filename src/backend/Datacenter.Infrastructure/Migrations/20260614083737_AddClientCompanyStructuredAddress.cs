using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientCompanyStructuredAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddrBuilding",
                table: "ClientCompanies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrDistrict",
                table: "ClientCompanies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrFloor",
                table: "ClientCompanies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrHouseNo",
                table: "ClientCompanies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrMoo",
                table: "ClientCompanies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrProvince",
                table: "ClientCompanies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrRoad",
                table: "ClientCompanies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrRoomNo",
                table: "ClientCompanies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrSoi",
                table: "ClientCompanies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrSubDistrict",
                table: "ClientCompanies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrVillage",
                table: "ClientCompanies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddrBuilding",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "AddrDistrict",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "AddrFloor",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "AddrHouseNo",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "AddrMoo",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "AddrProvince",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "AddrRoad",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "AddrRoomNo",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "AddrSoi",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "AddrSubDistrict",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "AddrVillage",
                table: "ClientCompanies");
        }
    }
}
