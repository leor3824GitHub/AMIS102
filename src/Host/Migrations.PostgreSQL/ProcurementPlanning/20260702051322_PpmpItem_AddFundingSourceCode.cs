using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementPlanning
{
    /// <inheritdoc />
    public partial class PpmpItem_AddFundingSourceCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FundingSourceCode",
                schema: "procurement_planning",
                table: "PpmpItems",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FundingSourceCode",
                schema: "procurement_planning",
                table: "PpmpItems");
        }
    }
}
