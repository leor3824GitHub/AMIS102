using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementAcquisition
{
    /// <inheritdoc />
    public partial class ProcurementAcquisition_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "procurement");

            migrationBuilder.CreateTable(
                name: "AssetIARs",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IarNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IarDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    InspectedById = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedById = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryReceiptNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SubmittedForInspectionOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InspectedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AcceptedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LineItems = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetIARs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CanvassRequests",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RivNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PurchaseRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnDeadline = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AwardedSupplierId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_CanvassRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IarNumberSequences",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    LastSerial = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IarNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrNumberSequences",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    LastSerial = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PoNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PoDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PurchaseRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanvassRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SupplierAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SupplierTin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ModeOfProcurement = table.Column<int>(type: "integer", nullable: false),
                    PlaceOfDelivery = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DateOfDelivery = table.Column<DateOnly>(type: "date", nullable: true),
                    DeliveryTerm = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PaymentTerm = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FundCluster = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OursBursNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LineItems = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequests",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PrNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PrDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SaiNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SaiDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AlobsNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AlobsDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponsibilityCenterCode = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PrType = table.Column<int>(type: "integer", nullable: false),
                    Justification = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ApprovedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LineItems = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CanvassQuotations",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CanvassRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SupplierAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TinNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    QuotationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DeliveryTerms = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsAwarded = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LineItems = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanvassQuotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CanvassQuotations_CanvassRequests_CanvassRequestId",
                        column: x => x.CanvassRequestId,
                        principalSchema: "procurement",
                        principalTable: "CanvassRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetIARs_CreatedOnUtc",
                schema: "procurement",
                table: "AssetIARs",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AssetIARs_TenantId_IarNumber",
                schema: "procurement",
                table: "AssetIARs",
                columns: new[] { "TenantId", "IarNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetIARs_TenantId_PurchaseOrderId",
                schema: "procurement",
                table: "AssetIARs",
                columns: new[] { "TenantId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetIARs_TenantId_Status",
                schema: "procurement",
                table: "AssetIARs",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CanvassQuotations_CanvassRequestId",
                schema: "procurement",
                table: "CanvassQuotations",
                column: "CanvassRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CanvassQuotations_SupplierId",
                schema: "procurement",
                table: "CanvassQuotations",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_CanvassRequests_PurchaseRequestId",
                schema: "procurement",
                table: "CanvassRequests",
                column: "PurchaseRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CanvassRequests_Status",
                schema: "procurement",
                table: "CanvassRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CanvassRequests_TenantId_RivNumber",
                schema: "procurement",
                table: "CanvassRequests",
                columns: new[] { "TenantId", "RivNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IarNumberSequences_TenantId_Year",
                schema: "procurement",
                table: "IarNumberSequences",
                columns: new[] { "TenantId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrNumberSequences_TenantId_Year",
                schema: "procurement",
                table: "PrNumberSequences",
                columns: new[] { "TenantId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PurchaseRequestId",
                schema: "procurement",
                table: "PurchaseOrders",
                column: "PurchaseRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Status",
                schema: "procurement",
                table: "PurchaseOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId_PoNumber",
                schema: "procurement",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "PoNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_DepartmentId",
                schema: "procurement",
                table: "PurchaseRequests",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_Status",
                schema: "procurement",
                table: "PurchaseRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_TenantId_PrNumber",
                schema: "procurement",
                table: "PurchaseRequests",
                columns: new[] { "TenantId", "PrNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetIARs",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "CanvassQuotations",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "IarNumberSequences",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "PrNumberSequences",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "PurchaseOrders",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "PurchaseRequests",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "CanvassRequests",
                schema: "procurement");
        }
    }
}
