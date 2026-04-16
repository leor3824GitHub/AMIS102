using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.AssetManagement
{
    /// <inheritdoc />
    public partial class InitialAssetManagement : Migration
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
                    ICSNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    FundCluster = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IssuedFromEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceivedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
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
                name: "PhysicalCountSessions",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    ClassCode = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    ItemCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    OfficeCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
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
                    ClassCode = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    ItemCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    OfficeCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
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
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemiExpendablePropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CategoryAtTimeOfReport = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
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
                name: "ReceiptForReturnedProperties",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "RRSPItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RRSPId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemiExpendablePropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CategoryAtTimeOfReturn = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
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
                name: "SemiExpendableItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_SemiExpendableItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SMIRItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SMIRId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemiExpendablePropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CategoryAtTimeOfIssuance = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMIRItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SMRRs",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SMRRNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
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
                    table.PrimaryKey("PK_SMRRs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnserviceablePropertyItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemiExpendablePropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CategoryAtTimeOfReport = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
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
                name: "SemiExpendableProperties",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SemiExpendableItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FundCluster = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CurrentCustodianId = table.Column<Guid>(type: "uuid", nullable: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SMRRItemId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_SemiExpendableProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SemiExpendableProperties_SemiExpendableItems_SemiExpendable~",
                        column: x => x.SemiExpendableItemId,
                        principalSchema: "am",
                        principalTable: "SemiExpendableItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SMRRItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SMRRId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SemiExpendableItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMRRItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SMRRItems_SMRRs_SMRRId",
                        column: x => x.SMRRId,
                        principalSchema: "am",
                        principalTable: "SMRRs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SMRRItems_SemiExpendableItems_SemiExpendableItemId",
                        column: x => x.SemiExpendableItemId,
                        principalSchema: "am",
                        principalTable: "SemiExpendableItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ICSItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ICSId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemiExpendablePropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EstimatedUsefulLifeYears = table.Column<int>(type: "integer", nullable: true),
                    CategoryAtTimeOfIssuance = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
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
                        name: "FK_ICSItems_SemiExpendableProperties_SemiExpendablePropertyId",
                        column: x => x.SemiExpendablePropertyId,
                        principalSchema: "am",
                        principalTable: "SemiExpendableProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhysicalCountEntries",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PPEItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    SemiExpendablePropertyId = table.Column<Guid>(type: "uuid", nullable: true),
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
                        name: "FK_PhysicalCountEntries_PPEItems_PPEItemId",
                        column: x => x.PPEItemId,
                        principalSchema: "am",
                        principalTable: "PPEItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhysicalCountEntries_PhysicalCountSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "am",
                        principalTable: "PhysicalCountSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhysicalCountEntries_SemiExpendableProperties_SemiExpendabl~",
                        column: x => x.SemiExpendablePropertyId,
                        principalSchema: "am",
                        principalTable: "SemiExpendableProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ICSItems_ICSId",
                schema: "am",
                table: "ICSItems",
                column: "ICSId");

            migrationBuilder.CreateIndex(
                name: "IX_ICSItems_SemiExpendablePropertyId",
                schema: "am",
                table: "ICSItems",
                column: "SemiExpendablePropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCustodianSlips_CancelledByRRSPId",
                schema: "am",
                table: "InventoryCustodianSlips",
                column: "CancelledByRRSPId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCustodianSlips_Category",
                schema: "am",
                table: "InventoryCustodianSlips",
                column: "Category");

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
                name: "IX_InventoryCustodianSlips_ICSNo",
                schema: "am",
                table: "InventoryCustodianSlips",
                column: "ICSNo",
                unique: true);

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
                name: "IX_PhysicalCountEntries_PPEItemId",
                schema: "am",
                table: "PhysicalCountEntries",
                column: "PPEItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_Result",
                schema: "am",
                table: "PhysicalCountEntries",
                column: "Result");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_SemiExpendablePropertyId",
                schema: "am",
                table: "PhysicalCountEntries",
                column: "SemiExpendablePropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_SessionId",
                schema: "am",
                table: "PhysicalCountEntries",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_SessionId_PPEItemId",
                schema: "am",
                table: "PhysicalCountEntries",
                columns: new[] { "SessionId", "PPEItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_SessionId_SemiExpendablePropertyId",
                schema: "am",
                table: "PhysicalCountEntries",
                columns: new[] { "SessionId", "SemiExpendablePropertyId" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountSessions_CountDate",
                schema: "am",
                table: "PhysicalCountSessions",
                column: "CountDate");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountSessions_SessionNo",
                schema: "am",
                table: "PhysicalCountSessions",
                column: "SessionNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountSessions_Status",
                schema: "am",
                table: "PhysicalCountSessions",
                column: "Status");

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
                name: "IX_PropertyIncidentItems_SemiExpendablePropertyId",
                schema: "am",
                table: "PropertyIncidentItems",
                column: "SemiExpendablePropertyId");

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
                name: "IX_PropertyIncidentReports_ReportNo",
                schema: "am",
                table: "PropertyIncidentReports",
                column: "ReportNo",
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
                name: "IX_ReceiptForReturnedProperties_RRSPNo",
                schema: "am",
                table: "ReceiptForReturnedProperties",
                column: "RRSPNo",
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
                name: "IX_ReceiptsForReturnedPPE_RRPNo",
                schema: "am",
                table: "ReceiptsForReturnedPPE",
                column: "RRPNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReclassificationRecords_CreatedOnUtc",
                schema: "am",
                table: "ReclassificationRecords",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ReclassificationRecords_ThresholdId",
                schema: "am",
                table: "ReclassificationRecords",
                column: "ThresholdId");

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

            migrationBuilder.CreateIndex(
                name: "IX_RRSPItems_RRSPId",
                schema: "am",
                table: "RRSPItems",
                column: "RRSPId");

            migrationBuilder.CreateIndex(
                name: "IX_RRSPItems_SemiExpendablePropertyId",
                schema: "am",
                table: "RRSPItems",
                column: "SemiExpendablePropertyId");

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
                name: "IX_SemiExpendableIssuanceRecords_SMIRNo",
                schema: "am",
                table: "SemiExpendableIssuanceRecords",
                column: "SMIRNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableIssuanceRecords_TransferredToTenantId",
                schema: "am",
                table: "SemiExpendableIssuanceRecords",
                column: "TransferredToTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableItems_Code",
                schema: "am",
                table: "SemiExpendableItems",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableItems_Name",
                schema: "am",
                table: "SemiExpendableItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableProperties_Category",
                schema: "am",
                table: "SemiExpendableProperties",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableProperties_CurrentCustodianId",
                schema: "am",
                table: "SemiExpendableProperties",
                column: "CurrentCustodianId");

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableProperties_PropertyNo",
                schema: "am",
                table: "SemiExpendableProperties",
                column: "PropertyNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableProperties_SemiExpendableItemId",
                schema: "am",
                table: "SemiExpendableProperties",
                column: "SemiExpendableItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableProperties_SMRRItemId",
                schema: "am",
                table: "SemiExpendableProperties",
                column: "SMRRItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableProperties_Status",
                schema: "am",
                table: "SemiExpendableProperties",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SMIRItems_SemiExpendablePropertyId",
                schema: "am",
                table: "SMIRItems",
                column: "SemiExpendablePropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_SMIRItems_SMIRId",
                schema: "am",
                table: "SMIRItems",
                column: "SMIRId");

            migrationBuilder.CreateIndex(
                name: "IX_SMRRItems_SemiExpendableItemId",
                schema: "am",
                table: "SMRRItems",
                column: "SemiExpendableItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SMRRItems_SMRRId",
                schema: "am",
                table: "SMRRItems",
                column: "SMRRId");

            migrationBuilder.CreateIndex(
                name: "IX_SMRRs_Date",
                schema: "am",
                table: "SMRRs",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_SMRRs_ReceivedByEmployeeId",
                schema: "am",
                table: "SMRRs",
                column: "ReceivedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SMRRs_SMRRNo",
                schema: "am",
                table: "SMRRs",
                column: "SMRRNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyItems_ReportId",
                schema: "am",
                table: "UnserviceablePropertyItems",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyItems_SemiExpendablePropertyId",
                schema: "am",
                table: "UnserviceablePropertyItems",
                column: "SemiExpendablePropertyId");

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
                name: "IX_UnserviceablePropertyReports_ReportNo",
                schema: "am",
                table: "UnserviceablePropertyReports",
                column: "ReportNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "PPEReceivingReports",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PPERRItems",
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
                name: "SMRRItems",
                schema: "am");

            migrationBuilder.DropTable(
                name: "UnserviceablePropertyItems",
                schema: "am");

            migrationBuilder.DropTable(
                name: "UnserviceablePropertyReports",
                schema: "am");

            migrationBuilder.DropTable(
                name: "InventoryCustodianSlips",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PPEItems",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PhysicalCountSessions",
                schema: "am");

            migrationBuilder.DropTable(
                name: "SemiExpendableProperties",
                schema: "am");

            migrationBuilder.DropTable(
                name: "SMRRs",
                schema: "am");

            migrationBuilder.DropTable(
                name: "SemiExpendableItems",
                schema: "am");
        }
    }
}
