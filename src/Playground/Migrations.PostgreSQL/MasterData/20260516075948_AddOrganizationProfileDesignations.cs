using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.MasterData
{
    /// <inheritdoc />
    public partial class AddOrganizationProfileDesignations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountantDesignation",
                schema: "masterdata",
                table: "OrganizationProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssistantRegionalManagerDesignation",
                schema: "masterdata",
                table: "OrganizationProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegionalManagerDesignation",
                schema: "masterdata",
                table: "OrganizationProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisingAdminOfficerDesignation",
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
                name: "AccountantDesignation",
                schema: "masterdata",
                table: "OrganizationProfiles");

            migrationBuilder.DropColumn(
                name: "AssistantRegionalManagerDesignation",
                schema: "masterdata",
                table: "OrganizationProfiles");

            migrationBuilder.DropColumn(
                name: "RegionalManagerDesignation",
                schema: "masterdata",
                table: "OrganizationProfiles");

            migrationBuilder.DropColumn(
                name: "SupervisingAdminOfficerDesignation",
                schema: "masterdata",
                table: "OrganizationProfiles");
        }
    }
}
