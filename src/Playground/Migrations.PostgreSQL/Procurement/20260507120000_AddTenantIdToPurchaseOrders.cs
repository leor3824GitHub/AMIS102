using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.Procurement
{
    /// <inheritdoc />
    public partial class AddTenantIdToPurchaseOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_PoNumber",
                schema: "procurement",
                table: "PurchaseOrders");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "procurement",
                table: "PurchaseOrders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE procurement.\"PurchaseOrders\" SET \"TenantId\" = 'root' WHERE \"TenantId\" IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                schema: "procurement",
                table: "PurchaseOrders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId_PoNumber",
                schema: "procurement",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "PoNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_TenantId_PoNumber",
                schema: "procurement",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "procurement",
                table: "PurchaseOrders");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PoNumber",
                schema: "procurement",
                table: "PurchaseOrders",
                column: "PoNumber",
                unique: true);
        }
    }
}
