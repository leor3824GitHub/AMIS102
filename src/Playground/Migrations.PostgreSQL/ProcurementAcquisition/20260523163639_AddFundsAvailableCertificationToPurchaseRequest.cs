using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementAcquisition
{
    /// <inheritdoc />
    public partial class AddFundsAvailableCertificationToPurchaseRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedById",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedOnUtc",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FundsAvailableCertifiedById",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundsAvailableCertifiedByName",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FundsAvailableCertifiedOnUtc",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReturnedById",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnedByName",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReturnedOnUtc",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnedReason",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedById",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "ApprovedOnUtc",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "FundsAvailableCertifiedById",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "FundsAvailableCertifiedByName",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "FundsAvailableCertifiedOnUtc",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "ReturnedById",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "ReturnedByName",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "ReturnedOnUtc",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "ReturnedReason",
                schema: "procurement",
                table: "PurchaseRequests");
        }
    }
}
