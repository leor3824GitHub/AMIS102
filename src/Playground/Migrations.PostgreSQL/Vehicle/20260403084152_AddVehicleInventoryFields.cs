using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.Vehicle
{
    /// <inheritdoc />
    public partial class AddVehicleInventoryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountableOfficerTitle",
                schema: "vehicle",
                table: "Vehicles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AcquisitionCost",
                schema: "vehicle",
                table: "Vehicles",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChassisNumber",
                schema: "vehicle",
                table: "Vehicles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EngineDisplacementCC",
                schema: "vehicle",
                table: "Vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuelType",
                schema: "vehicle",
                table: "Vehicles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotorNumber",
                schema: "vehicle",
                table: "Vehicles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfCylinders",
                schema: "vehicle",
                table: "Vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleUse",
                schema: "vehicle",
                table: "Vehicles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountableOfficerTitle",
                schema: "vehicle",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "AcquisitionCost",
                schema: "vehicle",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "ChassisNumber",
                schema: "vehicle",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "EngineDisplacementCC",
                schema: "vehicle",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "FuelType",
                schema: "vehicle",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "MotorNumber",
                schema: "vehicle",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "NumberOfCylinders",
                schema: "vehicle",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleUse",
                schema: "vehicle",
                table: "Vehicles");
        }
    }
}
