using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Vehicle
{
    /// <inheritdoc />
    public partial class VehicleDailyUsageAllowMultiplePerDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleDailyUsages_TenantId_VehicleId_Date",
                schema: "vehicle",
                table: "VehicleDailyUsages");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDailyUsages_TenantId_VehicleId_Date",
                schema: "vehicle",
                table: "VehicleDailyUsages",
                columns: new[] { "TenantId", "VehicleId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleDailyUsages_TenantId_VehicleId_Date",
                schema: "vehicle",
                table: "VehicleDailyUsages");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDailyUsages_TenantId_VehicleId_Date",
                schema: "vehicle",
                table: "VehicleDailyUsages",
                columns: new[] { "TenantId", "VehicleId", "Date" },
                unique: true);
        }
    }
}
