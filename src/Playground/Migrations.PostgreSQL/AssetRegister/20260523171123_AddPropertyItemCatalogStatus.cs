using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class AddPropertyItemCatalogStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "asset_register",
                table: "PropertyItemCatalog",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Existing catalog rows pre-date the Status concept. Treat anything carrying a UACS as Ready;
            // anything without UACS (rare for pre-existing rows) stays Draft and will be promoted later
            // when an Accountant certifies a PR that references it.
            migrationBuilder.Sql(@"
                UPDATE asset_register.""PropertyItemCatalog""
                SET ""Status"" = 1
                WHERE ""UacsObjectCode"" IS NOT NULL AND ""UacsObjectCode"" <> '';");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyItemCatalog_Status",
                schema: "asset_register",
                table: "PropertyItemCatalog",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PropertyItemCatalog_Status",
                schema: "asset_register",
                table: "PropertyItemCatalog");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "asset_register",
                table: "PropertyItemCatalog");
        }
    }
}
