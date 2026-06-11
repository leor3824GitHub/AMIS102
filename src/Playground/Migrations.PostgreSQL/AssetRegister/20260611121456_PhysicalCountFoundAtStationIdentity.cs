using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class PhysicalCountFoundAtStationIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProposedCatalogItemId",
                schema: "asset_register",
                table: "PhysicalCountEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedPropertyNo",
                schema: "asset_register",
                table: "PhysicalCountEntries",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProposedCatalogItemId",
                schema: "asset_register",
                table: "PhysicalCountEntries");

            migrationBuilder.DropColumn(
                name: "ProposedPropertyNo",
                schema: "asset_register",
                table: "PhysicalCountEntries");
        }
    }
}
