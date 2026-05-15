using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetProcurement
{
    /// <inheritdoc />
    public partial class IAR_StageWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcceptedOnUtc",
                schema: "asset_procurement",
                table: "AssetIARs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledOnUtc",
                schema: "asset_procurement",
                table: "AssetIARs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InspectedOnUtc",
                schema: "asset_procurement",
                table: "AssetIARs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedForInspectionOnUtc",
                schema: "asset_procurement",
                table: "AssetIARs",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedOnUtc",
                schema: "asset_procurement",
                table: "AssetIARs");

            migrationBuilder.DropColumn(
                name: "CancelledOnUtc",
                schema: "asset_procurement",
                table: "AssetIARs");

            migrationBuilder.DropColumn(
                name: "InspectedOnUtc",
                schema: "asset_procurement",
                table: "AssetIARs");

            migrationBuilder.DropColumn(
                name: "SubmittedForInspectionOnUtc",
                schema: "asset_procurement",
                table: "AssetIARs");
        }
    }
}
