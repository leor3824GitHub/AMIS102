using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class AssetRegister_ReturnedPropertyAssignedInspector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedInspector_Designation",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedInspector_EmployeeId",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "AssignedInspector_PrintedName",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedInspector_Designation",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.DropColumn(
                name: "AssignedInspector_EmployeeId",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.DropColumn(
                name: "AssignedInspector_PrintedName",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");
        }
    }
}
