using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementPlanning
{
    /// <inheritdoc />
    public partial class AppGppbColumns : Migration
    {
        private static readonly string[] BackfillTables = ["PpmpItems", "AppLineItems"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BidEvaluationCriteria",
                schema: "procurement_planning",
                table: "PpmpItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsEarlyProcurement",
                schema: "procurement_planning",
                table: "PpmpItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProcurementStrategy",
                schema: "procurement_planning",
                table: "PpmpItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectTitle",
                schema: "procurement_planning",
                table: "PpmpItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Section",
                schema: "procurement_planning",
                table: "PpmpItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BidEvaluationCriteria",
                schema: "procurement_planning",
                table: "AppLineItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsEarlyProcurement",
                schema: "procurement_planning",
                table: "AppLineItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProcurementStrategy",
                schema: "procurement_planning",
                table: "AppLineItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectTitle",
                schema: "procurement_planning",
                table: "AppLineItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Section",
                schema: "procurement_planning",
                table: "AppLineItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill existing rows with the same defaults ProcurementPlanRules applies to new items:
            // title = description; section/criteria derived from the mode of procurement.
            foreach (var table in BackfillTables)
            {
                migrationBuilder.Sql($"""
                    UPDATE procurement_planning."{table}" SET
                        "ProjectTitle" = LEFT("GeneralDescription", 500),
                        "Section" = CASE
                            WHEN "ModeOfProcurement" ILIKE '%Direct Acquisition%' THEN 1
                            WHEN "ModeOfProcurement" ILIKE '%PS-DBM%' OR "ModeOfProcurement" ILIKE '%Procurement Service%' THEN 2
                            ELSE 0 END,
                        "BidEvaluationCriteria" = CASE
                            WHEN "ModeOfProcurement" ILIKE '%Competitive Bidding%' OR "ModeOfProcurement" ILIKE '%Public Bidding%'
                                THEN CASE WHEN "ProjectType" = 2 THEN 3 ELSE 1 END
                            ELSE 0 END;
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BidEvaluationCriteria",
                schema: "procurement_planning",
                table: "PpmpItems");

            migrationBuilder.DropColumn(
                name: "IsEarlyProcurement",
                schema: "procurement_planning",
                table: "PpmpItems");

            migrationBuilder.DropColumn(
                name: "ProcurementStrategy",
                schema: "procurement_planning",
                table: "PpmpItems");

            migrationBuilder.DropColumn(
                name: "ProjectTitle",
                schema: "procurement_planning",
                table: "PpmpItems");

            migrationBuilder.DropColumn(
                name: "Section",
                schema: "procurement_planning",
                table: "PpmpItems");

            migrationBuilder.DropColumn(
                name: "BidEvaluationCriteria",
                schema: "procurement_planning",
                table: "AppLineItems");

            migrationBuilder.DropColumn(
                name: "IsEarlyProcurement",
                schema: "procurement_planning",
                table: "AppLineItems");

            migrationBuilder.DropColumn(
                name: "ProcurementStrategy",
                schema: "procurement_planning",
                table: "AppLineItems");

            migrationBuilder.DropColumn(
                name: "ProjectTitle",
                schema: "procurement_planning",
                table: "AppLineItems");

            migrationBuilder.DropColumn(
                name: "Section",
                schema: "procurement_planning",
                table: "AppLineItems");
        }
    }
}
