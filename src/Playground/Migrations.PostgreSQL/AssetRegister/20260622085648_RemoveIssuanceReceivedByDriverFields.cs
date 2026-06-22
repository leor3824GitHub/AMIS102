using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class RemoveIssuanceReceivedByDriverFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillOfLadingNo",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "DateReceived",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "DriverName",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "ReceivedBy_Designation",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "ReceivedBy_EmployeeId",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "ReceivedBy_PrintedName",
                schema: "asset_register",
                table: "PropertyIssuanceReports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillOfLadingNo",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateReceived",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceivedBy_Designation",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReceivedBy_EmployeeId",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ReceivedBy_PrintedName",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
