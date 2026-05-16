using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementAcquisition
{
    /// <inheritdoc />
    public partial class AddPrNumberSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Section",
                schema: "procurement",
                table: "PurchaseRequests",
                newName: "ResponsibilityCenterCode");

            migrationBuilder.CreateTable(
                name: "PrNumberSequences",
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
                    table.PrimaryKey("PK_PrNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrNumberSequences_TenantId_Year",
                schema: "procurement",
                table: "PrNumberSequences",
                columns: new[] { "TenantId", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrNumberSequences",
                schema: "procurement");

            migrationBuilder.RenameColumn(
                name: "ResponsibilityCenterCode",
                schema: "procurement",
                table: "PurchaseRequests",
                newName: "Section");
        }
    }
}
