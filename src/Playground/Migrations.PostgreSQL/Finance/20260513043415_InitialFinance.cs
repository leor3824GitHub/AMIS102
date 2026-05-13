using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Finance
{
    /// <inheritdoc />
    public partial class InitialFinance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.CreateTable(
                name: "BudgetUtilizationRecords",
                schema: "finance",
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
                    Version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("PK_BudgetUtilizationRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DisbursementVouchers",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DvNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DvDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
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
                    Version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_BudgetUtilizationRecords_BurNumber",
                schema: "finance",
                table: "BudgetUtilizationRecords",
                column: "BurNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetUtilizationRecords_DisbursementVoucherId",
                schema: "finance",
                table: "BudgetUtilizationRecords",
                column: "DisbursementVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetUtilizationRecords_PurchaseOrderId",
                schema: "finance",
                table: "BudgetUtilizationRecords",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetUtilizationRecords_Status",
                schema: "finance",
                table: "BudgetUtilizationRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementVouchers_DvNumber",
                schema: "finance",
                table: "DisbursementVouchers",
                column: "DvNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementVouchers_PurchaseOrderId",
                schema: "finance",
                table: "DisbursementVouchers",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementVouchers_Status",
                schema: "finance",
                table: "DisbursementVouchers",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetUtilizationRecords",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "DisbursementVouchers",
                schema: "finance");
        }
    }
}

