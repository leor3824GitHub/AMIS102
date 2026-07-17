using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class InlineSignedCopy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignedDocuments",
                schema: "asset_register");

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_FileName",
                schema: "asset_register",
                table: "UnserviceablePropertyReports",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SignedCopy_FileSizeBytes",
                schema: "asset_register",
                table: "UnserviceablePropertyReports",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_Sha256",
                schema: "asset_register",
                table: "UnserviceablePropertyReports",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_StorageKey",
                schema: "asset_register",
                table: "UnserviceablePropertyReports",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_UploadedByName",
                schema: "asset_register",
                table: "UnserviceablePropertyReports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedCopy_UploadedOnUtc",
                schema: "asset_register",
                table: "UnserviceablePropertyReports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_FileName",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SignedCopy_FileSizeBytes",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_Sha256",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_StorageKey",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_UploadedByName",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedCopy_UploadedOnUtc",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_FileName",
                schema: "asset_register",
                table: "ReceivingReports",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SignedCopy_FileSizeBytes",
                schema: "asset_register",
                table: "ReceivingReports",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_Sha256",
                schema: "asset_register",
                table: "ReceivingReports",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_StorageKey",
                schema: "asset_register",
                table: "ReceivingReports",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_UploadedByName",
                schema: "asset_register",
                table: "ReceivingReports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedCopy_UploadedOnUtc",
                schema: "asset_register",
                table: "ReceivingReports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_FileName",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SignedCopy_FileSizeBytes",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_Sha256",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_StorageKey",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_UploadedByName",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedCopy_UploadedOnUtc",
                schema: "asset_register",
                table: "PropertyIssuanceReports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_FileName",
                schema: "asset_register",
                table: "PropertyIncidentReports",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SignedCopy_FileSizeBytes",
                schema: "asset_register",
                table: "PropertyIncidentReports",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_Sha256",
                schema: "asset_register",
                table: "PropertyIncidentReports",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_StorageKey",
                schema: "asset_register",
                table: "PropertyIncidentReports",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_UploadedByName",
                schema: "asset_register",
                table: "PropertyIncidentReports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedCopy_UploadedOnUtc",
                schema: "asset_register",
                table: "PropertyIncidentReports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_FileName",
                schema: "asset_register",
                table: "PropertyAccountabilities",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SignedCopy_FileSizeBytes",
                schema: "asset_register",
                table: "PropertyAccountabilities",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_Sha256",
                schema: "asset_register",
                table: "PropertyAccountabilities",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_StorageKey",
                schema: "asset_register",
                table: "PropertyAccountabilities",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_UploadedByName",
                schema: "asset_register",
                table: "PropertyAccountabilities",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedCopy_UploadedOnUtc",
                schema: "asset_register",
                table: "PropertyAccountabilities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_FileName",
                schema: "asset_register",
                table: "PhysicalCountSessions",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SignedCopy_FileSizeBytes",
                schema: "asset_register",
                table: "PhysicalCountSessions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_Sha256",
                schema: "asset_register",
                table: "PhysicalCountSessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_StorageKey",
                schema: "asset_register",
                table: "PhysicalCountSessions",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_UploadedByName",
                schema: "asset_register",
                table: "PhysicalCountSessions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedCopy_UploadedOnUtc",
                schema: "asset_register",
                table: "PhysicalCountSessions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignedCopy_FileName",
                schema: "asset_register",
                table: "UnserviceablePropertyReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileSizeBytes",
                schema: "asset_register",
                table: "UnserviceablePropertyReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_Sha256",
                schema: "asset_register",
                table: "UnserviceablePropertyReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_StorageKey",
                schema: "asset_register",
                table: "UnserviceablePropertyReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedByName",
                schema: "asset_register",
                table: "UnserviceablePropertyReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedOnUtc",
                schema: "asset_register",
                table: "UnserviceablePropertyReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileName",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileSizeBytes",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.DropColumn(
                name: "SignedCopy_Sha256",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.DropColumn(
                name: "SignedCopy_StorageKey",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedByName",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedOnUtc",
                schema: "asset_register",
                table: "ReturnedPropertyReceipts");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileName",
                schema: "asset_register",
                table: "ReceivingReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileSizeBytes",
                schema: "asset_register",
                table: "ReceivingReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_Sha256",
                schema: "asset_register",
                table: "ReceivingReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_StorageKey",
                schema: "asset_register",
                table: "ReceivingReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedByName",
                schema: "asset_register",
                table: "ReceivingReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedOnUtc",
                schema: "asset_register",
                table: "ReceivingReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileName",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileSizeBytes",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_Sha256",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_StorageKey",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedByName",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedOnUtc",
                schema: "asset_register",
                table: "PropertyIssuanceReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileName",
                schema: "asset_register",
                table: "PropertyIncidentReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileSizeBytes",
                schema: "asset_register",
                table: "PropertyIncidentReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_Sha256",
                schema: "asset_register",
                table: "PropertyIncidentReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_StorageKey",
                schema: "asset_register",
                table: "PropertyIncidentReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedByName",
                schema: "asset_register",
                table: "PropertyIncidentReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedOnUtc",
                schema: "asset_register",
                table: "PropertyIncidentReports");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileName",
                schema: "asset_register",
                table: "PropertyAccountabilities");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileSizeBytes",
                schema: "asset_register",
                table: "PropertyAccountabilities");

            migrationBuilder.DropColumn(
                name: "SignedCopy_Sha256",
                schema: "asset_register",
                table: "PropertyAccountabilities");

            migrationBuilder.DropColumn(
                name: "SignedCopy_StorageKey",
                schema: "asset_register",
                table: "PropertyAccountabilities");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedByName",
                schema: "asset_register",
                table: "PropertyAccountabilities");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedOnUtc",
                schema: "asset_register",
                table: "PropertyAccountabilities");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileName",
                schema: "asset_register",
                table: "PhysicalCountSessions");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileSizeBytes",
                schema: "asset_register",
                table: "PhysicalCountSessions");

            migrationBuilder.DropColumn(
                name: "SignedCopy_Sha256",
                schema: "asset_register",
                table: "PhysicalCountSessions");

            migrationBuilder.DropColumn(
                name: "SignedCopy_StorageKey",
                schema: "asset_register",
                table: "PhysicalCountSessions");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedByName",
                schema: "asset_register",
                table: "PhysicalCountSessions");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedOnUtc",
                schema: "asset_register",
                table: "PhysicalCountSessions");

            migrationBuilder.CreateTable(
                name: "SignedDocuments",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
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
                schema: "asset_register",
                table: "SignedDocuments",
                columns: new[] { "TenantId", "DocumentType", "DocumentId" },
                unique: true);
        }
    }
}
