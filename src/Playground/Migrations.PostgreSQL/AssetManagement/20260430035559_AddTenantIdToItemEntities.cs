using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.AssetManagement
{
    /// <inheritdoc />
    public partial class AddTenantIdToItemEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "UnserviceablePropertyItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "TangibleInventoryItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "SMIRItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "RRSPItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "RRPItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "PropertyIncidentItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "PPEIRItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "PhysicalCountEntries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "PARItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "ICSItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyItems_TenantId",
                schema: "am",
                table: "UnserviceablePropertyItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyItems_TenantId_ReportId",
                schema: "am",
                table: "UnserviceablePropertyItems",
                columns: new[] { "TenantId", "ReportId" });

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "UnserviceablePropertyItems",
                columns: new[] { "TenantId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_TangibleInventoryItems_TenantId",
                schema: "am",
                table: "TangibleInventoryItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TangibleInventoryItems_TenantId_AssetType",
                schema: "am",
                table: "TangibleInventoryItems",
                columns: new[] { "TenantId", "AssetType" });

            migrationBuilder.CreateIndex(
                name: "IX_TangibleInventoryItems_TenantId_IsIssued",
                schema: "am",
                table: "TangibleInventoryItems",
                columns: new[] { "TenantId", "IsIssued" });

            migrationBuilder.CreateIndex(
                name: "IX_TangibleInventoryItems_TenantId_ItemId",
                schema: "am",
                table: "TangibleInventoryItems",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_TangibleInventoryItems_TenantId_TangibleInventoryId",
                schema: "am",
                table: "TangibleInventoryItems",
                columns: new[] { "TenantId", "TangibleInventoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_SMIRItems_TenantId",
                schema: "am",
                table: "SMIRItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SMIRItems_TenantId_SMIRId",
                schema: "am",
                table: "SMIRItems",
                columns: new[] { "TenantId", "SMIRId" });

            migrationBuilder.CreateIndex(
                name: "IX_SMIRItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "SMIRItems",
                columns: new[] { "TenantId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_RRSPItems_TenantId",
                schema: "am",
                table: "RRSPItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RRSPItems_TenantId_RRSPId",
                schema: "am",
                table: "RRSPItems",
                columns: new[] { "TenantId", "RRSPId" });

            migrationBuilder.CreateIndex(
                name: "IX_RRSPItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "RRSPItems",
                columns: new[] { "TenantId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_RRPItems_TenantId",
                schema: "am",
                table: "RRPItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RRPItems_TenantId_RRPId",
                schema: "am",
                table: "RRPItems",
                columns: new[] { "TenantId", "RRPId" });

            migrationBuilder.CreateIndex(
                name: "IX_RRPItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "RRPItems",
                columns: new[] { "TenantId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIncidentItems_TenantId",
                schema: "am",
                table: "PropertyIncidentItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIncidentItems_TenantId_ReportId",
                schema: "am",
                table: "PropertyIncidentItems",
                columns: new[] { "TenantId", "ReportId" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIncidentItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "PropertyIncidentItems",
                columns: new[] { "TenantId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyCodeCounters_TenantId",
                schema: "am",
                table: "PropertyCodeCounters",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PPEIRItems_TenantId",
                schema: "am",
                table: "PPEIRItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PPEIRItems_TenantId_PPEIRId",
                schema: "am",
                table: "PPEIRItems",
                columns: new[] { "TenantId", "PPEIRId" });

            migrationBuilder.CreateIndex(
                name: "IX_PPEIRItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "PPEIRItems",
                columns: new[] { "TenantId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_TenantId",
                schema: "am",
                table: "PhysicalCountEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_TenantId_Result",
                schema: "am",
                table: "PhysicalCountEntries",
                columns: new[] { "TenantId", "Result" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_TenantId_SessionId",
                schema: "am",
                table: "PhysicalCountEntries",
                columns: new[] { "TenantId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_TenantId_SessionId_TangibleInventoryIt~",
                schema: "am",
                table: "PhysicalCountEntries",
                columns: new[] { "TenantId", "SessionId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "PhysicalCountEntries",
                columns: new[] { "TenantId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_PARItems_TenantId",
                schema: "am",
                table: "PARItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PARItems_TenantId_PARId",
                schema: "am",
                table: "PARItems",
                columns: new[] { "TenantId", "PARId" });

            migrationBuilder.CreateIndex(
                name: "IX_PARItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "PARItems",
                columns: new[] { "TenantId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_ICSItems_TenantId",
                schema: "am",
                table: "ICSItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ICSItems_TenantId_ICSId",
                schema: "am",
                table: "ICSItems",
                columns: new[] { "TenantId", "ICSId" });

            migrationBuilder.CreateIndex(
                name: "IX_ICSItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "ICSItems",
                columns: new[] { "TenantId", "TangibleInventoryItemId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnserviceablePropertyItems_TenantId",
                schema: "am",
                table: "UnserviceablePropertyItems");

            migrationBuilder.DropIndex(
                name: "IX_UnserviceablePropertyItems_TenantId_ReportId",
                schema: "am",
                table: "UnserviceablePropertyItems");

            migrationBuilder.DropIndex(
                name: "IX_UnserviceablePropertyItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "UnserviceablePropertyItems");

            migrationBuilder.DropIndex(
                name: "IX_TangibleInventoryItems_TenantId",
                schema: "am",
                table: "TangibleInventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_TangibleInventoryItems_TenantId_AssetType",
                schema: "am",
                table: "TangibleInventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_TangibleInventoryItems_TenantId_IsIssued",
                schema: "am",
                table: "TangibleInventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_TangibleInventoryItems_TenantId_ItemId",
                schema: "am",
                table: "TangibleInventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_TangibleInventoryItems_TenantId_TangibleInventoryId",
                schema: "am",
                table: "TangibleInventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_SMIRItems_TenantId",
                schema: "am",
                table: "SMIRItems");

            migrationBuilder.DropIndex(
                name: "IX_SMIRItems_TenantId_SMIRId",
                schema: "am",
                table: "SMIRItems");

            migrationBuilder.DropIndex(
                name: "IX_SMIRItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "SMIRItems");

            migrationBuilder.DropIndex(
                name: "IX_RRSPItems_TenantId",
                schema: "am",
                table: "RRSPItems");

            migrationBuilder.DropIndex(
                name: "IX_RRSPItems_TenantId_RRSPId",
                schema: "am",
                table: "RRSPItems");

            migrationBuilder.DropIndex(
                name: "IX_RRSPItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "RRSPItems");

            migrationBuilder.DropIndex(
                name: "IX_RRPItems_TenantId",
                schema: "am",
                table: "RRPItems");

            migrationBuilder.DropIndex(
                name: "IX_RRPItems_TenantId_RRPId",
                schema: "am",
                table: "RRPItems");

            migrationBuilder.DropIndex(
                name: "IX_RRPItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "RRPItems");

            migrationBuilder.DropIndex(
                name: "IX_PropertyIncidentItems_TenantId",
                schema: "am",
                table: "PropertyIncidentItems");

            migrationBuilder.DropIndex(
                name: "IX_PropertyIncidentItems_TenantId_ReportId",
                schema: "am",
                table: "PropertyIncidentItems");

            migrationBuilder.DropIndex(
                name: "IX_PropertyIncidentItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "PropertyIncidentItems");

            migrationBuilder.DropIndex(
                name: "IX_PropertyCodeCounters_TenantId",
                schema: "am",
                table: "PropertyCodeCounters");

            migrationBuilder.DropIndex(
                name: "IX_PPEIRItems_TenantId",
                schema: "am",
                table: "PPEIRItems");

            migrationBuilder.DropIndex(
                name: "IX_PPEIRItems_TenantId_PPEIRId",
                schema: "am",
                table: "PPEIRItems");

            migrationBuilder.DropIndex(
                name: "IX_PPEIRItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "PPEIRItems");

            migrationBuilder.DropIndex(
                name: "IX_PhysicalCountEntries_TenantId",
                schema: "am",
                table: "PhysicalCountEntries");

            migrationBuilder.DropIndex(
                name: "IX_PhysicalCountEntries_TenantId_Result",
                schema: "am",
                table: "PhysicalCountEntries");

            migrationBuilder.DropIndex(
                name: "IX_PhysicalCountEntries_TenantId_SessionId",
                schema: "am",
                table: "PhysicalCountEntries");

            migrationBuilder.DropIndex(
                name: "IX_PhysicalCountEntries_TenantId_SessionId_TangibleInventoryIt~",
                schema: "am",
                table: "PhysicalCountEntries");

            migrationBuilder.DropIndex(
                name: "IX_PhysicalCountEntries_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "PhysicalCountEntries");

            migrationBuilder.DropIndex(
                name: "IX_PARItems_TenantId",
                schema: "am",
                table: "PARItems");

            migrationBuilder.DropIndex(
                name: "IX_PARItems_TenantId_PARId",
                schema: "am",
                table: "PARItems");

            migrationBuilder.DropIndex(
                name: "IX_PARItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "PARItems");

            migrationBuilder.DropIndex(
                name: "IX_ICSItems_TenantId",
                schema: "am",
                table: "ICSItems");

            migrationBuilder.DropIndex(
                name: "IX_ICSItems_TenantId_ICSId",
                schema: "am",
                table: "ICSItems");

            migrationBuilder.DropIndex(
                name: "IX_ICSItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "ICSItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "UnserviceablePropertyItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "TangibleInventoryItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "SMIRItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "RRSPItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "RRPItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "PropertyIncidentItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "PPEIRItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "PhysicalCountEntries");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "PARItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "ICSItems");
        }
    }
}
