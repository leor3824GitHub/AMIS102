using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Migrations.BudgetDisbursement
{
    /// <inheritdoc />
    public partial class AddBudgetDisbursementModuleSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BudgetDisbursementModuleSettings",
                schema: "budgetdisbursement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WatermarkSignedCopies = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetDisbursementModuleSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetDisbursementModuleSettings_TenantId",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetDisbursementModuleSettings",
                schema: "budgetdisbursement");
        }
    }
}
