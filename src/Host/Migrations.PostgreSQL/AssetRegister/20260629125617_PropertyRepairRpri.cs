using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class PropertyRepairRpri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PropertyRepairs",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssetRegistryId = table.Column<Guid>(type: "uuid", nullable: false),
                    RpriNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    NatureOfWork = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PartsToReplace = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RequestedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EngineNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ChassisNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OdometerReading = table.Column<int>(type: "integer", nullable: true),
                    NatureOfLastRepair = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DateOfLastRepair = table.Column<DateOnly>(type: "date", nullable: true),
                    PreInspectionFindings = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PreInspectedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NotedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PreInspectedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    RepairShop = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    JobOrderNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    InvoiceNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AmountPerJO = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PostInspectionFindings = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PostInspectedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PostInspectedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    PrNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PoJoNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BurNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DvNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AcceptedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AcceptedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyRepairs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyRepairs_TenantId_AssetRegistryId",
                schema: "asset_register",
                table: "PropertyRepairs",
                columns: new[] { "TenantId", "AssetRegistryId" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyRepairs_TenantId_RpriNo",
                schema: "asset_register",
                table: "PropertyRepairs",
                columns: new[] { "TenantId", "RpriNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyRepairs_TenantId_Status",
                schema: "asset_register",
                table: "PropertyRepairs",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropertyRepairs",
                schema: "asset_register");
        }
    }
}
