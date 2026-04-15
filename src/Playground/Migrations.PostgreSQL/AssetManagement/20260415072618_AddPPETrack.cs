using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.AssetManagement
{
    /// <inheritdoc />
    public partial class AddPPETrack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PARItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PARId = table.Column<Guid>(type: "uuid", nullable: false),
                    PPEItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ItemDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EstimatedUsefulLifeYears = table.Column<int>(type: "integer", nullable: false),
                    DateAcquired = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PARItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PPEIRItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PPEIRId = table.Column<Guid>(type: "uuid", nullable: false),
                    PPEItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    PropertyCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PPESpecification = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DateAcquired = table.Column<DateOnly>(type: "date", nullable: false),
                    AcquisitionCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AccumulatedDepreciation = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    BookValue = table.Column<decimal>(type: "numeric(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PPEIRItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PPEIssuanceReports",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PPEIRNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    IssuedToEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedToOfficeAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IssuanceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IssuedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateReceived = table.Column<DateOnly>(type: "date", nullable: true),
                    ApprovedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillOfLadingNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PPEIssuanceReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PPEItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PropertyNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DateAcquired = table.Column<DateOnly>(type: "date", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EstimatedUsefulLifeYears = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CurrentAccountableEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourcePPERRId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PPEItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PPEReceivingReports",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PPERRNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ReceivedFrom = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReceiptNature = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReceivedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PPEReceivingReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PPERRItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PPERRId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    PropertyCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DateAcquired = table.Column<DateOnly>(type: "date", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PPERRItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyAcknowledgementReceipts",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PARNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    PARType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReceivedFromEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyAcknowledgementReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptsForReturnedPPE",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RRPNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ReturnCategory = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReturnedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyInspectorCertified = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptsForReturnedPPE", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RRPItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RRPId = table.Column<Guid>(type: "uuid", nullable: false),
                    PPEItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    SourceDocumentRef = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PropertyCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RRPItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PARItems_PARId",
                schema: "am",
                table: "PARItems",
                column: "PARId");

            migrationBuilder.CreateIndex(
                name: "IX_PARItems_PPEItemId",
                schema: "am",
                table: "PARItems",
                column: "PPEItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PPEIRItems_PPEIRId",
                schema: "am",
                table: "PPEIRItems",
                column: "PPEIRId");

            migrationBuilder.CreateIndex(
                name: "IX_PPEIRItems_PPEItemId",
                schema: "am",
                table: "PPEIRItems",
                column: "PPEItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PPEIssuanceReports_Date",
                schema: "am",
                table: "PPEIssuanceReports",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_PPEIssuanceReports_IssuanceType",
                schema: "am",
                table: "PPEIssuanceReports",
                column: "IssuanceType");

            migrationBuilder.CreateIndex(
                name: "IX_PPEIssuanceReports_IssuedToEmployeeId",
                schema: "am",
                table: "PPEIssuanceReports",
                column: "IssuedToEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PPEIssuanceReports_PPEIRNo",
                schema: "am",
                table: "PPEIssuanceReports",
                column: "PPEIRNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PPEItems_CurrentAccountableEmployeeId",
                schema: "am",
                table: "PPEItems",
                column: "CurrentAccountableEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PPEItems_PropertyCode",
                schema: "am",
                table: "PPEItems",
                column: "PropertyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PPEItems_PropertyNumber",
                schema: "am",
                table: "PPEItems",
                column: "PropertyNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PPEItems_SourcePPERRId",
                schema: "am",
                table: "PPEItems",
                column: "SourcePPERRId");

            migrationBuilder.CreateIndex(
                name: "IX_PPEItems_Status",
                schema: "am",
                table: "PPEItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PPEReceivingReports_Date",
                schema: "am",
                table: "PPEReceivingReports",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_PPEReceivingReports_PPERRNo",
                schema: "am",
                table: "PPEReceivingReports",
                column: "PPERRNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PPEReceivingReports_ReceivedByEmployeeId",
                schema: "am",
                table: "PPEReceivingReports",
                column: "ReceivedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PPERRItems_PPERRId",
                schema: "am",
                table: "PPERRItems",
                column: "PPERRId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyAcknowledgementReceipts_Date",
                schema: "am",
                table: "PropertyAcknowledgementReceipts",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyAcknowledgementReceipts_PARNo",
                schema: "am",
                table: "PropertyAcknowledgementReceipts",
                column: "PARNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyAcknowledgementReceipts_PARType",
                schema: "am",
                table: "PropertyAcknowledgementReceipts",
                column: "PARType");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyAcknowledgementReceipts_ReceivedByEmployeeId",
                schema: "am",
                table: "PropertyAcknowledgementReceipts",
                column: "ReceivedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptsForReturnedPPE_Date",
                schema: "am",
                table: "ReceiptsForReturnedPPE",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptsForReturnedPPE_ReturnCategory",
                schema: "am",
                table: "ReceiptsForReturnedPPE",
                column: "ReturnCategory");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptsForReturnedPPE_ReturnedByEmployeeId",
                schema: "am",
                table: "ReceiptsForReturnedPPE",
                column: "ReturnedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptsForReturnedPPE_RRPNo",
                schema: "am",
                table: "ReceiptsForReturnedPPE",
                column: "RRPNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RRPItems_PPEItemId",
                schema: "am",
                table: "RRPItems",
                column: "PPEItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RRPItems_RRPId",
                schema: "am",
                table: "RRPItems",
                column: "RRPId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PARItems",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PPEIRItems",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PPEIssuanceReports",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PPEItems",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PPEReceivingReports",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PPERRItems",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PropertyAcknowledgementReceipts",
                schema: "am");

            migrationBuilder.DropTable(
                name: "ReceiptsForReturnedPPE",
                schema: "am");

            migrationBuilder.DropTable(
                name: "RRPItems",
                schema: "am");
        }
    }
}
