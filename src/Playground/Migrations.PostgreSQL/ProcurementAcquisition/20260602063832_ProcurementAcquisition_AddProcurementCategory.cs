using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementAcquisition
{
    /// <inheritdoc />
    public partial class ProcurementAcquisition_AddProcurementCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                schema: "procurement",
                table: "PurchaseOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                schema: "procurement",
                table: "AssetIARs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "procurement",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "procurement",
                table: "AssetIARs");
        }
    }
}
