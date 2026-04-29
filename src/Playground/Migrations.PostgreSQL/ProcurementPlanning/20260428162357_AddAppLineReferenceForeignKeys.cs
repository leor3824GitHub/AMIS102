using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.ProcurementPlanning
{
    /// <inheritdoc />
    public partial class AddAppLineReferenceForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_AppLineReferences_PpmpItems_SourcePpmpItemId",
                schema: "procurement_planning",
                table: "AppLineReferences",
                column: "SourcePpmpItemId",
                principalSchema: "procurement_planning",
                principalTable: "PpmpItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppLineReferences_Ppmps_SourcePpmpId",
                schema: "procurement_planning",
                table: "AppLineReferences",
                column: "SourcePpmpId",
                principalSchema: "procurement_planning",
                principalTable: "Ppmps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppLineReferences_PpmpItems_SourcePpmpItemId",
                schema: "procurement_planning",
                table: "AppLineReferences");

            migrationBuilder.DropForeignKey(
                name: "FK_AppLineReferences_Ppmps_SourcePpmpId",
                schema: "procurement_planning",
                table: "AppLineReferences");
        }
    }
}
