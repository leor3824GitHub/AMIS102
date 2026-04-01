using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.Expendable
{
    /// <inheritdoc />
    public partial class AddFulfilledValueToSupplyRequestItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_ParentProductId",
                schema: "expendable",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ParentProductId",
                schema: "expendable",
                table: "Products",
                column: "ParentProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_ParentProductId",
                schema: "expendable",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_ParentProductId",
                schema: "expendable",
                table: "Products",
                columns: new[] { "TenantId", "ParentProductId" });
        }
    }
}
