using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class ReturnedPropertyReturnWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ReceiptNo",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcceptedOnUtc",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResolvedOnUtc",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnedPropertyReceipts_TenantId_Status",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReturnedPropertyReceipts_TenantId_Status",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.DropColumn(
                name: "AcceptedOnUtc",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.DropColumn(
                name: "ResolvedOnUtc",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.AlterColumn<string>(
                name: "ReceiptNo",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);
        }
    }
}
