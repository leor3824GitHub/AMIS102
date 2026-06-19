using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.MasterData
{
    /// <inheritdoc />
    public partial class MasterData_AddPropertyCustodian : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PropertyCustodianDesignation",
                schema: "masterdata",
                table: "OrganizationProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PropertyCustodianId",
                schema: "masterdata",
                table: "OrganizationProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PropertyCustodianName",
                schema: "masterdata",
                table: "OrganizationProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PropertyCustodianDesignation",
                schema: "masterdata",
                table: "OrganizationProfiles");

            migrationBuilder.DropColumn(
                name: "PropertyCustodianId",
                schema: "masterdata",
                table: "OrganizationProfiles");

            migrationBuilder.DropColumn(
                name: "PropertyCustodianName",
                schema: "masterdata",
                table: "OrganizationProfiles");
        }
    }
}
