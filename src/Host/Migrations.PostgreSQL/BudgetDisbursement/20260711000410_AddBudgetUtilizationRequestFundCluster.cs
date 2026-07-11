using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.BudgetDisbursement
{
    /// <inheritdoc />
    public partial class AddBudgetUtilizationRequestFundCluster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FundCluster",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FundCluster",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests");
        }
    }
}
