using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.AssetManagement
{
    /// <inheritdoc />
    public partial class AddTangibleItemPurchaseOrderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseOrderId",
                schema: "am",
                table: "TangibleItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TangibleItems_PurchaseOrderId",
                schema: "am",
                table: "TangibleItems",
                column: "PurchaseOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TangibleItems_PurchaseOrderId",
                schema: "am",
                table: "TangibleItems");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                schema: "am",
                table: "TangibleItems");
        }
    }
}
