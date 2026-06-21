using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Migrations.BudgetDisbursement
{
    /// <inheritdoc />
    public partial class BudgetDisbursement_AddBurSignatories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BurSectionADesignation",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BurSectionAName",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BurSectionBDesignation",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BurSectionBName",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BurSectionADesignation",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings");

            migrationBuilder.DropColumn(
                name: "BurSectionAName",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings");

            migrationBuilder.DropColumn(
                name: "BurSectionBDesignation",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings");

            migrationBuilder.DropColumn(
                name: "BurSectionBName",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings");
        }
    }
}
