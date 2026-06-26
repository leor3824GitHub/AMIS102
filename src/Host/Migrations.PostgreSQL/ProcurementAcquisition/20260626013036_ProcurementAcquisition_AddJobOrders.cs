using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementAcquisition
{
    /// <inheritdoc />
    public partial class ProcurementAcquisition_AddJobOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobOrders",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    JoNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    JoDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PurchaseRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    JobRequestNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RequisitioningOffice = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
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
                    OursBursDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FundsAvailableCertifiedById = table.Column<Guid>(type: "uuid", nullable: true),
                    FundsAvailableCertifiedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FundsAvailableCertifiedByDesignation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FundsAvailableCertifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IssuedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IssuedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IssuedByDesignation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IssuedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InspectedById = table.Column<Guid>(type: "uuid", nullable: true),
                    InspectedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InspectedByDesignation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InspectedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InspectionInvoiceNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    InspectionInvoiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DateInspected = table.Column<DateOnly>(type: "date", nullable: true),
                    InspectionFindings = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FoundInOrder = table.Column<bool>(type: "boolean", nullable: false),
                    AcceptedById = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AcceptedByDesignation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AcceptedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AcceptanceInvoiceNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DateReceived = table.Column<DateOnly>(type: "date", nullable: true),
                    IsCompleteDelivery = table.Column<bool>(type: "boolean", nullable: false),
                    PartialDeliveryNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    LineItems = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JoNumberSequences",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    LastSerial = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JoNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_PurchaseRequestId",
                schema: "procurement",
                table: "JobOrders",
                column: "PurchaseRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_Status",
                schema: "procurement",
                table: "JobOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_TenantId_JoNumber",
                schema: "procurement",
                table: "JobOrders",
                columns: new[] { "TenantId", "JoNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JoNumberSequences_TenantId_Year_Month",
                schema: "procurement",
                table: "JoNumberSequences",
                columns: new[] { "TenantId", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobOrders",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "JoNumberSequences",
                schema: "procurement");
        }
    }
}
