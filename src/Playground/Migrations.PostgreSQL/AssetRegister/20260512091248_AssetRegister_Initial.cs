using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class AssetRegister_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "asset_register");

            migrationBuilder.CreateTable(
                name: "AssetRegistries",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    PropertyNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetType = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    PropertyClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CategoryCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SerialNo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Unit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FundCluster = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UacsObjectCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedUsefulLifeYears = table.Column<int>(type: "integer", nullable: false),
                    AccumulatedDepreciation = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AccumulatedImpairmentLosses = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LifecycleState = table.Column<int>(type: "integer", nullable: false),
                    CurrentCondition = table.Column<int>(type: "integer", nullable: false),
                    CurrentCustodianId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentAccountabilityId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceIARId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourcePurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetRegistries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhysicalCountSessions",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FundCluster = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StartedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ClosedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    AsAt = table.Column<DateOnly>(type: "date", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ApprovedBy_EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedBy_PrintedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApprovedBy_Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WitnessedBy_EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    WitnessedBy_PrintedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WitnessedBy_Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalCountSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyAccountabilities",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    DocumentNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AccountabilityType = table.Column<int>(type: "integer", nullable: false),
                    FundCluster = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IssuedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SupersededByAccountabilityId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupersedesAccountabilityId = table.Column<Guid>(type: "uuid", nullable: true),
                    IssuedBy_EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedBy_PrintedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IssuedBy_Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReceivedBy_EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedBy_PrintedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReceivedBy_Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyAccountabilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyCodeCounters",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    CounterKey = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSerial = table.Column<int>(type: "integer", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyCodeCounters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyIncidentReports",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    IncidentNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IncidentType = table.Column<int>(type: "integer", nullable: false),
                    IncidentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FundCluster = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DepartmentOffice = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Circumstances = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AccountableOfficer_EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountableOfficer_PrintedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AccountableOfficer_Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AccountableOfficerDesignation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AccountableOfficerGovIdType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AccountableOfficerGovIdNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AccountableOfficerGovIdIssuedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    NotedBy_EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    NotedBy_PrintedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NotedBy_Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PoliceNotified = table.Column<bool>(type: "boolean", nullable: false),
                    PoliceStation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PoliceNotifiedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    PoliceBlotterRef = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NotarizedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    NotaryDocNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    NotaryPageNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    NotaryBookNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    NotarySeriesOf = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReliefRequestedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ReliefGrantedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ReliefGrantedRef = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AmountSettled = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    SettledOn = table.Column<DateOnly>(type: "date", nullable: true),
                    RecoveredOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyIncidentReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyIssuanceReports",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    ReportNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReportType = table.Column<int>(type: "integer", nullable: false),
                    FundCluster = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PeriodFromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodToDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PreparedBy_EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreparedBy_PrintedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PreparedBy_Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CertifiedBy_EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CertifiedBy_PrintedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CertifiedBy_Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PostedBy_EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    PostedBy_PrintedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PostedBy_Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PostedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyIssuanceReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyItemCatalog",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DefaultPropertyClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DefaultCategoryCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DefaultUnit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UacsObjectCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    EstimatedUsefulLifeYears = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyItemCatalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnserviceablePropertyReports",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    ReportNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReportType = table.Column<int>(type: "integer", nullable: false),
                    AsAt = table.Column<DateOnly>(type: "date", nullable: false),
                    FundCluster = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Station = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AccountableOfficer_EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountableOfficer_PrintedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AccountableOfficer_Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApprovedBy_EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedBy_PrintedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApprovedBy_Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InspectedBy_EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    InspectedBy_PrintedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InspectedBy_Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InspectedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    WitnessedBy_EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    WitnessedBy_PrintedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WitnessedBy_Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WitnessedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnserviceablePropertyReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhysicalCountEntries",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetRegistryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Snapshot_PropertyNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Snapshot_Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Snapshot_AssetType = table.Column<int>(type: "integer", nullable: true),
                    Snapshot_UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Snapshot_Unit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Snapshot_EstimatedUsefulLifeYears = table.Column<int>(type: "integer", nullable: true),
                    Snapshot_AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Snapshot_UacsObjectCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Snapshot_SerialNo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Snapshot_Brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Snapshot_Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SnapshotArticle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SnapshotUnit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SnapshotUnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Condition = table.Column<int>(type: "integer", nullable: false),
                    ScannedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PhotoPath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ScannedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProposedPropertyClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProposedCategoryCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProposedAcquisitionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ProposedUnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalCountEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhysicalCountEntries_PhysicalCountSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "asset_register",
                        principalTable: "PhysicalCountSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhysicalCountSessionConductors",
                schema: "asset_register",
                columns: table => new
                {
                    PhysicalCountSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrintedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalCountSessionConductors", x => new { x.PhysicalCountSessionId, x.Ordinal });
                    table.ForeignKey(
                        name: "FK_PhysicalCountSessionConductors_PhysicalCountSessions_Physic~",
                        column: x => x.PhysicalCountSessionId,
                        principalSchema: "asset_register",
                        principalTable: "PhysicalCountSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PropertyAccountabilityLines",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountabilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetRegistryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Snapshot_PropertyNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Snapshot_Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Snapshot_AssetType = table.Column<int>(type: "integer", nullable: false),
                    Snapshot_UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Snapshot_Unit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Snapshot_EstimatedUsefulLifeYears = table.Column<int>(type: "integer", nullable: false),
                    Snapshot_AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Snapshot_UacsObjectCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Snapshot_SerialNo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Snapshot_Brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Snapshot_Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SnapshotItemNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SnapshotResponsibilityCenterCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IssuedQty = table.Column<int>(type: "integer", nullable: false),
                    ReturnedQty = table.Column<int>(type: "integer", nullable: false),
                    LineStatus = table.Column<int>(type: "integer", nullable: false),
                    ReturnedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ReturnedConditionAtReturn = table.Column<int>(type: "integer", nullable: true),
                    LostOnIncidentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyAccountabilityLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyAccountabilityLines_PropertyAccountabilities_Accoun~",
                        column: x => x.AccountabilityId,
                        principalSchema: "asset_register",
                        principalTable: "PropertyAccountabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PropertyIncidentItems",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetRegistryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Snapshot_PropertyNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Snapshot_Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Snapshot_AssetType = table.Column<int>(type: "integer", nullable: false),
                    Snapshot_UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Snapshot_Unit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Snapshot_EstimatedUsefulLifeYears = table.Column<int>(type: "integer", nullable: false),
                    Snapshot_AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Snapshot_UacsObjectCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Snapshot_SerialNo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Snapshot_Brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Snapshot_Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SnapshotAcquisitionCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SnapshotCurrentReplacementCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AccountabilityLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemResolution = table.Column<int>(type: "integer", nullable: false),
                    ResolvedOn = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyIncidentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyIncidentItems_PropertyIncidentReports_ReportId",
                        column: x => x.ReportId,
                        principalSchema: "asset_register",
                        principalTable: "PropertyIncidentReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PropertyIssuanceReportLines",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountabilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountabilityLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetRegistryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Snapshot_PropertyNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Snapshot_Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Snapshot_AssetType = table.Column<int>(type: "integer", nullable: false),
                    Snapshot_UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Snapshot_Unit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Snapshot_EstimatedUsefulLifeYears = table.Column<int>(type: "integer", nullable: false),
                    Snapshot_AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Snapshot_UacsObjectCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Snapshot_SerialNo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Snapshot_Brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Snapshot_Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SnapshotResponsibilityCenterCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SnapshotQuantityIssued = table.Column<int>(type: "integer", nullable: false),
                    SnapshotUnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SnapshotAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyIssuanceReportLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyIssuanceReportLines_PropertyIssuanceReports_ReportId",
                        column: x => x.ReportId,
                        principalSchema: "asset_register",
                        principalTable: "PropertyIssuanceReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnserviceablePropertyItems",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetRegistryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Snapshot_PropertyNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Snapshot_Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Snapshot_AssetType = table.Column<int>(type: "integer", nullable: false),
                    Snapshot_UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Snapshot_Unit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Snapshot_EstimatedUsefulLifeYears = table.Column<int>(type: "integer", nullable: false),
                    Snapshot_AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Snapshot_UacsObjectCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Snapshot_SerialNo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Snapshot_Brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Snapshot_Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SnapshotDateAcquired = table.Column<DateOnly>(type: "date", nullable: false),
                    SnapshotAcquisitionCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SnapshotAccumulatedDepreciation = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SnapshotAccumulatedImpairmentLosses = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DisposalMethod = table.Column<int>(type: "integer", nullable: true),
                    DisposalOtherSpecify = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AppraisedValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DisposalRecordedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    SaleORNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SaleAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnserviceablePropertyItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnserviceablePropertyItems_UnserviceablePropertyReports_Rep~",
                        column: x => x.ReportId,
                        principalSchema: "asset_register",
                        principalTable: "UnserviceablePropertyReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetRegistries_TenantId_CurrentCustodianId",
                schema: "asset_register",
                table: "AssetRegistries",
                columns: new[] { "TenantId", "CurrentCustodianId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetRegistries_TenantId_ItemId",
                schema: "asset_register",
                table: "AssetRegistries",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetRegistries_TenantId_LifecycleState",
                schema: "asset_register",
                table: "AssetRegistries",
                columns: new[] { "TenantId", "LifecycleState" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetRegistries_TenantId_PropertyNo",
                schema: "asset_register",
                table: "AssetRegistries",
                columns: new[] { "TenantId", "PropertyNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_AssetRegistryId",
                schema: "asset_register",
                table: "PhysicalCountEntries",
                column: "AssetRegistryId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_SessionId",
                schema: "asset_register",
                table: "PhysicalCountEntries",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountSessions_TenantId_Code",
                schema: "asset_register",
                table: "PhysicalCountSessions",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountSessions_TenantId_Status",
                schema: "asset_register",
                table: "PhysicalCountSessions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyAccountabilities_TenantId_DocumentNo",
                schema: "asset_register",
                table: "PropertyAccountabilities",
                columns: new[] { "TenantId", "DocumentNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyAccountabilities_TenantId_Status_ExpiresOn",
                schema: "asset_register",
                table: "PropertyAccountabilities",
                columns: new[] { "TenantId", "Status", "ExpiresOn" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyAccountabilityLines_AccountabilityId_LineStatus",
                schema: "asset_register",
                table: "PropertyAccountabilityLines",
                columns: new[] { "AccountabilityId", "LineStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyAccountabilityLines_AssetRegistryId",
                schema: "asset_register",
                table: "PropertyAccountabilityLines",
                column: "AssetRegistryId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyCodeCounters_TenantId_Year_Month_CounterKey",
                schema: "asset_register",
                table: "PropertyCodeCounters",
                columns: new[] { "TenantId", "Year", "Month", "CounterKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIncidentItems_AssetRegistryId",
                schema: "asset_register",
                table: "PropertyIncidentItems",
                column: "AssetRegistryId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIncidentItems_ReportId",
                schema: "asset_register",
                table: "PropertyIncidentItems",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIncidentReports_TenantId_IncidentNo",
                schema: "asset_register",
                table: "PropertyIncidentReports",
                columns: new[] { "TenantId", "IncidentNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIncidentReports_TenantId_Status",
                schema: "asset_register",
                table: "PropertyIncidentReports",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIssuanceReportLines_AccountabilityId",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines",
                column: "AccountabilityId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIssuanceReportLines_AccountabilityLineId",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines",
                column: "AccountabilityLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIssuanceReportLines_AssetRegistryId",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines",
                column: "AssetRegistryId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIssuanceReportLines_ReportId",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIssuanceReports_TenantId_ReportNo",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                columns: new[] { "TenantId", "ReportNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIssuanceReports_TenantId_Status",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyItemCatalog_TenantId_Code",
                schema: "asset_register",
                table: "PropertyItemCatalog",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyItems_AssetRegistryId",
                schema: "asset_register",
                table: "UnserviceablePropertyItems",
                column: "AssetRegistryId");

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyItems_ReportId",
                schema: "asset_register",
                table: "UnserviceablePropertyItems",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyReports_TenantId_ReportNo",
                schema: "asset_register",
                table: "UnserviceablePropertyReports",
                columns: new[] { "TenantId", "ReportNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnserviceablePropertyReports_TenantId_Status",
                schema: "asset_register",
                table: "UnserviceablePropertyReports",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetRegistries",
                schema: "asset_register");

            migrationBuilder.DropTable(
                name: "PhysicalCountEntries",
                schema: "asset_register");

            migrationBuilder.DropTable(
                name: "PhysicalCountSessionConductors",
                schema: "asset_register");

            migrationBuilder.DropTable(
                name: "PropertyAccountabilityLines",
                schema: "asset_register");

            migrationBuilder.DropTable(
                name: "PropertyCodeCounters",
                schema: "asset_register");

            migrationBuilder.DropTable(
                name: "PropertyIncidentItems",
                schema: "asset_register");

            migrationBuilder.DropTable(
                name: "PropertyIssuanceReportLines",
                schema: "asset_register");

            migrationBuilder.DropTable(
                name: "PropertyItemCatalog",
                schema: "asset_register");

            migrationBuilder.DropTable(
                name: "UnserviceablePropertyItems",
                schema: "asset_register");

            migrationBuilder.DropTable(
                name: "PhysicalCountSessions",
                schema: "asset_register");

            migrationBuilder.DropTable(
                name: "PropertyAccountabilities",
                schema: "asset_register");

            migrationBuilder.DropTable(
                name: "PropertyIncidentReports",
                schema: "asset_register");

            migrationBuilder.DropTable(
                name: "PropertyIssuanceReports",
                schema: "asset_register");

            migrationBuilder.DropTable(
                name: "UnserviceablePropertyReports",
                schema: "asset_register");
        }
    }
}
