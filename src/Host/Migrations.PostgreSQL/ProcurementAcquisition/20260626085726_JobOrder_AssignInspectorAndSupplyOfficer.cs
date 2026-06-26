using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementAcquisition
{
    /// <inheritdoc />
    public partial class JobOrder_AssignInspectorAndSupplyOfficer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedByDesignation",
                schema: "procurement",
                table: "JobOrders");

            migrationBuilder.DropColumn(
                name: "AcceptedByName",
                schema: "procurement",
                table: "JobOrders");

            migrationBuilder.DropColumn(
                name: "InspectedByDesignation",
                schema: "procurement",
                table: "JobOrders");

            migrationBuilder.DropColumn(
                name: "InspectedById",
                schema: "procurement",
                table: "JobOrders");

            migrationBuilder.RenameColumn(
                name: "InspectedByName",
                schema: "procurement",
                table: "JobOrders",
                newName: "InspectorDesignation");

            migrationBuilder.AddColumn<Guid>(
                name: "InspectorId",
                schema: "procurement",
                table: "JobOrders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "InspectorName",
                schema: "procurement",
                table: "JobOrders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InspectorId",
                schema: "procurement",
                table: "JobOrders");

            migrationBuilder.DropColumn(
                name: "InspectorName",
                schema: "procurement",
                table: "JobOrders");

            migrationBuilder.RenameColumn(
                name: "InspectorDesignation",
                schema: "procurement",
                table: "JobOrders",
                newName: "InspectedByName");

            migrationBuilder.AddColumn<string>(
                name: "AcceptedByDesignation",
                schema: "procurement",
                table: "JobOrders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcceptedByName",
                schema: "procurement",
                table: "JobOrders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectedByDesignation",
                schema: "procurement",
                table: "JobOrders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InspectedById",
                schema: "procurement",
                table: "JobOrders",
                type: "uuid",
                nullable: true);
        }
    }
}
