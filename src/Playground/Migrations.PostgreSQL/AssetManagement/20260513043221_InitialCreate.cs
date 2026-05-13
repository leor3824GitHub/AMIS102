using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetManagement
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "am");

            migrationBuilder.CreateTable(
                name: "InventoryCustodianSlips",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ICSNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    FundCluster = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IssuedFromEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceivedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetType = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    RenewedFromICSId = table.Column<Guid>(type: "uuid", nullable: true),
                    RenewedByICSId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelledByRRSPId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_InventoryCustodianSlips", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ParentLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_Locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Locations_Locations_ParentLocationId",
                        column: x => x.ParentLocationId,
                        principalSchema: "am",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PARItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PARId = table.Column<Guid>(type: "uuid", nullable: false),
                    TangibleInventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
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
                name: "PhysicalCountSessions",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SessionNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CountDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StationOffice = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PreparedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertifiedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_PhysicalCountSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PPEIRItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PPEIRId = table.Column<Guid>(type: "uuid", nullable: false),
                    TangibleInventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
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
                name: "PropertyAcknowledgementReceipts",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
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
                name: "PropertyCodeCounters",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClassCode = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    ItemCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    LastSequence = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyCodeCounters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyIncidentItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    TangibleInventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AssetTypeAtTimeOfReport = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyIncidentItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyIncidentReports",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReportNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    IncidentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IncidentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FundCluster = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AccountableEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    IncidentDetails = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_PropertyIncidentReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyItemCatalog",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UACSObjectCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EstimatedUsefulLifeYears = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_PropertyItemCatalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptForReturnedProperties",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RRSPNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ICSId = table.Column<Guid>(type: "uuid", nullable: false),
                    FundCluster = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReceivedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReturnedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_ReceiptForReturnedProperties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptsForReturnedPPE",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
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
                name: "ReclassificationRecords",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ThresholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalReclassified = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_ReclassificationRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RRPItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RRPId = table.Column<Guid>(type: "uuid", nullable: false),
                    TangibleInventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "RRSPItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RRSPId = table.Column<Guid>(type: "uuid", nullable: false),
                    TangibleInventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AssetTypeAtTimeOfReturn = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RRSPItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SemiExpendableIssuanceRecords",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SMIRNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    FundCluster = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IssuanceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TransferredToTenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TransferredToOfficerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IssuedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_SemiExpendableIssuanceRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SMIRItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SMIRId = table.Column<Guid>(type: "uuid", nullable: false),
                    TangibleInventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AssetTypeAtTimeOfIssuance = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMIRItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TangibleInventories",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReportNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ReceivedFrom = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReceiptType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OtherReceiptType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FundCluster = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReceivedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    NotedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_TangibleInventories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnserviceablePropertyItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    TangibleInventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AssetTypeAtTimeOfReport = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ConditionRemarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnserviceablePropertyItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnserviceablePropertyReports",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReportNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    DisposalMethod = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FundCluster = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InspectedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_UnserviceablePropertyReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TangibleItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PropertyClass = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CategoryCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TangibleInventoryItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_TangibleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TangibleItems_PropertyItemCatalog_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "am",
                        principalTable: "PropertyItemCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TangibleInventoryItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TangibleInventoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TangibleItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AssetType = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ThresholdAmountUsed = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsIssued = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PropertyNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TangibleInventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TangibleInventoryItems_PropertyItemCatalog_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "am",
                        principalTable: "PropertyItemCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TangibleInventoryItems_TangibleInventories_TangibleInventor~",
                        column: x => x.TangibleInventoryId,
                        principalSchema: "am",
                        principalTable: "TangibleInventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TangibleInventoryItems_TangibleItems_TangibleItemId",
                        column: x => x.TangibleItemId,
                        principalSchema: "am",
                        principalTable: "TangibleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetRegistry",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TangibleInventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AssetType = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LifecycleState = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CurrentPropertyStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CurrentCustodianId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentAssignmentHistoryId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_AssetRegistry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetRegistry_Locations_CurrentLocationId",
                        column: x => x.CurrentLocationId,
                        principalSchema: "am",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AssetRegistry_PropertyItemCatalog_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "am",
                        principalTable: "PropertyItemCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetRegistry_TangibleInventoryItems_TangibleInventoryItemId",
                        column: x => x.TangibleInventoryItemId,
                        principalSchema: "am",
                        principalTable: "TangibleInventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ICSItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ICSId = table.Column<Guid>(type: "uuid", nullable: false),
                    TangibleInventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EstimatedUsefulLifeYears = table.Column<int>(type: "integer", nullable: true),
                    AssetTypeAtTimeOfIssuance = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ICSItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ICSItems_InventoryCustodianSlips_ICSId",
                        column: x => x.ICSId,
                        principalSchema: "am",
                        principalTable: "InventoryCustodianSlips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ICSItems_TangibleInventoryItems_TangibleInventoryItemId",
                        column: x => x.TangibleInventoryItemId,
                        principalSchema: "am",
                        principalTable: "TangibleInventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhysicalCountEntries",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TangibleInventoryItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    PropertyNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Result = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Condition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QuantityOnHand = table.Column<int>(type: "integer", nullable: false),
                    ScannedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PhotoPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalCountEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhysicalCountEntries_PhysicalCountSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "am",
                        principalTable: "PhysicalCountSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhysicalCountEntries_TangibleInventoryItems_TangibleInvento~",
                        column: x => x.TangibleInventoryItemId,
                        principalSchema: "am",
                        principalTable: "TangibleInventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetAssignmentHistory",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssetRegistryId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OccurredOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceDocumentType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDocumentNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FromCustodianId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToCustodianId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetAssignmentHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetAssignmentHistory_AssetRegistry_AssetRegistryId",
                        column: x => x.AssetRegistryId,
                        principalSchema: "am",
                        principalTable: "AssetRegistry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetAssignmentHistory_Locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "am",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignmentHistory_AssetRegistryId",
                schema: "am",
                table: "AssetAssignmentHistory",
                column: "AssetRegistryId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignmentHistory_LocationId",
                schema: "am",
                table: "AssetAssignmentHistory",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignmentHistory_TenantId_AssetRegistryId_OccurredOnU~",
                schema: "am",
                table: "AssetAssignmentHistory",
                columns: new[] { "TenantId", "AssetRegistryId", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignmentHistory_TenantId_SourceDocumentType_SourceDo~",
                schema: "am",
                table: "AssetAssignmentHistory",
                columns: new[] { "TenantId", "SourceDocumentType", "SourceDocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignmentHistory_TenantId_ToCustodianId",
                schema: "am",
                table: "AssetAssignmentHistory",
                columns: new[] { "TenantId", "ToCustodianId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetRegistry_CurrentLocationId",
                schema: "am",
                table: "AssetRegistry",
                column: "CurrentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetRegistry_ItemId",
                schema: "am",
                table: "AssetRegistry",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetRegistry_TangibleInventoryItemId",
                schema: "am",
                table: "AssetRegistry",
                column: "TangibleInventoryItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetRegistry_TenantId_CurrentCustodianId",
                schema: "am",
                table: "AssetRegistry",
                columns: new[] { "TenantId", "CurrentCustodianId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetRegistry_TenantId_CurrentLocationId",
                schema: "am",
                table: "AssetRegistry",
                columns: new[] { "TenantId", "CurrentLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetRegistry_TenantId_LifecycleState",
                schema: "am",
                table: "AssetRegistry",
                columns: new[] { "TenantId", "LifecycleState" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetRegistry_TenantId_PropertyNo",
                schema: "am",
                table: "AssetRegistry",
                columns: new[] { "TenantId", "PropertyNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ICSItems_ICSId",
                schema: "am",
                table: "ICSItems",
                column: "ICSId");

            migrationBuilder.CreateIndex(
                name: "IX_ICSItems_TangibleInventoryItemId",
                schema: "am",
                table: "ICSItems",
                column: "TangibleInventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ICSItems_TenantId",
                schema: "am",
                table: "ICSItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ICSItems_TenantId_ICSId",
                schema: "am",
                table: "ICSItems",
                columns: new[] { "TenantId", "ICSId" });

            migrationBuilder.CreateIndex(
                name: "IX_ICSItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "ICSItems",
                columns: new[] { "TenantId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCustodianSlips_AssetType",
                schema: "am",
                table: "InventoryCustodianSlips",
                column: "AssetType");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCustodianSlips_CancelledByRRSPId",
                schema: "am",
                table: "InventoryCustodianSlips",
                column: "CancelledByRRSPId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCustodianSlips_Date",
                schema: "am",
                table: "InventoryCustodianSlips",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCustodianSlips_ExpiresOn",
                schema: "am",
                table: "InventoryCustodianSlips",
                column: "ExpiresOn");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCustodianSlips_IssuedFromEmployeeId",
                schema: "am",
                table: "InventoryCustodianSlips",
                column: "IssuedFromEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCustodianSlips_ReceivedByEmployeeId",
                schema: "am",
                table: "InventoryCustodianSlips",
                column: "ReceivedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCustodianSlips_RenewedByICSId",
                schema: "am",
                table: "InventoryCustodianSlips",
                column: "RenewedByICSId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCustodianSlips_RenewedFromICSId",
                schema: "am",
                table: "InventoryCustodianSlips",
                column: "RenewedFromICSId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCustodianSlips_Status",
                schema: "am",
                table: "InventoryCustodianSlips",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCustodianSlips_TenantId_ICSNo",
                schema: "am",
                table: "InventoryCustodianSlips",
                columns: new[] { "TenantId", "ICSNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_ParentLocationId",
                schema: "am",
                table: "Locations",
                column: "ParentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId_Code",
                schema: "am",
                table: "Locations",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId_Name",
                schema: "am",
                table: "Locations",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId_ParentLocationId",
                schema: "am",
                table: "Locations",
                columns: new[] { "TenantId", "ParentLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId_Type",
                schema: "am",
                table: "Locations",
                columns: new[] { "TenantId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_PARItems_PARId",
                schema: "am",
                table: "PARItems",
                column: "PARId");

            migrationBuilder.CreateIndex(
                name: "IX_PARItems_TangibleInventoryItemId",
                schema: "am",
                table: "PARItems",
                column: "TangibleInventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PARItems_TenantId",
                schema: "am",
                table: "PARItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PARItems_TenantId_PARId",
                schema: "am",
                table: "PARItems",
                columns: new[] { "TenantId", "PARId" });

            migrationBuilder.CreateIndex(
                name: "IX_PARItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "PARItems",
                columns: new[] { "TenantId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_Result",
                schema: "am",
                table: "PhysicalCountEntries",
                column: "Result");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_SessionId",
                schema: "am",
                table: "PhysicalCountEntries",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_SessionId_TangibleInventoryItemId",
                schema: "am",
                table: "PhysicalCountEntries",
                columns: new[] { "SessionId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_TangibleInventoryItemId",
                schema: "am",
                table: "PhysicalCountEntries",
                column: "TangibleInventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_TenantId",
                schema: "am",
                table: "PhysicalCountEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_TenantId_Result",
                schema: "am",
                table: "PhysicalCountEntries",
                columns: new[] { "TenantId", "Result" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_TenantId_SessionId",
                schema: "am",
                table: "PhysicalCountEntries",
                columns: new[] { "TenantId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_TenantId_SessionId_TangibleInventoryIt~",
                schema: "am",
                table: "PhysicalCountEntries",
                columns: new[] { "TenantId", "SessionId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "PhysicalCountEntries",
                columns: new[] { "TenantId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountSessions_CountDate",
                schema: "am",
                table: "PhysicalCountSessions",
                column: "CountDate");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountSessions_Status",
                schema: "am",
                table: "PhysicalCountSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountSessions_TenantId_SessionNo",
                schema: "am",
                table: "PhysicalCountSessions",
                columns: new[] { "TenantId", "SessionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PPEIRItems_PPEIRId",
                schema: "am",
                table: "PPEIRItems",
                column: "PPEIRId");

            migrationBuilder.CreateIndex(
                name: "IX_PPEIRItems_TangibleInventoryItemId",
                schema: "am",
                table: "PPEIRItems",
                column: "TangibleInventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PPEIRItems_TenantId",
                schema: "am",
                table: "PPEIRItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PPEIRItems_TenantId_PPEIRId",
                schema: "am",
                table: "PPEIRItems",
                columns: new[] { "TenantId", "PPEIRId" });

            migrationBuilder.CreateIndex(
                name: "IX_PPEIRItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "PPEIRItems",
                columns: new[] { "TenantId", "TangibleInventoryItemId" });

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
                name: "IX_PPEIssuanceReports_TenantId_PPEIRNo",
                schema: "am",
                table: "PPEIssuanceReports",
                columns: new[] { "TenantId", "PPEIRNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyAcknowledgementReceipts_Date",
                schema: "am",
                table: "PropertyAcknowledgementReceipts",
                column: "Date");

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
                name: "IX_PropertyAcknowledgementReceipts_TenantId_PARNo",
                schema: "am",
                table: "PropertyAcknowledgementReceipts",
                columns: new[] { "TenantId", "PARNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyCodeCounters_TenantId",
                schema: "am",
                table: "PropertyCodeCounters",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyCodeCounters_TenantId_ClassCode_ItemCode_Year",
                schema: "am",
                table: "PropertyCodeCounters",
                columns: new[] { "TenantId", "ClassCode", "ItemCode", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIncidentItems_ReportId",
                schema: "am",
                table: "PropertyIncidentItems",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIncidentItems_TangibleInventoryItemId",
                schema: "am",
                table: "PropertyIncidentItems",
                column: "TangibleInventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIncidentItems_TenantId",
                schema: "am",
                table: "PropertyIncidentItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIncidentItems_TenantId_ReportId",
                schema: "am",
                table: "PropertyIncidentItems",
                columns: new[] { "TenantId", "ReportId" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIncidentItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "PropertyIncidentItems",
                columns: new[] { "TenantId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIncidentReports_AccountableEmployeeId",
                schema: "am",
                table: "PropertyIncidentReports",
                column: "AccountableEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIncidentReports_Date",
                schema: "am",
                table: "PropertyIncidentReports",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIncidentReports_IncidentType",
                schema: "am",
                table: "PropertyIncidentReports",
                column: "IncidentType");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIncidentReports_TenantId_ReportNo",
                schema: "am",
                table: "PropertyIncidentReports",
                columns: new[] { "TenantId", "ReportNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyItemCatalog_Name",
                schema: "am",
                table: "PropertyItemCatalog",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyItemCatalog_TenantId_Code",
                schema: "am",
                table: "PropertyItemCatalog",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptForReturnedProperties_Date",
                schema: "am",
                table: "ReceiptForReturnedProperties",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptForReturnedProperties_ICSId",
                schema: "am",
                table: "ReceiptForReturnedProperties",
                column: "ICSId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptForReturnedProperties_ReceivedByEmployeeId",
                schema: "am",
                table: "ReceiptForReturnedProperties",
                column: "ReceivedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptForReturnedProperties_ReturnedByEmployeeId",
                schema: "am",
                table: "ReceiptForReturnedProperties",
                column: "ReturnedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptForReturnedProperties_TenantId_RRSPNo",
                schema: "am",
                table: "ReceiptForReturnedProperties",
                columns: new[] { "TenantId", "RRSPNo" },
                unique: true);

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
                name: "IX_ReceiptsForReturnedPPE_TenantId_RRPNo",
                schema: "am",
                table: "ReceiptsForReturnedPPE",
                columns: new[] { "TenantId", "RRPNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReclassificationRecords_CreatedOnUtc",
                schema: "am",
                table: "ReclassificationRecords",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ReclassificationRecords_TenantId",
                schema: "am",
                table: "ReclassificationRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ReclassificationRecords_ThresholdId",
                schema: "am",
                table: "ReclassificationRecords",
                column: "ThresholdId");

            migrationBuilder.CreateIndex(
                name: "IX_RRPItems_RRPId",
                schema: "am",
                table: "RRPItems",
                column: "RRPId");

            migrationBuilder.CreateIndex(
                name: "IX_RRPItems_TangibleInventoryItemId",
                schema: "am",
                table: "RRPItems",
                column: "TangibleInventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RRPItems_TenantId",
                schema: "am",
                table: "RRPItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RRPItems_TenantId_RRPId",
                schema: "am",
                table: "RRPItems",
                columns: new[] { "TenantId", "RRPId" });

            migrationBuilder.CreateIndex(
                name: "IX_RRPItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "RRPItems",
                columns: new[] { "TenantId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_RRSPItems_RRSPId",
                schema: "am",
                table: "RRSPItems",
                column: "RRSPId");

            migrationBuilder.CreateIndex(
                name: "IX_RRSPItems_TangibleInventoryItemId",
                schema: "am",
                table: "RRSPItems",
                column: "TangibleInventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RRSPItems_TenantId",
                schema: "am",
                table: "RRSPItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RRSPItems_TenantId_RRSPId",
                schema: "am",
                table: "RRSPItems",
                columns: new[] { "TenantId", "RRSPId" });

            migrationBuilder.CreateIndex(
                name: "IX_RRSPItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "RRSPItems",
                columns: new[] { "TenantId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableIssuanceRecords_Date",
                schema: "am",
                table: "SemiExpendableIssuanceRecords",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableIssuanceRecords_IssuanceType",
                schema: "am",
                table: "SemiExpendableIssuanceRecords",
                column: "IssuanceType");

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableIssuanceRecords_IssuedByEmployeeId",
                schema: "am",
                table: "SemiExpendableIssuanceRecords",
                column: "IssuedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableIssuanceRecords_TenantId_SMIRNo",
                schema: "am",
                table: "SemiExpendableIssuanceRecords",
                columns: new[] { "TenantId", "SMIRNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableIssuanceRecords_TransferredToTenantId",
                schema: "am",
                table: "SemiExpendableIssuanceRecords",
                column: "TransferredToTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SMIRItems_SMIRId",
                schema: "am",
                table: "SMIRItems",
                column: "SMIRId");

            migrationBuilder.CreateIndex(
                name: "IX_SMIRItems_TangibleInventoryItemId",
                schema: "am",
                table: "SMIRItems",
                column: "TangibleInventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SMIRItems_TenantId",
                schema: "am",
                table: "SMIRItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SMIRItems_TenantId_SMIRId",
                schema: "am",
                table: "SMIRItems",
                columns: new[] { "TenantId", "SMIRId" });

            migrationBuilder.CreateIndex(
                name: "IX_SMIRItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "SMIRItems",
                columns: new[] { "TenantId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_TangibleInventories_Date",
                schema: "am",
                table: "TangibleInventories",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_TangibleInventories_ReceivedByEmployeeId",
                schema: "am",
                table: "TangibleInventories",
                column: "ReceivedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TangibleInventories_TenantId_ReportNo",
                schema: "am",
                table: "TangibleInventories",
                columns: new[] { "TenantId", "ReportNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TangibleInventoryItems_AssetType",
                schema: "am",
                table: "TangibleInventoryItems",
                column: "AssetType");

            migrationBuilder.CreateIndex(
                name: "IX_TangibleInventoryItems_IsIssued",
                schema: "am",
                table: "TangibleInventoryItems",
                column: "IsIssued");

            migrationBuilder.CreateIndex(
                name: "IX_TangibleInventoryItems_ItemId",
                schema: "am",
                table: "TangibleInventoryItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TangibleInventoryItems_TangibleInventoryId",
                schema: "am",
                table: "TangibleInventoryItems",
                column: "TangibleInventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TangibleInventoryItems_TangibleItemId",
                schema: "am",
                table: "TangibleInventoryItems",
                column: "TangibleItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TangibleInventoryItems_TenantId",
                schema: "am",
                table: "TangibleInventoryItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TangibleInventoryItems_TenantId_AssetType",
                schema: "am",
                table: "TangibleInventoryItems",
                columns: new[] { "TenantId", "AssetType" });

            migrationBuilder.CreateIndex(
                name: "IX_TangibleInventoryItems_TenantId_IsIssued",
                schema: "am",
                table: "TangibleInventoryItems",
                columns: new[] { "TenantId", "IsIssued" });

            migrationBuilder.CreateIndex(
                name: "IX_TangibleInventoryItems_TenantId_ItemId",
                schema: "am",
                table: "TangibleInventoryItems",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_TangibleInventoryItems_TenantId_TangibleInventoryId",
                schema: "am",
                table: "TangibleInventoryItems",
                columns: new[] { "TenantId", "TangibleInventoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_TangibleItems_ItemId",
                schema: "am",
                table: "TangibleItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TangibleItems_PurchaseOrderId",
                schema: "am",
                table: "TangibleItems",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TangibleItems_TangibleInventoryItemId",
                schema: "am",
                table: "TangibleItems",
                column: "TangibleInventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TangibleItems_TenantId_CategoryCode",
                schema: "am",
                table: "TangibleItems",
                columns: new[] { "TenantId", "CategoryCode" });

            migrationBuilder.CreateIndex(
                name: "IX_TangibleItems_TenantId_PropertyClass",
                schema: "am",
                table: "TangibleItems",
                columns: new[] { "TenantId", "PropertyClass" });

            migrationBuilder.CreateIndex(
                name: "IX_TangibleItems_TenantId_PropertyNo",
                schema: "am",
                table: "TangibleItems",
                columns: new[] { "TenantId", "PropertyNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyItems_ReportId",
                schema: "am",
                table: "UnserviceablePropertyItems",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyItems_TangibleInventoryItemId",
                schema: "am",
                table: "UnserviceablePropertyItems",
                column: "TangibleInventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyItems_TenantId",
                schema: "am",
                table: "UnserviceablePropertyItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyItems_TenantId_ReportId",
                schema: "am",
                table: "UnserviceablePropertyItems",
                columns: new[] { "TenantId", "ReportId" });

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyItems_TenantId_TangibleInventoryItemId",
                schema: "am",
                table: "UnserviceablePropertyItems",
                columns: new[] { "TenantId", "TangibleInventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyReports_ApprovedByEmployeeId",
                schema: "am",
                table: "UnserviceablePropertyReports",
                column: "ApprovedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyReports_Date",
                schema: "am",
                table: "UnserviceablePropertyReports",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyReports_DisposalMethod",
                schema: "am",
                table: "UnserviceablePropertyReports",
                column: "DisposalMethod");

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyReports_InspectedByEmployeeId",
                schema: "am",
                table: "UnserviceablePropertyReports",
                column: "InspectedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyReports_TenantId_ReportNo",
                schema: "am",
                table: "UnserviceablePropertyReports",
                columns: new[] { "TenantId", "ReportNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetAssignmentHistory",
                schema: "am");

            migrationBuilder.DropTable(
                name: "ICSItems",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PARItems",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PhysicalCountEntries",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PPEIRItems",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PPEIssuanceReports",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PropertyAcknowledgementReceipts",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PropertyCodeCounters",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PropertyIncidentItems",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PropertyIncidentReports",
                schema: "am");

            migrationBuilder.DropTable(
                name: "ReceiptForReturnedProperties",
                schema: "am");

            migrationBuilder.DropTable(
                name: "ReceiptsForReturnedPPE",
                schema: "am");

            migrationBuilder.DropTable(
                name: "ReclassificationRecords",
                schema: "am");

            migrationBuilder.DropTable(
                name: "RRPItems",
                schema: "am");

            migrationBuilder.DropTable(
                name: "RRSPItems",
                schema: "am");

            migrationBuilder.DropTable(
                name: "SemiExpendableIssuanceRecords",
                schema: "am");

            migrationBuilder.DropTable(
                name: "SMIRItems",
                schema: "am");

            migrationBuilder.DropTable(
                name: "UnserviceablePropertyItems",
                schema: "am");

            migrationBuilder.DropTable(
                name: "UnserviceablePropertyReports",
                schema: "am");

            migrationBuilder.DropTable(
                name: "AssetRegistry",
                schema: "am");

            migrationBuilder.DropTable(
                name: "InventoryCustodianSlips",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PhysicalCountSessions",
                schema: "am");

            migrationBuilder.DropTable(
                name: "Locations",
                schema: "am");

            migrationBuilder.DropTable(
                name: "TangibleInventoryItems",
                schema: "am");

            migrationBuilder.DropTable(
                name: "TangibleInventories",
                schema: "am");

            migrationBuilder.DropTable(
                name: "TangibleItems",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PropertyItemCatalog",
                schema: "am");
        }
    }
}

