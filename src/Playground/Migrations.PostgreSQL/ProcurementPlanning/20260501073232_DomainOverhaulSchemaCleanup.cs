using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.ProcurementPlanning
{
    /// <inheritdoc />
    public partial class DomainOverhaulSchemaCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppLineReferences",
                schema: "procurement_planning");

            migrationBuilder.DropTable(
                name: "AppSnapshotItems",
                schema: "procurement_planning");

            migrationBuilder.DropTable(
                name: "AppSnapshots",
                schema: "procurement_planning");

            migrationBuilder.DropColumn(
                name: "AppId",
                schema: "procurement_planning",
                table: "Ppmps");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "procurement_planning",
                table: "Ppmps");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "procurement_planning",
                table: "AnnualProcurementPlans");

            migrationBuilder.CreateTable(
                name: "AppLineItems",
                schema: "procurement_planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePpmpId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePpmpItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePpmpNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OfficeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EndUserUnit = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    GeneralDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ProjectType = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Unit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModeOfProcurement = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PreProcurementConference = table.Column<bool>(type: "boolean", nullable: false),
                    ProcurementStart = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ProcurementEnd = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExpectedDelivery = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SourceOfFunds = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EstimatedBudget = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SupportingDocuments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConsolidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppLineItems_AnnualProcurementPlans_AppId",
                        column: x => x.AppId,
                        principalSchema: "procurement_planning",
                        principalTable: "AnnualProcurementPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppSourcePpmps",
                schema: "procurement_planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppId = table.Column<Guid>(type: "uuid", nullable: false),
                    PpmpId = table.Column<Guid>(type: "uuid", nullable: false),
                    PpmpNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OfficeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EndUserUnit = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phase = table.Column<int>(type: "integer", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    IncludedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IncludedById = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSourcePpmps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSourcePpmps_AnnualProcurementPlans_AppId",
                        column: x => x.AppId,
                        principalSchema: "procurement_planning",
                        principalTable: "AnnualProcurementPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppLineItems_AppId",
                schema: "procurement_planning",
                table: "AppLineItems",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLineItems_AppId_SourcePpmpItemId",
                schema: "procurement_planning",
                table: "AppLineItems",
                columns: new[] { "AppId", "SourcePpmpItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppLineItems_SourcePpmpId",
                schema: "procurement_planning",
                table: "AppLineItems",
                column: "SourcePpmpId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLineItems_SourcePpmpItemId",
                schema: "procurement_planning",
                table: "AppLineItems",
                column: "SourcePpmpItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSourcePpmps_AppId",
                schema: "procurement_planning",
                table: "AppSourcePpmps",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSourcePpmps_AppId_PpmpId",
                schema: "procurement_planning",
                table: "AppSourcePpmps",
                columns: new[] { "AppId", "PpmpId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSourcePpmps_PpmpId",
                schema: "procurement_planning",
                table: "AppSourcePpmps",
                column: "PpmpId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppLineItems",
                schema: "procurement_planning");

            migrationBuilder.DropTable(
                name: "AppSourcePpmps",
                schema: "procurement_planning");

            migrationBuilder.AddColumn<Guid>(
                name: "AppId",
                schema: "procurement_planning",
                table: "Ppmps",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "procurement_planning",
                table: "Ppmps",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "procurement_planning",
                table: "AnnualProcurementPlans",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "AppLineReferences",
                schema: "procurement_planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    SourcePpmpId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePpmpItemId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLineReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppLineReferences_AnnualProcurementPlans_AppId",
                        column: x => x.AppId,
                        principalSchema: "procurement_planning",
                        principalTable: "AnnualProcurementPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppLineReferences_PpmpItems_SourcePpmpItemId",
                        column: x => x.SourcePpmpItemId,
                        principalSchema: "procurement_planning",
                        principalTable: "PpmpItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppLineReferences_Ppmps_SourcePpmpId",
                        column: x => x.SourcePpmpId,
                        principalSchema: "procurement_planning",
                        principalTable: "Ppmps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppSnapshots",
                schema: "procurement_planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CapturedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CapturedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FiscalYear = table.Column<int>(type: "integer", nullable: false),
                    Phase = table.Column<int>(type: "integer", nullable: false),
                    SnapshotType = table.Column<int>(type: "integer", nullable: false),
                    StatusAtCapture = table.Column<int>(type: "integer", nullable: false),
                    TotalEstimatedBudget = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VersionChainId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSnapshots_AnnualProcurementPlans_AppId",
                        column: x => x.AppId,
                        principalSchema: "procurement_planning",
                        principalTable: "AnnualProcurementPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppSnapshotItems",
                schema: "procurement_planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndUserUnit = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EstimatedBudget = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExpectedDelivery = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    GeneralDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    ModeOfProcurement = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OfficeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PreProcurementConference = table.Column<bool>(type: "boolean", nullable: false),
                    ProcurementEnd = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ProcurementStart = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ProjectType = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SourceOfFunds = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SourcePpmpId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePpmpItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Unit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSnapshotItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSnapshotItems_AppSnapshots_AppSnapshotId",
                        column: x => x.AppSnapshotId,
                        principalSchema: "procurement_planning",
                        principalTable: "AppSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppLineReferences_AppId",
                schema: "procurement_planning",
                table: "AppLineReferences",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLineReferences_AppId_SourcePpmpItemId",
                schema: "procurement_planning",
                table: "AppLineReferences",
                columns: new[] { "AppId", "SourcePpmpItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppLineReferences_SourcePpmpId",
                schema: "procurement_planning",
                table: "AppLineReferences",
                column: "SourcePpmpId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLineReferences_SourcePpmpItemId",
                schema: "procurement_planning",
                table: "AppLineReferences",
                column: "SourcePpmpItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSnapshotItems_AppSnapshotId",
                schema: "procurement_planning",
                table: "AppSnapshotItems",
                column: "AppSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSnapshotItems_SourcePpmpId",
                schema: "procurement_planning",
                table: "AppSnapshotItems",
                column: "SourcePpmpId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSnapshotItems_SourcePpmpItemId",
                schema: "procurement_planning",
                table: "AppSnapshotItems",
                column: "SourcePpmpItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSnapshots_AppId",
                schema: "procurement_planning",
                table: "AppSnapshots",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSnapshots_AppId_VersionNumber_SnapshotType",
                schema: "procurement_planning",
                table: "AppSnapshots",
                columns: new[] { "AppId", "VersionNumber", "SnapshotType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSnapshots_VersionChainId",
                schema: "procurement_planning",
                table: "AppSnapshots",
                column: "VersionChainId");
        }
    }
}
