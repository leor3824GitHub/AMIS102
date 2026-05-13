using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class AssetRegister_UseXminConcurrency : Migration
    {
        // Switch concurrency tracking from a managed byte[] column (which fails on
        // PostgreSQL because the DB doesn't auto-populate it on INSERT) to the
        // built-in `xmin` system column already present on every PostgreSQL table.
        // No DDL is needed to "add" xmin — it's a system column — so we only drop
        // the old Version/RowVersion columns. The model-side mapping is recorded
        // in the migration snapshot.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                schema: "asset_register",
                table: "UnserviceablePropertyReports");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "asset_register",
                table: "ReceivingReports");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "asset_register",
                table: "PropertyIncidentReports");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "asset_register",
                table: "PropertyCodeCounters");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "asset_register",
                table: "PropertyAccountabilities");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "asset_register",
                table: "PhysicalCountSessions");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "asset_register",
                table: "AssetRegistries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "asset_register",
                table: "UnserviceablePropertyReports",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "asset_register",
                table: "ReceivingReports",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "asset_register",
                table: "PropertyIncidentReports",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "asset_register",
                table: "PropertyCodeCounters",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "asset_register",
                table: "PropertyAccountabilities",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "asset_register",
                table: "PhysicalCountSessions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "asset_register",
                table: "AssetRegistries",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
