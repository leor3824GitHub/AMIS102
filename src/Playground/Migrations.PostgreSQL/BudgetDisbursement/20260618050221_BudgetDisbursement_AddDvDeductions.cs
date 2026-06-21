using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.BudgetDisbursement
{
    /// <inheritdoc />
    public partial class BudgetDisbursement_AddDvDeductions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DisbursementVoucherDeductions",
                schema: "budgetdisbursement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DisbursementVoucherId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisbursementVoucherDeductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisbursementVoucherDeductions_DisbursementVouchers_Disburse~",
                        column: x => x.DisbursementVoucherId,
                        principalSchema: "budgetdisbursement",
                        principalTable: "DisbursementVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementVoucherDeductions_DisbursementVoucherId",
                schema: "budgetdisbursement",
                table: "DisbursementVoucherDeductions",
                column: "DisbursementVoucherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DisbursementVoucherDeductions",
                schema: "budgetdisbursement");
        }
    }
}
