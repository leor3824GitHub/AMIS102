using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class AssetRegister_SnapshotNetBookValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_NetBookValue",
                schema: "asset_register",
                table: "UnserviceablePropertyItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_NetBookValue",
                schema: "asset_register",
                table: "ReturnedPropertyReceiptItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_NetBookValue",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_NetBookValue",
                schema: "asset_register",
                table: "PropertyIncidentItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_NetBookValue",
                schema: "asset_register",
                table: "PropertyAccountabilityLines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_NetBookValue",
                schema: "asset_register",
                table: "PhysicalCountEntries",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Snapshot_NetBookValue",
                schema: "asset_register",
                table: "UnserviceablePropertyItems");

            migrationBuilder.DropColumn(
                name: "Snapshot_NetBookValue",
                schema: "asset_register",
                table: "ReturnedPropertyReceiptItems");

            migrationBuilder.DropColumn(
                name: "Snapshot_NetBookValue",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines");

            migrationBuilder.DropColumn(
                name: "Snapshot_NetBookValue",
                schema: "asset_register",
                table: "PropertyIncidentItems");

            migrationBuilder.DropColumn(
                name: "Snapshot_NetBookValue",
                schema: "asset_register",
                table: "PropertyAccountabilityLines");

            migrationBuilder.DropColumn(
                name: "Snapshot_NetBookValue",
                schema: "asset_register",
                table: "PhysicalCountEntries");
        }
    }
}
