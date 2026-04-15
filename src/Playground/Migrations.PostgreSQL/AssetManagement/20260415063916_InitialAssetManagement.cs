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
                name: "CapitalizationThresholdPolicies",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LowValueThreshold = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CapitalizationThreshold = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_CapitalizationThresholdPolicies", x => x.Id);
                });

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
                name: "ReclassificationRecords",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_CapitalizationThresholdPolicies_EffectiveDate",
                schema: "am",
                table: "CapitalizationThresholdPolicies",
                column: "EffectiveDate");

            migrationBuilder.CreateIndex(
                name: "IX_CapitalizationThresholdPolicies_IsActive",
                schema: "am",
                table: "CapitalizationThresholdPolicies",
                column: "IsActive",
                unique: true,
                filter: "\"IsActive\" = true");

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
                name: "IX_ReclassificationRecords_CreatedOnUtc",
                schema: "am",
                table: "ReclassificationRecords",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ReclassificationRecords_PolicyId",
                schema: "am",
                table: "ReclassificationRecords",
                column: "PolicyId");

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
                name: "CapitalizationThresholdPolicies",
                schema: "am");

            migrationBuilder.DropTable(
                name: "ICSItems",
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
                name: "ReclassificationRecords",
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
