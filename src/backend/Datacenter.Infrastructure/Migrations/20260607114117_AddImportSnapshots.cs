using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datacenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImportSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportBatchId = table.Column<int>(type: "int", nullable: false),
                    ClientCompanyId = table.Column<int>(type: "int", nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceFolderPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ArchiveRelativePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ArchiveFileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ArchiveByteSize = table.Column<long>(type: "bigint", nullable: false),
                    ArchiveSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileCount = table.Column<int>(type: "int", nullable: false),
                    TotalSourceBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RetainUntil = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportSnapshots_ClientCompanies_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportSnapshots_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportSnapshotFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportSnapshotId = table.Column<int>(type: "int", nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ByteSize = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: true),
                    SourceModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportSnapshotFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportSnapshotFiles_ImportSnapshots_ImportSnapshotId",
                        column: x => x.ImportSnapshotId,
                        principalTable: "ImportSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportSnapshotFiles_ImportSnapshotId",
                table: "ImportSnapshotFiles",
                column: "ImportSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportSnapshots_ClientCompanyId_FiscalYear",
                table: "ImportSnapshots",
                columns: new[] { "ClientCompanyId", "FiscalYear" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportSnapshots_ImportBatchId",
                table: "ImportSnapshots",
                column: "ImportBatchId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportSnapshotFiles");

            migrationBuilder.DropTable(
                name: "ImportSnapshots");
        }
    }
}
