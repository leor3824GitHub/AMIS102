using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.BudgetDisbursement
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "budgetdisbursement");

            migrationBuilder.CreateTable(
                name: "BudgetDisbursementModuleSettings",
                schema: "budgetdisbursement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WatermarkSignedCopies = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DvSectionAName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DvSectionADesignation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DvSectionBName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DvSectionBDesignation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DvSectionCName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DvSectionCDesignation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BurSectionAName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BurSectionADesignation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BurSectionBName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BurSectionBDesignation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetDisbursementModuleSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BudgetUtilizationRequests",
                schema: "budgetdisbursement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BurNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BurDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DisbursementVoucherId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisbursementVoucherNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    AllotmentClass = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    UacsObjectCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResponsibilityCenter = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Particulars = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
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
                    table.PrimaryKey("PK_BudgetUtilizationRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BurNumberSequences",
                schema: "budgetdisbursement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    LastSerial = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BurNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DisbursementVouchers",
                schema: "budgetdisbursement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DvNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DvDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BudgetUtilizationRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    BurNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FundCluster = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Payee = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TinNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PayeeAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Particulars = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ModeOfPayment = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PaidDate = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_DisbursementVouchers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DvNumberSequences",
                schema: "budgetdisbursement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    LastSerial = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DvNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignedDocuments",
                schema: "budgetdisbursement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UploadedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_SignedDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DisbursementVoucherDeductions",
                schema: "budgetdisbursement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DisbursementVoucherId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisbursementVoucherDeductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisbursementVoucherDeductions_DisbursementVouchers_Disburse~",
                        column: x => x.DisbursementVoucherId,
                        principalSchema: "budgetdisbursement",
                        principalTable: "DisbursementVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetDisbursementModuleSettings_TenantId",
                schema: "budgetdisbursement",
                table: "BudgetDisbursementModuleSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetUtilizationRequests_BurNumber",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests",
                column: "BurNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetUtilizationRequests_DisbursementVoucherId",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests",
                column: "DisbursementVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetUtilizationRequests_PurchaseOrderId",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetUtilizationRequests_Status",
                schema: "budgetdisbursement",
                table: "BudgetUtilizationRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BurNumberSequences_Year",
                schema: "budgetdisbursement",
                table: "BurNumberSequences",
                column: "Year",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementVoucherDeductions_DisbursementVoucherId",
                schema: "budgetdisbursement",
                table: "DisbursementVoucherDeductions",
                column: "DisbursementVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementVouchers_BudgetUtilizationRequestId",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers",
                column: "BudgetUtilizationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementVouchers_DvNumber",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers",
                column: "DvNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementVouchers_PurchaseOrderId",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementVouchers_Status",
                schema: "budgetdisbursement",
                table: "DisbursementVouchers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DvNumberSequences_Year",
                schema: "budgetdisbursement",
                table: "DvNumberSequences",
                column: "Year",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignedDocuments_TenantId_DocumentType_DocumentId",
                schema: "budgetdisbursement",
                table: "SignedDocuments",
                columns: new[] { "TenantId", "DocumentType", "DocumentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetDisbursementModuleSettings",
                schema: "budgetdisbursement");

            migrationBuilder.DropTable(
                name: "BudgetUtilizationRequests",
                schema: "budgetdisbursement");

            migrationBuilder.DropTable(
                name: "BurNumberSequences",
                schema: "budgetdisbursement");

            migrationBuilder.DropTable(
                name: "DisbursementVoucherDeductions",
                schema: "budgetdisbursement");

            migrationBuilder.DropTable(
                name: "DvNumberSequences",
                schema: "budgetdisbursement");

            migrationBuilder.DropTable(
                name: "SignedDocuments",
                schema: "budgetdisbursement");

            migrationBuilder.DropTable(
                name: "DisbursementVouchers",
                schema: "budgetdisbursement");
        }
    }
}
