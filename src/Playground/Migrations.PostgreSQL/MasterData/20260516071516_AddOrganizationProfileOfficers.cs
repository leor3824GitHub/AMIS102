using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.MasterData
{
    /// <inheritdoc />
    public partial class AddOrganizationProfileOfficers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccountantId",
                schema: "masterdata",
                table: "OrganizationProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountantName",
                schema: "masterdata",
                table: "OrganizationProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssistantRegionalManagerId",
                schema: "masterdata",
                table: "OrganizationProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssistantRegionalManagerName",
                schema: "masterdata",
                table: "OrganizationProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RegionalManagerId",
                schema: "masterdata",
                table: "OrganizationProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegionalManagerName",
                schema: "masterdata",
                table: "OrganizationProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupervisingAdminOfficerId",
                schema: "masterdata",
                table: "OrganizationProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisingAdminOfficerName",
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
                name: "AccountantId",
                schema: "masterdata",
                table: "OrganizationProfiles");

            migrationBuilder.DropColumn(
                name: "AccountantName",
                schema: "masterdata",
                table: "OrganizationProfiles");

            migrationBuilder.DropColumn(
                name: "AssistantRegionalManagerId",
                schema: "masterdata",
                table: "OrganizationProfiles");

            migrationBuilder.DropColumn(
                name: "AssistantRegionalManagerName",
                schema: "masterdata",
                table: "OrganizationProfiles");

            migrationBuilder.DropColumn(
                name: "RegionalManagerId",
                schema: "masterdata",
                table: "OrganizationProfiles");

            migrationBuilder.DropColumn(
                name: "RegionalManagerName",
                schema: "masterdata",
                table: "OrganizationProfiles");

            migrationBuilder.DropColumn(
                name: "SupervisingAdminOfficerId",
                schema: "masterdata",
                table: "OrganizationProfiles");

            migrationBuilder.DropColumn(
                name: "SupervisingAdminOfficerName",
                schema: "masterdata",
                table: "OrganizationProfiles");
        }
    }
}
