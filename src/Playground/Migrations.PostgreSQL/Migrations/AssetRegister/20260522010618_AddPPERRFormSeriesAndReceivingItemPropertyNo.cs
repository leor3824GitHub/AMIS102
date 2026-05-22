using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Migrations.AssetRegister
{
    /// <inheritdoc />
    public partial class AddPPERRFormSeriesAndReceivingItemPropertyNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PropertyNo",
                schema: "asset_register",
                table: "ReceivingReportItems",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PPERRFormSeries",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartSerial = table.Column<int>(type: "integer", nullable: false),
                    EndSerial = table.Column<int>(type: "integer", nullable: false),
                    NextSerial = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PPERRFormSeries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PPERRFormSeries_TenantId_IsActive",
                schema: "asset_register",
                table: "PPERRFormSeries",
                columns: new[] { "TenantId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PPERRFormSeries",
                schema: "asset_register");

            migrationBuilder.DropColumn(
                name: "PropertyNo",
                schema: "asset_register",
                table: "ReceivingReportItems");
        }
    }
}
