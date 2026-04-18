using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.MasterData
{
    /// <inheritdoc />
    public partial class DropTenantIdFromCapitalizationThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CapitalizationThresholds_TenantId",
                schema: "masterdata",
                table: "CapitalizationThresholds");

            migrationBuilder.DropIndex(
                name: "IX_CapitalizationThresholds_TenantId_IsActive",
                schema: "masterdata",
                table: "CapitalizationThresholds");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "masterdata",
                table: "CapitalizationThresholds");

            migrationBuilder.CreateIndex(
                name: "IX_CapitalizationThresholds_IsActive",
                schema: "masterdata",
                table: "CapitalizationThresholds",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CapitalizationThresholds_IsActive",
                schema: "masterdata",
                table: "CapitalizationThresholds");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "masterdata",
                table: "CapitalizationThresholds",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CapitalizationThresholds_TenantId",
                schema: "masterdata",
                table: "CapitalizationThresholds",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CapitalizationThresholds_TenantId_IsActive",
                schema: "masterdata",
                table: "CapitalizationThresholds",
                columns: new[] { "TenantId", "IsActive" });
        }
    }
}
