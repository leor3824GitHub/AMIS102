using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Migrations.Expendable
{
    /// <inheritdoc />
    public partial class Expendable_ProductInventoryBatchesToTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Batches",
                schema: "expendable",
                table: "ProductInventory");

            migrationBuilder.CreateTable(
                name: "ProductInventoryBatches",
                schema: "expendable",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityAvailable = table.Column<int>(type: "integer", nullable: false),
                    QuantityIssued = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SourceReference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ReceivedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InspectionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FirstIssueDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ProductInventoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductInventoryBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductInventoryBatches_ProductInventory_ProductInventoryId",
                        column: x => x.ProductInventoryId,
                        principalSchema: "expendable",
                        principalTable: "ProductInventory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductInventoryBatches_ProductInventoryId",
                schema: "expendable",
                table: "ProductInventoryBatches",
                column: "ProductInventoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductInventoryBatches",
                schema: "expendable");

            migrationBuilder.AddColumn<string>(
                name: "Batches",
                schema: "expendable",
                table: "ProductInventory",
                type: "jsonb",
                nullable: true);
        }
    }
}
