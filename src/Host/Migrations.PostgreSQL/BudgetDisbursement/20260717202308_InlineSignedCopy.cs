using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.BudgetDisbursement
{
    /// <inheritdoc />
    public partial class InlineSignedCopy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignedDocuments",
                schema: "budgetdisbursement");

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_FileName",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SignedCopy_FileSizeBytes",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_Sha256",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_StorageKey",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_UploadedByName",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedCopy_UploadedOnUtc",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_FileName",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SignedCopy_FileSizeBytes",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_Sha256",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_StorageKey",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedCopy_UploadedByName",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedCopy_UploadedOnUtc",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignedCopy_FileName",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileSizeBytes",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers");

            migrationBuilder.DropColumn(
                name: "SignedCopy_Sha256",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers");

            migrationBuilder.DropColumn(
                name: "SignedCopy_StorageKey",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedByName",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedOnUtc",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileName",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests");

            migrationBuilder.DropColumn(
                name: "SignedCopy_FileSizeBytes",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests");

            migrationBuilder.DropColumn(
                name: "SignedCopy_Sha256",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests");

            migrationBuilder.DropColumn(
                name: "SignedCopy_StorageKey",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedByName",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests");

            migrationBuilder.DropColumn(
                name: "SignedCopy_UploadedOnUtc",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests");

            migrationBuilder.CreateTable(
                name: "SignedDocuments",
                schema: "budgetdisbursement",
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
                schema: "budgetdisbursement",
                table: "SignedDocuments",
                columns: new[] { "TenantId", "DocumentType", "DocumentId" },
                unique: true);
        }
    }
}
