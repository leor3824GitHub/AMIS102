using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.BudgetDisbursement
{
    /// <inheritdoc />
    public partial class BudgetDisbursement_AddNumberSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BurNumberSequences",
                schema: "budgetdisbursement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    LastSerial = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BurNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DvNumberSequences",
                schema: "budgetdisbursement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    LastSerial = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DvNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BurNumberSequences_Year",
                schema: "budgetdisbursement",
                table: "BurNumberSequences",
                column: "Year",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DvNumberSequences_Year",
                schema: "budgetdisbursement",
                table: "DvNumberSequences",
                column: "Year",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BurNumberSequences",
                schema: "budgetdisbursement");

            migrationBuilder.DropTable(
                name: "DvNumberSequences",
                schema: "budgetdisbursement");
        }
    }
}
