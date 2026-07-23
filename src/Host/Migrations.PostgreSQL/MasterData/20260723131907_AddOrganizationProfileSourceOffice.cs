using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.MasterData
{
    /// <inheritdoc />
    public partial class AddOrganizationProfileSourceOffice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceOfficeId",
                schema: "masterdata",
                table: "OrganizationProfiles",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceOfficeId",
                schema: "masterdata",
                table: "OrganizationProfiles");
        }
    }
}
