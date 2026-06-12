using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class AssetRegister_IssuanceRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PropertyIssuanceReports_TenantId_Status",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropIndex(
                name: "IX_PropertyIssuanceReportLines_AccountabilityId",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines");

            migrationBuilder.DropIndex(
                name: "IX_PropertyIssuanceReportLines_AccountabilityLineId",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines");

            migrationBuilder.DropIndex(
                name: "IX_PropertyIssuanceReportLines_ReportId",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines");

            migrationBuilder.DropColumn(
                name: "PeriodFromDate",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "AccountabilityId",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines");

            migrationBuilder.DropColumn(
                name: "AccountabilityLineId",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines");

            migrationBuilder.DropColumn(
                name: "SnapshotResponsibilityCenterCode",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines");

            migrationBuilder.RenameColumn(
                name: "PreparedBy_PrintedName",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "ReceivedBy_PrintedName");

            migrationBuilder.RenameColumn(
                name: "PreparedBy_EmployeeId",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "ReceivedBy_EmployeeId");

            migrationBuilder.RenameColumn(
                name: "PreparedBy_Designation",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "ReceivedBy_Designation");

            migrationBuilder.RenameColumn(
                name: "PostedBy_PrintedName",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "IssuedTo_PrintedName");

            migrationBuilder.RenameColumn(
                name: "PostedBy_EmployeeId",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "IssuedTo_EmployeeId");

            migrationBuilder.RenameColumn(
                name: "PostedBy_Designation",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "IssuedTo_Designation");

            migrationBuilder.RenameColumn(
                name: "CertifiedBy_PrintedName",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "IssuedBy_PrintedName");

            migrationBuilder.RenameColumn(
                name: "CertifiedBy_EmployeeId",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "IssuedBy_EmployeeId");

            migrationBuilder.RenameColumn(
                name: "CertifiedBy_Designation",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "IssuedBy_Designation");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "Nature");

            migrationBuilder.RenameColumn(
                name: "PostedOn",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "DateReceived");

            migrationBuilder.RenameColumn(
                name: "PeriodToDate",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "SnapshotQuantityIssued",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines",
                newName: "ItemNo");

            migrationBuilder.AlterColumn<string>(
                name: "IssuedTo_PrintedName",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "IssuedTo_EmployeeId",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IssuedBy_PrintedName",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "IssuedBy_EmployeeId",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy_Designation",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedBy_EmployeeId",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy_PrintedName",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BillOfLadingNo",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuedToOfficeAddress",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AccumulatedDepreciation",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BookValue",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIssuanceReports_TenantId_Date",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                columns: new[] { "TenantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIssuanceReportLines_ReportId_ItemNo",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines",
                columns: new[] { "ReportId", "ItemNo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PropertyIssuanceReports_TenantId_Date",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropIndex(
                name: "IX_PropertyIssuanceReportLines_ReportId_ItemNo",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines");

            migrationBuilder.DropColumn(
                name: "ApprovedBy_Designation",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "ApprovedBy_EmployeeId",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "ApprovedBy_PrintedName",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "BillOfLadingNo",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "DriverName",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "IssuedToOfficeAddress",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "Remarks",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "AccumulatedDepreciation",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines");

            migrationBuilder.DropColumn(
                name: "BookValue",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines");

            migrationBuilder.RenameColumn(
                name: "ReceivedBy_PrintedName",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "PreparedBy_PrintedName");

            migrationBuilder.RenameColumn(
                name: "ReceivedBy_EmployeeId",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "PreparedBy_EmployeeId");

            migrationBuilder.RenameColumn(
                name: "ReceivedBy_Designation",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "PreparedBy_Designation");

            migrationBuilder.RenameColumn(
                name: "IssuedTo_PrintedName",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "PostedBy_PrintedName");

            migrationBuilder.RenameColumn(
                name: "IssuedTo_EmployeeId",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "PostedBy_EmployeeId");

            migrationBuilder.RenameColumn(
                name: "IssuedTo_Designation",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "PostedBy_Designation");

            migrationBuilder.RenameColumn(
                name: "IssuedBy_PrintedName",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "CertifiedBy_PrintedName");

            migrationBuilder.RenameColumn(
                name: "IssuedBy_EmployeeId",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "CertifiedBy_EmployeeId");

            migrationBuilder.RenameColumn(
                name: "IssuedBy_Designation",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "CertifiedBy_Designation");

            migrationBuilder.RenameColumn(
                name: "Nature",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "DateReceived",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "PostedOn");

            migrationBuilder.RenameColumn(
                name: "Date",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                newName: "PeriodToDate");

            migrationBuilder.RenameColumn(
                name: "ItemNo",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines",
                newName: "SnapshotQuantityIssued");

            migrationBuilder.AlterColumn<string>(
                name: "PostedBy_PrintedName",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<Guid>(
                name: "PostedBy_EmployeeId",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "CertifiedBy_PrintedName",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<Guid>(
                name: "CertifiedBy_EmployeeId",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateOnly>(
                name: "PeriodFromDate",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<Guid>(
                name: "AccountabilityId",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "AccountabilityLineId",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "SnapshotResponsibilityCenterCode",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIssuanceReports_TenantId_Status",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIssuanceReportLines_AccountabilityId",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines",
                column: "AccountabilityId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIssuanceReportLines_AccountabilityLineId",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines",
                column: "AccountabilityLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIssuanceReportLines_ReportId",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines",
                column: "ReportId");
        }
    }
}
