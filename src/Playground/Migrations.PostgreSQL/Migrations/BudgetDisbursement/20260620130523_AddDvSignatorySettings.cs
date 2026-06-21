using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Migrations.BudgetDisbursement
{
    /// <inheritdoc />
    public partial class AddBudgetDisbursementDvSignatorySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DvSectionADesignation",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DvSectionAName",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DvSectionBDesignation",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DvSectionBName",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DvSectionCDesignation",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DvSectionCName",
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
                name: "DvSectionADesignation",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings");

            migrationBuilder.DropColumn(
                name: "DvSectionAName",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings");

            migrationBuilder.DropColumn(
                name: "DvSectionBDesignation",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings");

            migrationBuilder.DropColumn(
                name: "DvSectionBName",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings");

            migrationBuilder.DropColumn(
                name: "DvSectionCDesignation",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings");

            migrationBuilder.DropColumn(
                name: "DvSectionCName",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings");
        }
    }
}
