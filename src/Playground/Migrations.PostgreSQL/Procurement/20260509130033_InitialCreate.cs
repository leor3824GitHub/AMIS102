using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Procurement
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "procurement");

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
                    Section = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PrType = table.Column<int>(type: "integer", nullable: false),
                    Justification = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
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
                name: "CanvassQuotations",
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

