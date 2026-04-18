using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.AssetManagement
{
    /// <inheritdoc />
    public partial class AddTenantIsolationToAssetManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnserviceablePropertyReports_ReportNo",
                schema: "am",
                table: "UnserviceablePropertyReports");

            migrationBuilder.DropIndex(
                name: "IX_SMRRs_SMRRNo",
                schema: "am",
                table: "SMRRs");

            migrationBuilder.DropIndex(
                name: "IX_SemiExpendableProperties_PropertyNo",
                schema: "am",
                table: "SemiExpendableProperties");

            migrationBuilder.DropIndex(
                name: "IX_SemiExpendableIssuanceRecords_SMIRNo",
                schema: "am",
                table: "SemiExpendableIssuanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_ReceiptsForReturnedPPE_RRPNo",
                schema: "am",
                table: "ReceiptsForReturnedPPE");

            migrationBuilder.DropIndex(
                name: "IX_ReceiptForReturnedProperties_RRSPNo",
                schema: "am",
                table: "ReceiptForReturnedProperties");

            migrationBuilder.DropIndex(
                name: "IX_PropertyItemCatalog_Code",
                schema: "am",
                table: "PropertyItemCatalog");

            migrationBuilder.DropIndex(
                name: "IX_PropertyIncidentReports_ReportNo",
                schema: "am",
                table: "PropertyIncidentReports");

            migrationBuilder.DropIndex(
                name: "IX_PropertyAcknowledgementReceipts_PARNo",
                schema: "am",
                table: "PropertyAcknowledgementReceipts");

            migrationBuilder.DropIndex(
                name: "IX_PPEReceivingReports_PPERRNo",
                schema: "am",
                table: "PPEReceivingReports");

            migrationBuilder.DropIndex(
                name: "IX_PPEItems_PropertyCode",
                schema: "am",
                table: "PPEItems");

            migrationBuilder.DropIndex(
                name: "IX_PPEItems_PropertyNumber",
                schema: "am",
                table: "PPEItems");

            migrationBuilder.DropIndex(
                name: "IX_PPEIssuanceReports_PPEIRNo",
                schema: "am",
                table: "PPEIssuanceReports");

            migrationBuilder.DropIndex(
                name: "IX_PhysicalCountSessions_SessionNo",
                schema: "am",
                table: "PhysicalCountSessions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryCustodianSlips_ICSNo",
                schema: "am",
                table: "InventoryCustodianSlips");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "UnserviceablePropertyReports",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "SMRRs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "SemiExpendableProperties",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "SemiExpendableIssuanceRecords",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "ReclassificationRecords",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "ReceiptsForReturnedPPE",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "ReceiptForReturnedProperties",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "PropertyItemCatalog",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "PropertyIncidentReports",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "PropertyAcknowledgementReceipts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "PPEReceivingReports",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "PPEItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "PPEIssuanceReports",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "PhysicalCountSessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "am",
                table: "InventoryCustodianSlips",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyReports_TenantId_ReportNo",
                schema: "am",
                table: "UnserviceablePropertyReports",
                columns: new[] { "TenantId", "ReportNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SMRRs_TenantId_SMRRNo",
                schema: "am",
                table: "SMRRs",
                columns: new[] { "TenantId", "SMRRNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableProperties_TenantId_PropertyNo",
                schema: "am",
                table: "SemiExpendableProperties",
                columns: new[] { "TenantId", "PropertyNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableIssuanceRecords_TenantId_SMIRNo",
                schema: "am",
                table: "SemiExpendableIssuanceRecords",
                columns: new[] { "TenantId", "SMIRNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReclassificationRecords_TenantId",
                schema: "am",
                table: "ReclassificationRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptsForReturnedPPE_TenantId_RRPNo",
                schema: "am",
                table: "ReceiptsForReturnedPPE",
                columns: new[] { "TenantId", "RRPNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptForReturnedProperties_TenantId_RRSPNo",
                schema: "am",
                table: "ReceiptForReturnedProperties",
                columns: new[] { "TenantId", "RRSPNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyItemCatalog_TenantId_Code",
                schema: "am",
                table: "PropertyItemCatalog",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIncidentReports_TenantId_ReportNo",
                schema: "am",
                table: "PropertyIncidentReports",
                columns: new[] { "TenantId", "ReportNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyAcknowledgementReceipts_TenantId_PARNo",
                schema: "am",
                table: "PropertyAcknowledgementReceipts",
                columns: new[] { "TenantId", "PARNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PPEReceivingReports_TenantId_PPERRNo",
                schema: "am",
                table: "PPEReceivingReports",
                columns: new[] { "TenantId", "PPERRNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PPEItems_TenantId_PropertyCode",
                schema: "am",
                table: "PPEItems",
                columns: new[] { "TenantId", "PropertyCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PPEItems_TenantId_PropertyNumber",
                schema: "am",
                table: "PPEItems",
                columns: new[] { "TenantId", "PropertyNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PPEIssuanceReports_TenantId_PPEIRNo",
                schema: "am",
                table: "PPEIssuanceReports",
                columns: new[] { "TenantId", "PPEIRNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountSessions_TenantId_SessionNo",
                schema: "am",
                table: "PhysicalCountSessions",
                columns: new[] { "TenantId", "SessionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCustodianSlips_TenantId_ICSNo",
                schema: "am",
                table: "InventoryCustodianSlips",
                columns: new[] { "TenantId", "ICSNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnserviceablePropertyReports_TenantId_ReportNo",
                schema: "am",
                table: "UnserviceablePropertyReports");

            migrationBuilder.DropIndex(
                name: "IX_SMRRs_TenantId_SMRRNo",
                schema: "am",
                table: "SMRRs");

            migrationBuilder.DropIndex(
                name: "IX_SemiExpendableProperties_TenantId_PropertyNo",
                schema: "am",
                table: "SemiExpendableProperties");

            migrationBuilder.DropIndex(
                name: "IX_SemiExpendableIssuanceRecords_TenantId_SMIRNo",
                schema: "am",
                table: "SemiExpendableIssuanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_ReclassificationRecords_TenantId",
                schema: "am",
                table: "ReclassificationRecords");

            migrationBuilder.DropIndex(
                name: "IX_ReceiptsForReturnedPPE_TenantId_RRPNo",
                schema: "am",
                table: "ReceiptsForReturnedPPE");

            migrationBuilder.DropIndex(
                name: "IX_ReceiptForReturnedProperties_TenantId_RRSPNo",
                schema: "am",
                table: "ReceiptForReturnedProperties");

            migrationBuilder.DropIndex(
                name: "IX_PropertyItemCatalog_TenantId_Code",
                schema: "am",
                table: "PropertyItemCatalog");

            migrationBuilder.DropIndex(
                name: "IX_PropertyIncidentReports_TenantId_ReportNo",
                schema: "am",
                table: "PropertyIncidentReports");

            migrationBuilder.DropIndex(
                name: "IX_PropertyAcknowledgementReceipts_TenantId_PARNo",
                schema: "am",
                table: "PropertyAcknowledgementReceipts");

            migrationBuilder.DropIndex(
                name: "IX_PPEReceivingReports_TenantId_PPERRNo",
                schema: "am",
                table: "PPEReceivingReports");

            migrationBuilder.DropIndex(
                name: "IX_PPEItems_TenantId_PropertyCode",
                schema: "am",
                table: "PPEItems");

            migrationBuilder.DropIndex(
                name: "IX_PPEItems_TenantId_PropertyNumber",
                schema: "am",
                table: "PPEItems");

            migrationBuilder.DropIndex(
                name: "IX_PPEIssuanceReports_TenantId_PPEIRNo",
                schema: "am",
                table: "PPEIssuanceReports");

            migrationBuilder.DropIndex(
                name: "IX_PhysicalCountSessions_TenantId_SessionNo",
                schema: "am",
                table: "PhysicalCountSessions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryCustodianSlips_TenantId_ICSNo",
                schema: "am",
                table: "InventoryCustodianSlips");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "UnserviceablePropertyReports");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "SMRRs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "SemiExpendableProperties");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "SemiExpendableIssuanceRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "ReclassificationRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "ReceiptsForReturnedPPE");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "ReceiptForReturnedProperties");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "PropertyItemCatalog");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "PropertyIncidentReports");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "PropertyAcknowledgementReceipts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "PPEReceivingReports");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "PPEItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "PPEIssuanceReports");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "PhysicalCountSessions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "am",
                table: "InventoryCustodianSlips");

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyReports_ReportNo",
                schema: "am",
                table: "UnserviceablePropertyReports",
                column: "ReportNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SMRRs_SMRRNo",
                schema: "am",
                table: "SMRRs",
                column: "SMRRNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableProperties_PropertyNo",
                schema: "am",
                table: "SemiExpendableProperties",
                column: "PropertyNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableIssuanceRecords_SMIRNo",
                schema: "am",
                table: "SemiExpendableIssuanceRecords",
                column: "SMIRNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptsForReturnedPPE_RRPNo",
                schema: "am",
                table: "ReceiptsForReturnedPPE",
                column: "RRPNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptForReturnedProperties_RRSPNo",
                schema: "am",
                table: "ReceiptForReturnedProperties",
                column: "RRSPNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyItemCatalog_Code",
                schema: "am",
                table: "PropertyItemCatalog",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIncidentReports_ReportNo",
                schema: "am",
                table: "PropertyIncidentReports",
                column: "ReportNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyAcknowledgementReceipts_PARNo",
                schema: "am",
                table: "PropertyAcknowledgementReceipts",
                column: "PARNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PPEReceivingReports_PPERRNo",
                schema: "am",
                table: "PPEReceivingReports",
                column: "PPERRNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PPEItems_PropertyCode",
                schema: "am",
                table: "PPEItems",
                column: "PropertyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PPEItems_PropertyNumber",
                schema: "am",
                table: "PPEItems",
                column: "PropertyNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PPEIssuanceReports_PPEIRNo",
                schema: "am",
                table: "PPEIssuanceReports",
                column: "PPEIRNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountSessions_SessionNo",
                schema: "am",
                table: "PhysicalCountSessions",
                column: "SessionNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCustodianSlips_ICSNo",
                schema: "am",
                table: "InventoryCustodianSlips",
                column: "ICSNo",
                unique: true);
        }
    }
}
