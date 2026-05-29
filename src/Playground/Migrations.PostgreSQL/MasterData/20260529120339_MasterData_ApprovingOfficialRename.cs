using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.MasterData
{
    /// <inheritdoc />
    public partial class MasterData_ApprovingOfficialRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RegionalManagerId",
                schema: "masterdata",
                table: "OrganizationProfiles",
                newName: "ApprovingOfficialId");

            migrationBuilder.RenameColumn(
                name: "RegionalManagerName",
                schema: "masterdata",
                table: "OrganizationProfiles",
                newName: "ApprovingOfficialName");

            migrationBuilder.RenameColumn(
                name: "RegionalManagerDesignation",
                schema: "masterdata",
                table: "OrganizationProfiles",
                newName: "ApprovingOfficialDesignation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ApprovingOfficialId",
                schema: "masterdata",
                table: "OrganizationProfiles",
                newName: "RegionalManagerId");

            migrationBuilder.RenameColumn(
                name: "ApprovingOfficialName",
                schema: "masterdata",
                table: "OrganizationProfiles",
                newName: "RegionalManagerName");

            migrationBuilder.RenameColumn(
                name: "ApprovingOfficialDesignation",
                schema: "masterdata",
                table: "OrganizationProfiles",
                newName: "RegionalManagerDesignation");
        }
    }
}
