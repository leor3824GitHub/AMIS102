using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Migrations.Expendable
{
    /// <inheritdoc />
    public partial class Expendable_DropInventorySnapshotColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductCode",
                schema: "expendable",
                table: "ProductInventory");

            migrationBuilder.DropColumn(
                name: "ProductName",
                schema: "expendable",
                table: "ProductInventory");

            migrationBuilder.DropColumn(
                name: "WarehouseLocationName",
                schema: "expendable",
                table: "ProductInventory");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                schema: "expendable",
                table: "ProductInventory",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                schema: "expendable",
                table: "ProductInventory",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WarehouseLocationName",
                schema: "expendable",
                table: "ProductInventory",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
