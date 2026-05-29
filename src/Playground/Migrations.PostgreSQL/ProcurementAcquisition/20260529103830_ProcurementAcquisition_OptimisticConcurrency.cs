using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementAcquisition
{
    /// <inheritdoc />
    public partial class ProcurementAcquisition_OptimisticConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "procurement",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "procurement",
                table: "CanvassRequests");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "procurement",
                table: "PurchaseOrders",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "procurement",
                table: "CanvassRequests",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "procurement",
                table: "AssetIARs",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "procurement",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "procurement",
                table: "CanvassRequests");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "procurement",
                table: "AssetIARs");

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "procurement",
                table: "PurchaseOrders",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "procurement",
                table: "CanvassRequests",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
