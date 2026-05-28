using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementAcquisition
{
    /// <inheritdoc />
    public partial class AddFundsAvailableCertificationToPurchaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FundsAvailableCertifiedById",
                schema: "procurement",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundsAvailableCertifiedByName",
                schema: "procurement",
                table: "PurchaseOrders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FundsAvailableCertifiedOnUtc",
                schema: "procurement",
                table: "PurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "OursBursDate",
                schema: "procurement",
                table: "PurchaseOrders",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FundsAvailableCertifiedById",
                schema: "procurement",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "FundsAvailableCertifiedByName",
                schema: "procurement",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "FundsAvailableCertifiedOnUtc",
                schema: "procurement",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "OursBursDate",
                schema: "procurement",
                table: "PurchaseOrders");
        }
    }
}
