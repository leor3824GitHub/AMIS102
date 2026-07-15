using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementAcquisition
{
    /// <inheritdoc />
    public partial class Procurement_TenantPrefixStatusIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequests_DepartmentId",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequests_Status",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_Status",
                schema: "procurement",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_JobOrders_Status",
                schema: "procurement",
                table: "JobOrders");

            migrationBuilder.DropIndex(
                name: "IX_CanvassRequests_Status",
                schema: "procurement",
                table: "CanvassRequests");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_TenantId_DepartmentId",
                schema: "procurement",
                table: "PurchaseRequests",
                columns: new[] { "TenantId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_TenantId_Status",
                schema: "procurement",
                table: "PurchaseRequests",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId_Status",
                schema: "procurement",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_TenantId_Status",
                schema: "procurement",
                table: "JobOrders",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CanvassRequests_TenantId_Status",
                schema: "procurement",
                table: "CanvassRequests",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequests_TenantId_DepartmentId",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequests_TenantId_Status",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_TenantId_Status",
                schema: "procurement",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_JobOrders_TenantId_Status",
                schema: "procurement",
                table: "JobOrders");

            migrationBuilder.DropIndex(
                name: "IX_CanvassRequests_TenantId_Status",
                schema: "procurement",
                table: "CanvassRequests");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_DepartmentId",
                schema: "procurement",
                table: "PurchaseRequests",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_Status",
                schema: "procurement",
                table: "PurchaseRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Status",
                schema: "procurement",
                table: "PurchaseOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_Status",
                schema: "procurement",
                table: "JobOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CanvassRequests_Status",
                schema: "procurement",
                table: "CanvassRequests",
                column: "Status");
        }
    }
}
