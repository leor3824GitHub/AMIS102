using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Migrations.AssetRegister
{
    /// <inheritdoc />
    public partial class AddReturnedPropertyReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReturnedPropertyReceipts",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReceiptNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReceiptType = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    AccountabilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountabilityDocumentNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReturnedBy_EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnedBy_PrintedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReturnedBy_Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReceivedBy_EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceivedBy_PrintedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReceivedBy_Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnedPropertyReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReturnedPropertyReceiptItems",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountabilityLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetRegistryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
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
                    Snapshot_Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnedPropertyReceiptItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnedPropertyReceiptItems_ReturnedPropertyReceipts_Recei~",
                        column: x => x.ReceiptId,
                        principalSchema: "asset_register",
                        principalTable: "ReturnedPropertyReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnedPropertyReceiptItems_AccountabilityLineId",
                schema: "asset_register",
                table: "ReturnedPropertyReceiptItems",
                column: "AccountabilityLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnedPropertyReceiptItems_AssetRegistryId",
                schema: "asset_register",
                table: "ReturnedPropertyReceiptItems",
                column: "AssetRegistryId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnedPropertyReceiptItems_ReceiptId",
                schema: "asset_register",
                table: "ReturnedPropertyReceiptItems",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnedPropertyReceipts_TenantId_AccountabilityId",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                columns: new[] { "TenantId", "AccountabilityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnedPropertyReceipts_TenantId_Date",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                columns: new[] { "TenantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnedPropertyReceipts_TenantId_ReceiptNo",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                columns: new[] { "TenantId", "ReceiptNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReturnedPropertyReceiptItems",
                schema: "asset_register");

            migrationBuilder.DropTable(
                name: "ReturnedPropertyReceipts",
                schema: "asset_register");
        }
    }
}
