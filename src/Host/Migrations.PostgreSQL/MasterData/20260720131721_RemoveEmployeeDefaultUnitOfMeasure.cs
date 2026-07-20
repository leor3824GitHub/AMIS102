using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.MasterData
{
    /// <inheritdoc />
    public partial class RemoveEmployeeDefaultUnitOfMeasure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeProfiles_UnitOfMeasures_DefaultUnitOfMeasureId",
                schema: "masterdata",
                table: "EmployeeProfiles");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProfiles_DefaultUnitOfMeasureId",
                schema: "masterdata",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "DefaultUnitOfMeasureId",
                schema: "masterdata",
                table: "EmployeeProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DefaultUnitOfMeasureId",
                schema: "masterdata",
                table: "EmployeeProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_DefaultUnitOfMeasureId",
                schema: "masterdata",
                table: "EmployeeProfiles",
                column: "DefaultUnitOfMeasureId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeProfiles_UnitOfMeasures_DefaultUnitOfMeasureId",
                schema: "masterdata",
                table: "EmployeeProfiles",
                column: "DefaultUnitOfMeasureId",
                principalSchema: "masterdata",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
