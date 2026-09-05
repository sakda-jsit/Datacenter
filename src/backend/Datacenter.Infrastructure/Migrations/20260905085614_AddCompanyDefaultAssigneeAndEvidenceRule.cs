using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyDefaultAssigneeAndEvidenceRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequireEvidence",
                table: "ComplianceTaskTemplates",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultAssigneeUserId",
                table: "ClientCompanies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientCompanies_DefaultAssigneeUserId",
                table: "ClientCompanies",
                column: "DefaultAssigneeUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientCompanies_Users_DefaultAssigneeUserId",
                table: "ClientCompanies",
                column: "DefaultAssigneeUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientCompanies_Users_DefaultAssigneeUserId",
                table: "ClientCompanies");

            migrationBuilder.DropIndex(
                name: "IX_ClientCompanies_DefaultAssigneeUserId",
                table: "ClientCompanies");

            migrationBuilder.DropColumn(
                name: "RequireEvidence",
                table: "ComplianceTaskTemplates");

            migrationBuilder.DropColumn(
                name: "DefaultAssigneeUserId",
                table: "ClientCompanies");
        }
    }
}
