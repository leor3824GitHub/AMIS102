using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Vehicle
{
    /// <inheritdoc />
    public partial class VehicleAssetEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountableOfficerTitle",
                schema: "vehicle",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "AssignedDepartment",
                schema: "vehicle",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "AssignedDepartmentId",
                schema: "vehicle",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "AssignedDriver",
                schema: "vehicle",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "AssignedDriverId",
                schema: "vehicle",
                table: "Vehicles");

            migrationBuilder.AddColumn<DateOnly>(
                name: "AcquisitionDate",
                schema: "vehicle",
                table: "Vehicles",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<Guid>(
                name: "AssetRegistryId",
                schema: "vehicle",
                table: "Vehicles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "PropertyNo",
                schema: "vehicle",
                table: "Vehicles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_TenantId_AssetRegistryId",
                schema: "vehicle",
                table: "Vehicles",
                columns: new[] { "TenantId", "AssetRegistryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehicles_TenantId_AssetRegistryId",
                schema: "vehicle",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "AcquisitionDate",
                schema: "vehicle",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "AssetRegistryId",
                schema: "vehicle",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "PropertyNo",
                schema: "vehicle",
                table: "Vehicles");

            migrationBuilder.AddColumn<string>(
                name: "AccountableOfficerTitle",
                schema: "vehicle",
                table: "Vehicles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedDepartment",
                schema: "vehicle",
                table: "Vehicles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedDepartmentId",
                schema: "vehicle",
                table: "Vehicles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedDriver",
                schema: "vehicle",
                table: "Vehicles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedDriverId",
                schema: "vehicle",
                table: "Vehicles",
                type: "uuid",
                nullable: true);
        }
    }
}
