using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class AssetRegister_AcquisitionDateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AssetRegistries_TenantId_AcquisitionDate",
                schema: "asset_register",
                table: "AssetRegistries",
                columns: new[] { "TenantId", "AcquisitionDate" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetRegistries_TenantId_AcquisitionDate",
                schema: "asset_register",
                table: "AssetRegistries");
        }
    }
}
