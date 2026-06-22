using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.BudgetDisbursement
{
    /// <inheritdoc />
    public partial class AddDisbursementVoucherTenantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementVouchers_TenantId",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetUtilizationRequests_TenantId",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DisbursementVouchers_TenantId",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers");

            migrationBuilder.DropIndex(
                name: "IX_BudgetUtilizationRequests_TenantId",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests");
        }
    }
}
