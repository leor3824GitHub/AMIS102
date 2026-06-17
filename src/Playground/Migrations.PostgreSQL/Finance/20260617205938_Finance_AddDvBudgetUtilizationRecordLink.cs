using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Finance
{
    /// <inheritdoc />
    public partial class Finance_AddDvBudgetUtilizationRecordLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BudgetUtilizationRecordId",
                schema: "finance",
                table: "DisbursementVouchers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "BurNumber",
                schema: "finance",
                table: "DisbursementVouchers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementVouchers_BudgetUtilizationRecordId",
                schema: "finance",
                table: "DisbursementVouchers",
                column: "BudgetUtilizationRecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DisbursementVouchers_BudgetUtilizationRecordId",
                schema: "finance",
                table: "DisbursementVouchers");

            migrationBuilder.DropColumn(
                name: "BudgetUtilizationRecordId",
                schema: "finance",
                table: "DisbursementVouchers");

            migrationBuilder.DropColumn(
                name: "BurNumber",
                schema: "finance",
                table: "DisbursementVouchers");
        }
    }
}
