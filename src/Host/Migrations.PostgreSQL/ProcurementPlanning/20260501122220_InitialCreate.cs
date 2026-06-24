using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementPlanning
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "procurement_planning");

            migrationBuilder.CreateTable(
                name: "AnnualProcurementPlans",
                schema: "procurement_planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FiscalYear = table.Column<int>(type: "integer", nullable: false),
                    Phase = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    IsCurrentVersion = table.Column<bool>(type: "boolean", nullable: false),
                    VersionChainId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AmendmentReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AmendedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AmendedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsolidatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsolidatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReturnReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReturnedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReturnedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnualProcurementPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ppmps",
                schema: "procurement_planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PpmpNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FiscalYear = table.Column<int>(type: "integer", nullable: false),
                    Phase = table.Column<int>(type: "integer", nullable: false),
                    OfficeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EndUserUnit = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    IsCurrentVersion = table.Column<bool>(type: "boolean", nullable: false),
                    VersionChainId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AmendmentReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AmendedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AmendedById = table.Column<Guid>(type: "uuid", nullable: true),
                    PreparedById = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReturnReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReturnedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReturnedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ppmps", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "PpmpItems",
                schema: "procurement_planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PpmpId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PpmpItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PpmpItems_Ppmps_PpmpId",
                        column: x => x.PpmpId,
                        principalSchema: "procurement_planning",
                        principalTable: "Ppmps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnnualProcurementPlans_AppNumber",
                schema: "procurement_planning",
                table: "AnnualProcurementPlans",
                column: "AppNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AnnualProcurementPlans_FiscalYear_IsCurrentVersion",
                schema: "procurement_planning",
                table: "AnnualProcurementPlans",
                columns: new[] { "FiscalYear", "IsCurrentVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_AnnualProcurementPlans_VersionChainId",
                schema: "procurement_planning",
                table: "AnnualProcurementPlans",
                column: "VersionChainId");

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

            migrationBuilder.CreateIndex(
                name: "IX_PpmpItems_PpmpId",
                schema: "procurement_planning",
                table: "PpmpItems",
                column: "PpmpId");

            migrationBuilder.CreateIndex(
                name: "IX_Ppmps_FiscalYear_OfficeCode_IsCurrentVersion",
                schema: "procurement_planning",
                table: "Ppmps",
                columns: new[] { "FiscalYear", "OfficeCode", "IsCurrentVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_Ppmps_PpmpNumber",
                schema: "procurement_planning",
                table: "Ppmps",
                column: "PpmpNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Ppmps_VersionChainId",
                schema: "procurement_planning",
                table: "Ppmps",
                column: "VersionChainId");
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

            migrationBuilder.DropTable(
                name: "PpmpItems",
                schema: "procurement_planning");

            migrationBuilder.DropTable(
                name: "AnnualProcurementPlans",
                schema: "procurement_planning");

            migrationBuilder.DropTable(
                name: "Ppmps",
                schema: "procurement_planning");
        }
    }
}

