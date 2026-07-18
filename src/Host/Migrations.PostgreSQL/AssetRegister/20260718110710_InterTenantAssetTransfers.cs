using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class InterTenantAssetTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AccumulatedDepreciation",
                schema: "asset_register",
                table: "ReceivingReportItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssetTransferOffers",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    FromTenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FromAgencyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ToTenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ToAgencyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceIssuanceReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceIssuanceReportNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IssuanceReportType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReceivingReportId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceivingReportNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RejectedReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RespondedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OfferProjectedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResponseProjectedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetTransferOffers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetTransferOfferLines",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    SourcePropertyNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SerialNo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OriginalAcquisitionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AccumulatedDepreciation = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DepreciationCurrentThrough = table.Column<DateOnly>(type: "date", nullable: true),
                    NetBookValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CatalogUacsCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetTransferOfferLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetTransferOfferLines_AssetTransferOffers_OfferId",
                        column: x => x.OfferId,
                        principalSchema: "asset_register",
                        principalTable: "AssetTransferOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransferOfferLines_OfferId",
                schema: "asset_register",
                table: "AssetTransferOfferLines",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransferOffers_TenantId_CorrelationId",
                schema: "asset_register",
                table: "AssetTransferOffers",
                columns: new[] { "TenantId", "CorrelationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransferOffers_TenantId_Direction_Status",
                schema: "asset_register",
                table: "AssetTransferOffers",
                columns: new[] { "TenantId", "Direction", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetTransferOfferLines",
                schema: "asset_register");

            migrationBuilder.DropTable(
                name: "AssetTransferOffers",
                schema: "asset_register");

            migrationBuilder.DropColumn(
                name: "AccumulatedDepreciation",
                schema: "asset_register",
                table: "ReceivingReportItems");
        }
    }
}
