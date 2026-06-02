using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Expendable
{
    /// <inheritdoc />
    public partial class Expendable_RenameSkuToStockNoAddArticle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SKU",
                schema: "expendable",
                table: "Products",
                newName: "StockNo");

            migrationBuilder.RenameIndex(
                name: "IX_Products_TenantId_SKU",
                schema: "expendable",
                table: "Products",
                newName: "IX_Products_TenantId_StockNo");

            migrationBuilder.AddColumn<string>(
                name: "Article",
                schema: "expendable",
                table: "Products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Article",
                schema: "expendable",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "StockNo",
                schema: "expendable",
                table: "Products",
                newName: "SKU");

            migrationBuilder.RenameIndex(
                name: "IX_Products_TenantId_StockNo",
                schema: "expendable",
                table: "Products",
                newName: "IX_Products_TenantId_SKU");
        }
    }
}
