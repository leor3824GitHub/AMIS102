using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementAcquisition
{
    /// <inheritdoc />
    public partial class ProcurementAcquisition_AddAcceptedByToInspectionAcceptanceReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcceptedByDesignation",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AcceptedById",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcceptedByName",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedByDesignation",
                schema: "procurement",
                table: "InspectionAcceptanceReports");

            migrationBuilder.DropColumn(
                name: "AcceptedById",
                schema: "procurement",
                table: "InspectionAcceptanceReports");

            migrationBuilder.DropColumn(
                name: "AcceptedByName",
                schema: "procurement",
                table: "InspectionAcceptanceReports");
        }
    }
}
