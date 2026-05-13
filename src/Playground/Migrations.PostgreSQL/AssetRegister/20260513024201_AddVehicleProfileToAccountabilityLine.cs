using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class AddVehicleProfileToAccountabilityLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "vehicle_chassis_number",
                schema: "asset_register",
                table: "PropertyAccountabilityLines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vehicle_engine_number",
                schema: "asset_register",
                table: "PropertyAccountabilityLines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "vehicle_odometer_at_issue",
                schema: "asset_register",
                table: "PropertyAccountabilityLines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "vehicle_odometer_at_return",
                schema: "asset_register",
                table: "PropertyAccountabilityLines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vehicle_plate_number",
                schema: "asset_register",
                table: "PropertyAccountabilityLines",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "vehicle_chassis_number",
                schema: "asset_register",
                table: "PropertyAccountabilityLines");

            migrationBuilder.DropColumn(
                name: "vehicle_engine_number",
                schema: "asset_register",
                table: "PropertyAccountabilityLines");

            migrationBuilder.DropColumn(
                name: "vehicle_odometer_at_issue",
                schema: "asset_register",
                table: "PropertyAccountabilityLines");

            migrationBuilder.DropColumn(
                name: "vehicle_odometer_at_return",
                schema: "asset_register",
                table: "PropertyAccountabilityLines");

            migrationBuilder.DropColumn(
                name: "vehicle_plate_number",
                schema: "asset_register",
                table: "PropertyAccountabilityLines");
        }
    }
}
