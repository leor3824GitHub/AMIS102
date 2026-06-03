using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementAcquisition
{
    /// <inheritdoc />
    public partial class ProcurementAcquisition_RenameAssetIARsToInspectionAcceptanceReports : Migration
    {
        // Hand-edited: the entity CLR type was renamed (AssetInspectionAcceptanceReport ->
        // InspectionAcceptanceReport), so EF scaffolded a drop+create that would destroy data.
        // This migration instead renames the existing table, its primary key, and its indexes
        // in place, preserving all rows.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "AssetIARs",
                schema: "procurement",
                newName: "InspectionAcceptanceReports",
                newSchema: "procurement");

            migrationBuilder.Sql(
                "ALTER TABLE procurement.\"InspectionAcceptanceReports\" RENAME CONSTRAINT \"PK_AssetIARs\" TO \"PK_InspectionAcceptanceReports\";");

            migrationBuilder.RenameIndex(
                name: "IX_AssetIARs_CreatedOnUtc",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                newName: "IX_InspectionAcceptanceReports_CreatedOnUtc");

            migrationBuilder.RenameIndex(
                name: "IX_AssetIARs_TenantId_IarNumber",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                newName: "IX_InspectionAcceptanceReports_TenantId_IarNumber");

            migrationBuilder.RenameIndex(
                name: "IX_AssetIARs_TenantId_PurchaseOrderId",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                newName: "IX_InspectionAcceptanceReports_TenantId_PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_AssetIARs_TenantId_Status",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                newName: "IX_InspectionAcceptanceReports_TenantId_Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_InspectionAcceptanceReports_CreatedOnUtc",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                newName: "IX_AssetIARs_CreatedOnUtc");

            migrationBuilder.RenameIndex(
                name: "IX_InspectionAcceptanceReports_TenantId_IarNumber",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                newName: "IX_AssetIARs_TenantId_IarNumber");

            migrationBuilder.RenameIndex(
                name: "IX_InspectionAcceptanceReports_TenantId_PurchaseOrderId",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                newName: "IX_AssetIARs_TenantId_PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_InspectionAcceptanceReports_TenantId_Status",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                newName: "IX_AssetIARs_TenantId_Status");

            migrationBuilder.Sql(
                "ALTER TABLE procurement.\"InspectionAcceptanceReports\" RENAME CONSTRAINT \"PK_InspectionAcceptanceReports\" TO \"PK_AssetIARs\";");

            migrationBuilder.RenameTable(
                name: "InspectionAcceptanceReports",
                schema: "procurement",
                newName: "AssetIARs",
                newSchema: "procurement");
        }
    }
}
