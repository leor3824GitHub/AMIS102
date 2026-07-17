using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementAcquisition
{
    /// <inheritdoc />
    public partial class InlineSignedCopy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignedDocuments",
                schema: "procurement");

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_FileName",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SignedCopy_FileSizeBytes",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_Sha256",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_StorageKey",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_UploadedByName",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedCopy_UploadedOnUtc",
                schema: "procurement",
                table: "PurchaseRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_FileName",
                schema: "procurement",
                table: "PurchaseOrders",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SignedCopy_FileSizeBytes",
                schema: "procurement",
                table: "PurchaseOrders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_Sha256",
                schema: "procurement",
                table: "PurchaseOrders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_StorageKey",
                schema: "procurement",
                table: "PurchaseOrders",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_UploadedByName",
                schema: "procurement",
                table: "PurchaseOrders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedCopy_UploadedOnUtc",
                schema: "procurement",
                table: "PurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_FileName",
                schema: "procurement",
                table: "JobOrders",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SignedCopy_FileSizeBytes",
                schema: "procurement",
                table: "JobOrders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_Sha256",
                schema: "procurement",
                table: "JobOrders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_StorageKey",
                schema: "procurement",
                table: "JobOrders",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_UploadedByName",
                schema: "procurement",
                table: "JobOrders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedCopy_UploadedOnUtc",
                schema: "procurement",
                table: "JobOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_FileName",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SignedCopy_FileSizeBytes",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_Sha256",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_StorageKey",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_UploadedByName",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedCopy_UploadedOnUtc",
                schema: "procurement",
                table: "InspectionAcceptanceReports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_FileName",
                schema: "procurement",
                table: "CanvassRequests",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SignedCopy_FileSizeBytes",
                schema: "procurement",
                table: "CanvassRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_Sha256",
                schema: "procurement",
                table: "CanvassRequests",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_StorageKey",
                schema: "procurement",
                table: "CanvassRequests",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_UploadedByName",
                schema: "procurement",
                table: "CanvassRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedCopy_UploadedOnUtc",
                schema: "procurement",
                table: "CanvassRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_FileName",
                schema: "procurement",
                table: "CanvassQuotations",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SignedCopy_FileSizeBytes",
                schema: "procurement",
                table: "CanvassQuotations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_Sha256",
                schema: "procurement",
                table: "CanvassQuotations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_StorageKey",
                schema: "procurement",
                table: "CanvassQuotations",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_UploadedByName",
                schema: "procurement",
                table: "CanvassQuotations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedCopy_UploadedOnUtc",
                schema: "procurement",
                table: "CanvassQuotations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignedCopy_FileName",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileSizeBytes",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "SignedCopy_Sha256",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "SignedCopy_StorageKey",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedByName",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedOnUtc",
                schema: "procurement",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileName",
                schema: "procurement",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileSizeBytes",
                schema: "procurement",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SignedCopy_Sha256",
                schema: "procurement",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SignedCopy_StorageKey",
                schema: "procurement",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedByName",
                schema: "procurement",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedOnUtc",
                schema: "procurement",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileName",
                schema: "procurement",
                table: "JobOrders");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileSizeBytes",
                schema: "procurement",
                table: "JobOrders");

            migrationBuilder.DropColumn(
                name: "SignedCopy_Sha256",
                schema: "procurement",
                table: "JobOrders");

            migrationBuilder.DropColumn(
                name: "SignedCopy_StorageKey",
                schema: "procurement",
                table: "JobOrders");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedByName",
                schema: "procurement",
                table: "JobOrders");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedOnUtc",
                schema: "procurement",
                table: "JobOrders");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileName",
                schema: "procurement",
                table: "InspectionAcceptanceReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileSizeBytes",
                schema: "procurement",
                table: "InspectionAcceptanceReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_Sha256",
                schema: "procurement",
                table: "InspectionAcceptanceReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_StorageKey",
                schema: "procurement",
                table: "InspectionAcceptanceReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedByName",
                schema: "procurement",
                table: "InspectionAcceptanceReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedOnUtc",
                schema: "procurement",
                table: "InspectionAcceptanceReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileName",
                schema: "procurement",
                table: "CanvassRequests");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileSizeBytes",
                schema: "procurement",
                table: "CanvassRequests");

            migrationBuilder.DropColumn(
                name: "SignedCopy_Sha256",
                schema: "procurement",
                table: "CanvassRequests");

            migrationBuilder.DropColumn(
                name: "SignedCopy_StorageKey",
                schema: "procurement",
                table: "CanvassRequests");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedByName",
                schema: "procurement",
                table: "CanvassRequests");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedOnUtc",
                schema: "procurement",
                table: "CanvassRequests");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileName",
                schema: "procurement",
                table: "CanvassQuotations");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileSizeBytes",
                schema: "procurement",
                table: "CanvassQuotations");

            migrationBuilder.DropColumn(
                name: "SignedCopy_Sha256",
                schema: "procurement",
                table: "CanvassQuotations");

            migrationBuilder.DropColumn(
                name: "SignedCopy_StorageKey",
                schema: "procurement",
                table: "CanvassQuotations");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedByName",
                schema: "procurement",
                table: "CanvassQuotations");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedOnUtc",
                schema: "procurement",
                table: "CanvassQuotations");

            migrationBuilder.CreateTable(
                name: "SignedDocuments",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UploadedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UploadedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignedDocuments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SignedDocuments_TenantId_DocumentType_DocumentId",
                schema: "procurement",
                table: "SignedDocuments",
                columns: new[] { "TenantId", "DocumentType", "DocumentId" },
                unique: true);
        }
    }
}
