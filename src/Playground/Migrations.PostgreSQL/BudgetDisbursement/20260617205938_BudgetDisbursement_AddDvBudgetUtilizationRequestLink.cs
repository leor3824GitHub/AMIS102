using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.BudgetDisbursement
{
    /// <inheritdoc />
    public partial class BudgetDisbursement_AddDvBudgetUtilizationRequestLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BudgetUtilizationRequestId",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "BurNumber",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementVouchers_BudgetUtilizationRequestId",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers",
                column: "BudgetUtilizationRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DisbursementVouchers_BudgetUtilizationRequestId",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers");

            migrationBuilder.DropColumn(
                name: "BudgetUtilizationRequestId",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers");

            migrationBuilder.DropColumn(
                name: "BurNumber",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers");
        }
    }
}
