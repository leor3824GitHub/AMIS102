using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class AddSnapshotClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Snapshot_CategoryCode",
                schema: "asset_register",
                table: "UnserviceablePropertyItems",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Snapshot_PropertyClass",
                schema: "asset_register",
                table: "UnserviceablePropertyItems",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Snapshot_CategoryCode",
                schema: "asset_register",
                table: "ReturnedPropertyReceiptItems",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Snapshot_PropertyClass",
                schema: "asset_register",
                table: "ReturnedPropertyReceiptItems",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Snapshot_CategoryCode",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Snapshot_PropertyClass",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Snapshot_CategoryCode",
                schema: "asset_register",
                table: "PropertyIncidentItems",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Snapshot_PropertyClass",
                schema: "asset_register",
                table: "PropertyIncidentItems",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Snapshot_CategoryCode",
                schema: "asset_register",
                table: "PropertyAccountabilityLines",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Snapshot_PropertyClass",
                schema: "asset_register",
                table: "PropertyAccountabilityLines",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Snapshot_CategoryCode",
                schema: "asset_register",
                table: "PhysicalCountEntries",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Snapshot_PropertyClass",
                schema: "asset_register",
                table: "PhysicalCountEntries",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Snapshot_CategoryCode",
                schema: "asset_register",
                table: "UnserviceablePropertyItems");

            migrationBuilder.DropColumn(
                name: "Snapshot_PropertyClass",
                schema: "asset_register",
                table: "UnserviceablePropertyItems");

            migrationBuilder.DropColumn(
                name: "Snapshot_CategoryCode",
                schema: "asset_register",
                table: "ReturnedPropertyReceiptItems");

            migrationBuilder.DropColumn(
                name: "Snapshot_PropertyClass",
                schema: "asset_register",
                table: "ReturnedPropertyReceiptItems");

            migrationBuilder.DropColumn(
                name: "Snapshot_CategoryCode",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines");

            migrationBuilder.DropColumn(
                name: "Snapshot_PropertyClass",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines");

            migrationBuilder.DropColumn(
                name: "Snapshot_CategoryCode",
                schema: "asset_register",
                table: "PropertyIncidentItems");

            migrationBuilder.DropColumn(
                name: "Snapshot_PropertyClass",
                schema: "asset_register",
                table: "PropertyIncidentItems");

            migrationBuilder.DropColumn(
                name: "Snapshot_CategoryCode",
                schema: "asset_register",
                table: "PropertyAccountabilityLines");

            migrationBuilder.DropColumn(
                name: "Snapshot_PropertyClass",
                schema: "asset_register",
                table: "PropertyAccountabilityLines");

            migrationBuilder.DropColumn(
                name: "Snapshot_CategoryCode",
                schema: "asset_register",
                table: "PhysicalCountEntries");

            migrationBuilder.DropColumn(
                name: "Snapshot_PropertyClass",
                schema: "asset_register",
                table: "PhysicalCountEntries");
        }
    }
}
