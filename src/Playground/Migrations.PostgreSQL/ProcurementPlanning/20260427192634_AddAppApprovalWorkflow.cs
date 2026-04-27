using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.ProcurementPlanning
{
    /// <inheritdoc />
    public partial class AddAppApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReturnReason",
                schema: "procurement_planning",
                table: "AnnualProcurementPlans",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReturnedAt",
                schema: "procurement_planning",
                table: "AnnualProcurementPlans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReturnedById",
                schema: "procurement_planning",
                table: "AnnualProcurementPlans",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnReason",
                schema: "procurement_planning",
                table: "AnnualProcurementPlans");

            migrationBuilder.DropColumn(
                name: "ReturnedAt",
                schema: "procurement_planning",
                table: "AnnualProcurementPlans");

            migrationBuilder.DropColumn(
                name: "ReturnedById",
                schema: "procurement_planning",
                table: "AnnualProcurementPlans");
        }
    }
}
