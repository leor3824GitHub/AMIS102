using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class PhysicalCountFreeze : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FrozenOnUtc",
                schema: "asset_register",
                table: "PhysicalCountSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficeOrderNo",
                schema: "asset_register",
                table: "PhysicalCountSessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsRecount",
                schema: "asset_register",
                table: "PhysicalCountEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RecountReason",
                schema: "asset_register",
                table: "PhysicalCountEntries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RecountRequestedOnUtc",
                schema: "asset_register",
                table: "PhysicalCountEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountSessions_TenantId_Status_FundCluster",
                schema: "asset_register",
                table: "PhysicalCountSessions",
                columns: new[] { "TenantId", "Status", "FundCluster" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PhysicalCountSessions_TenantId_Status_FundCluster",
                schema: "asset_register",
                table: "PhysicalCountSessions");

            migrationBuilder.DropColumn(
                name: "FrozenOnUtc",
                schema: "asset_register",
                table: "PhysicalCountSessions");

            migrationBuilder.DropColumn(
                name: "OfficeOrderNo",
                schema: "asset_register",
                table: "PhysicalCountSessions");

            migrationBuilder.DropColumn(
                name: "NeedsRecount",
                schema: "asset_register",
                table: "PhysicalCountEntries");

            migrationBuilder.DropColumn(
                name: "RecountReason",
                schema: "asset_register",
                table: "PhysicalCountEntries");

            migrationBuilder.DropColumn(
                name: "RecountRequestedOnUtc",
                schema: "asset_register",
                table: "PhysicalCountEntries");
        }
    }
}
