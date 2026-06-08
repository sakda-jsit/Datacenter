using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeStructuredAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddrBuilding",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrDistrict",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrFloor",
                table: "Employees",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrHouseNo",
                table: "Employees",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrMoo",
                table: "Employees",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrPostalCode",
                table: "Employees",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrProvince",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrRoad",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrRoomNo",
                table: "Employees",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrSoi",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrSubDistrict",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrVillage",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddrYaek",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddrBuilding",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddrDistrict",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddrFloor",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddrHouseNo",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddrMoo",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddrPostalCode",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddrProvince",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddrRoad",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddrRoomNo",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddrSoi",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddrSubDistrict",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddrVillage",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddrYaek",
                table: "Employees");
        }
    }
}
