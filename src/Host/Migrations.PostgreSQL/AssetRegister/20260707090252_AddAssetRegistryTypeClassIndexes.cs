using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class AddAssetRegistryTypeClassIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AssetRegistries_TenantId_AssetType",
                schema: "asset_register",
                table: "AssetRegistries",
                columns: new[] { "TenantId", "AssetType" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetRegistries_TenantId_PropertyClass",
                schema: "asset_register",
                table: "AssetRegistries",
                columns: new[] { "TenantId", "PropertyClass" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetRegistries_TenantId_AssetType",
                schema: "asset_register",
                table: "AssetRegistries");

            migrationBuilder.DropIndex(
                name: "IX_AssetRegistries_TenantId_PropertyClass",
                schema: "asset_register",
                table: "AssetRegistries");
        }
    }
}
