using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class AssetRegister_AddReceivingReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceivingReports",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    DocumentKind = table.Column<int>(type: "integer", nullable: false),
                    ReportNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ReceivedFrom = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReceiptType = table.Column<int>(type: "integer", nullable: false),
                    OtherReceiptType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FundCluster = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ReceivedBy_EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedBy_PrintedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReceivedBy_Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NotedBy_EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    NotedBy_PrintedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NotedBy_Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DateReceived = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivingReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReceivingReportItems",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SerialNo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivingReportItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceivingReportItems_ReceivingReports_ReportId",
                        column: x => x.ReportId,
                        principalSchema: "asset_register",
                        principalTable: "ReceivingReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivingReportItems_CatalogItemId",
                schema: "asset_register",
                table: "ReceivingReportItems",
                column: "CatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivingReportItems_ReportId",
                schema: "asset_register",
                table: "ReceivingReportItems",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivingReports_TenantId_DocumentKind_Date",
                schema: "asset_register",
                table: "ReceivingReports",
                columns: new[] { "TenantId", "DocumentKind", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivingReports_TenantId_ReportNo",
                schema: "asset_register",
                table: "ReceivingReports",
                columns: new[] { "TenantId", "ReportNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceivingReportItems",
                schema: "asset_register");

            migrationBuilder.DropTable(
                name: "ReceivingReports",
                schema: "asset_register");
        }
    }
}

