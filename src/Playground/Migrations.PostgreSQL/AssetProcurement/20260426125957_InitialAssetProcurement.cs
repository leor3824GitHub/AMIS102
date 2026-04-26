using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.AssetProcurement
{
    /// <inheritdoc />
    public partial class InitialAssetProcurement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "asset_procurement");

            migrationBuilder.CreateTable(
                name: "AssetIARs",
                schema: "asset_procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
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
                name: "AssetPurchaseOrders",
                schema: "asset_procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PoNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PoDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PurchaseRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SupplierAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SupplierTin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ModeOfProcurement = table.Column<int>(type: "integer", nullable: false),
                    PlaceOfDelivery = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DateOfDelivery = table.Column<DateOnly>(type: "date", nullable: true),
                    DeliveryTerm = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PaymentTerm = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FundCluster = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    OblRequestNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("PK_AssetPurchaseOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetPurchaseRequests",
                schema: "asset_procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PrDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SaiNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SaiDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AlobsNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AlobsDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Section = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PrType = table.Column<int>(type: "integer", nullable: false),
                    Justification = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("PK_AssetPurchaseRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetIARs_CreatedOnUtc",
                schema: "asset_procurement",
                table: "AssetIARs",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AssetIARs_IarNumber",
                schema: "asset_procurement",
                table: "AssetIARs",
                column: "IarNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetIARs_PurchaseOrderId",
                schema: "asset_procurement",
                table: "AssetIARs",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetIARs_Status",
                schema: "asset_procurement",
                table: "AssetIARs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AssetPurchaseOrders_CreatedOnUtc",
                schema: "asset_procurement",
                table: "AssetPurchaseOrders",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AssetPurchaseOrders_PoNumber",
                schema: "asset_procurement",
                table: "AssetPurchaseOrders",
                column: "PoNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetPurchaseOrders_PurchaseRequestId",
                schema: "asset_procurement",
                table: "AssetPurchaseOrders",
                column: "PurchaseRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetPurchaseOrders_Status",
                schema: "asset_procurement",
                table: "AssetPurchaseOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AssetPurchaseRequests_CreatedOnUtc",
                schema: "asset_procurement",
                table: "AssetPurchaseRequests",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AssetPurchaseRequests_DepartmentId",
                schema: "asset_procurement",
                table: "AssetPurchaseRequests",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetPurchaseRequests_PrNumber",
                schema: "asset_procurement",
                table: "AssetPurchaseRequests",
                column: "PrNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetPurchaseRequests_Status",
                schema: "asset_procurement",
                table: "AssetPurchaseRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetIARs",
                schema: "asset_procurement");

            migrationBuilder.DropTable(
                name: "AssetPurchaseOrders",
                schema: "asset_procurement");

            migrationBuilder.DropTable(
                name: "AssetPurchaseRequests",
                schema: "asset_procurement");
        }
    }
}
