using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementAcquisition
{
    /// <inheritdoc />
    public partial class AddPurchaseRequestNameSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedById",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "RequestedById",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByName",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedByName",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedByName",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "RequestedByName",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedById",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedById",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
