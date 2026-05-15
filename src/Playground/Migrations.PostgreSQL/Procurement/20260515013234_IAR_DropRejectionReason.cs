using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Procurement
{
    /// <inheritdoc />
    public partial class IAR_DropRejectionReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "asset_procurement",
                table: "AssetIARs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "asset_procurement",
                table: "AssetIARs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
