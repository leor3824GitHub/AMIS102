using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Procurement
{
    /// <inheritdoc />
    public partial class AddAssetIARs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    Version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetIARs",
                schema: "procurement");
        }
    }
}
