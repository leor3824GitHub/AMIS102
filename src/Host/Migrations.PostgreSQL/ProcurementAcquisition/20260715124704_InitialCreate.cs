using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementAcquisition
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
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    AwardSignatories = table.Column<string>(type: "jsonb", nullable: true),
                    LineItems = table.Column<string>(type: "jsonb", nullable: true)
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
                name: "InspectionAcceptanceReports",
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
                    Category = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SubmittedForInspectionOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InspectedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AcceptedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AcceptedById = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AcceptedByDesignation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    LineItems = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionAcceptanceReports", x => x.Id);
                });

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
                    InspectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InspectorDesignation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InspectedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InspectionInvoiceNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    InspectionInvoiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DateInspected = table.Column<DateOnly>(type: "date", nullable: true),
                    InspectionFindings = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FoundInOrder = table.Column<bool>(type: "boolean", nullable: false),
                    AcceptedById = table.Column<Guid>(type: "uuid", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "PoNumberSequences",
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
                    table.PrimaryKey("PK_PoNumberSequences", x => x.Id);
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
                    OursBursDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FundsAvailableCertifiedById = table.Column<Guid>(type: "uuid", nullable: true),
                    FundsAvailableCertifiedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FundsAvailableCertifiedByDesignation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FundsAvailableCertifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IssuedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IssuedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IssuedByDesignation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IssuedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    Category = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Justification = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestedByDesignation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FundsAvailableCertifiedById = table.Column<Guid>(type: "uuid", nullable: true),
                    FundsAvailableCertifiedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FundsAvailableCertifiedByDesignation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FundsAvailableCertifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApprovedByDesignation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReturnedReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReturnedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReturnedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReturnedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_PurchaseRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RivNumberSequences",
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
                    table.PrimaryKey("PK_RivNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignedDocuments",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UploadedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_SignedDocuments", x => x.Id);
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
                name: "IX_CanvassRequests_TenantId_RivNumber",
                schema: "procurement",
                table: "CanvassRequests",
                columns: new[] { "TenantId", "RivNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CanvassRequests_TenantId_Status",
                schema: "procurement",
                table: "CanvassRequests",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_IarNumberSequences_TenantId_Year",
                schema: "procurement",
                table: "IarNumberSequences",
                columns: new[] { "TenantId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InspectionAcceptanceReports_CreatedOnUtc",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionAcceptanceReports_TenantId_IarNumber",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                columns: new[] { "TenantId", "IarNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InspectionAcceptanceReports_TenantId_PurchaseOrderId",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                columns: new[] { "TenantId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_InspectionAcceptanceReports_TenantId_Status",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_PurchaseRequestId",
                schema: "procurement",
                table: "JobOrders",
                column: "PurchaseRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_TenantId_JoNumber",
                schema: "procurement",
                table: "JobOrders",
                columns: new[] { "TenantId", "JoNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_TenantId_Status",
                schema: "procurement",
                table: "JobOrders",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_JoNumberSequences_TenantId_Year_Month",
                schema: "procurement",
                table: "JoNumberSequences",
                columns: new[] { "TenantId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoNumberSequences_TenantId_Year_Month",
                schema: "procurement",
                table: "PoNumberSequences",
                columns: new[] { "TenantId", "Year", "Month" },
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
                name: "IX_PurchaseOrders_TenantId_PoNumber",
                schema: "procurement",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "PoNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId_Status",
                schema: "procurement",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_TenantId_DepartmentId",
                schema: "procurement",
                table: "PurchaseRequests",
                columns: new[] { "TenantId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_TenantId_PrNumber",
                schema: "procurement",
                table: "PurchaseRequests",
                columns: new[] { "TenantId", "PrNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_TenantId_Status",
                schema: "procurement",
                table: "PurchaseRequests",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RivNumberSequences_TenantId_Year",
                schema: "procurement",
                table: "RivNumberSequences",
                columns: new[] { "TenantId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignedDocuments_TenantId_DocumentType_DocumentId",
                schema: "procurement",
                table: "SignedDocuments",
                columns: new[] { "TenantId", "DocumentType", "DocumentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CanvassQuotations",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "IarNumberSequences",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "InspectionAcceptanceReports",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "JobOrders",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "JoNumberSequences",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "PoNumberSequences",
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
                name: "RivNumberSequences",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "SignedDocuments",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "CanvassRequests",
                schema: "procurement");
        }
    }
}
