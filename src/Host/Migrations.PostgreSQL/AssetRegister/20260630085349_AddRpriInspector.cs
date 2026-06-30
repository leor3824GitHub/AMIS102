using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class AddRpriInspector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InspectorId",
                schema: "asset_register",
                table: "PropertyRepairs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectorName",
                schema: "asset_register",
                table: "PropertyRepairs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InspectorId",
                schema: "asset_register",
                table: "PropertyRepairs");

            migrationBuilder.DropColumn(
                name: "InspectorName",
                schema: "asset_register",
                table: "PropertyRepairs");
        }
    }
}
