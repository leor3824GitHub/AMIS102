using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Expendable
{
    /// <inheritdoc />
    public partial class Expendable_RemovePurchasePipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseInspection",
                schema: "expendable");

            migrationBuilder.DropTable(
                name: "Purchases",
                schema: "expendable");

            migrationBuilder.DropTable(
                name: "RejectedInventory",
                schema: "expendable");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurchaseInspection",
                schema: "expendable",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InspectedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityAccepted = table.Column<int>(type: "integer", nullable: false),
                    QuantityReceivedForInspection = table.Column<int>(type: "integer", nullable: false),
                    QuantityRejected = table.Column<int>(type: "integer", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WarehouseLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Defects = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseInspection", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Purchases",
                schema: "expendable",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveryDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpectedDeliveryDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OrderDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PurchaseOrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReceiptDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReceivingNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SupplierId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SupplierName = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    WarehouseLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseLocationName = table.Column<string>(type: "text", nullable: false),
                    LineItems = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RejectedInventory",
                schema: "expendable",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DispositionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DispositionNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseInspectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityRejected = table.Column<int>(type: "integer", nullable: false),
                    RejectionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    WarehouseLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseLocationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RejectedInventory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInspection_TenantId_PurchaseId",
                schema: "expendable",
                table: "PurchaseInspection",
                columns: new[] { "TenantId", "PurchaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInspection_TenantId_Status",
                schema: "expendable",
                table: "PurchaseInspection",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_TenantId_PurchaseOrderNumber",
                schema: "expendable",
                table: "Purchases",
                columns: new[] { "TenantId", "PurchaseOrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_TenantId_Status",
                schema: "expendable",
                table: "Purchases",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_TenantId_SupplierId",
                schema: "expendable",
                table: "Purchases",
                columns: new[] { "TenantId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_RejectedInventory_TenantId_PurchaseId",
                schema: "expendable",
                table: "RejectedInventory",
                columns: new[] { "TenantId", "PurchaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_RejectedInventory_TenantId_PurchaseInspectionId",
                schema: "expendable",
                table: "RejectedInventory",
                columns: new[] { "TenantId", "PurchaseInspectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RejectedInventory_TenantId_Status",
                schema: "expendable",
                table: "RejectedInventory",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RejectedInventory_TenantId_WarehouseLocationId",
                schema: "expendable",
                table: "RejectedInventory",
                columns: new[] { "TenantId", "WarehouseLocationId" });
        }
    }
}
