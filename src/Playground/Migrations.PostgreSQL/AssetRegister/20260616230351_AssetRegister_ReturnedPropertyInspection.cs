using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class AssetRegister_ReturnedPropertyInspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InspectedBy_Designation",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InspectedBy_EmployeeId",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectedBy_PrintedName",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InspectedOnUtc",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectionRemarks",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InspectedCondition",
                schema: "asset_register",
                table: "ReturnedPropertyReceiptItems",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InspectedBy_Designation",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.DropColumn(
                name: "InspectedBy_EmployeeId",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.DropColumn(
                name: "InspectedBy_PrintedName",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.DropColumn(
                name: "InspectedOnUtc",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.DropColumn(
                name: "InspectionRemarks",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.DropColumn(
                name: "InspectedCondition",
                schema: "asset_register",
                table: "ReturnedPropertyReceiptItems");
        }
    }
}
