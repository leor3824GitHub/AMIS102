using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.AssetManagement
{
    /// <inheritdoc />
    public partial class AddAssetRegistryAndLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetAssignmentHistory",
                schema: "am");

            migrationBuilder.DropTable(
                name: "AssetRegistry",
                schema: "am");

            migrationBuilder.DropTable(
                name: "Locations",
                schema: "am");
        }
    }
}
