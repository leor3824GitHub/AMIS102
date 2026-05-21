using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementAcquisition
{
    /// <inheritdoc />
    public partial class AddIarNumberSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                schema: "procurement",
                table: "AssetIARs");

            migrationBuilder.CreateTable(
                name: "IarNumberSequences",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    LastSerial = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IarNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IarNumberSequences_TenantId_Year",
                schema: "procurement",
                table: "IarNumberSequences",
                columns: new[] { "TenantId", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IarNumberSequences",
                schema: "procurement");

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "procurement",
                table: "AssetIARs",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
