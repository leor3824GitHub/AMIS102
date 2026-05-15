using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class AssetRegister_RemoveItemId : Migration
    {
        // Drops the AssetRegistry → PropertyItemCatalog FK column. The catalog still
        // supplies defaults at registration time; once denormalized onto the asset, the
        // back-link is no longer needed (IAR is the source of truth, not the catalog).
        //
        // The model also picks up an `xmin` mapping on PropertyItemCatalog. xmin is a
        // PostgreSQL system column present on every table, so no DDL is required —
        // we leave the snapshot to record the mapping, same as AssetRegister_UseXminConcurrency.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetRegistries_TenantId_ItemId",
                schema: "asset_register",
                table: "AssetRegistries");

            migrationBuilder.DropColumn(
                name: "ItemId",
                schema: "asset_register",
                table: "AssetRegistries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ItemId",
                schema: "asset_register",
                table: "AssetRegistries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_AssetRegistries_TenantId_ItemId",
                schema: "asset_register",
                table: "AssetRegistries",
                columns: new[] { "TenantId", "ItemId" });
        }
    }
}
