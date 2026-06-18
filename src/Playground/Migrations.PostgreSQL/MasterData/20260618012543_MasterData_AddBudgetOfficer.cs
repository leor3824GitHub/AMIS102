using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.MasterData
{
    /// <inheritdoc />
    public partial class MasterData_AddBudgetOfficer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BudgetOfficerDesignation",
                schema: "masterdata",
                table: "OrganizationProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BudgetOfficerId",
                schema: "masterdata",
                table: "OrganizationProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BudgetOfficerName",
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
                name: "BudgetOfficerDesignation",
                schema: "masterdata",
                table: "OrganizationProfiles");

            migrationBuilder.DropColumn(
                name: "BudgetOfficerId",
                schema: "masterdata",
                table: "OrganizationProfiles");

            migrationBuilder.DropColumn(
                name: "BudgetOfficerName",
                schema: "masterdata",
                table: "OrganizationProfiles");
        }
    }
}
