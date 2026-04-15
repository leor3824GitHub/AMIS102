using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.AssetManagement
{
    /// <inheritdoc />
    public partial class AddPhysicalCountTrack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhysicalCountSessions",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CountDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StationOffice = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PreparedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertifiedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_PhysicalCountSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhysicalCountEntries",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PPEItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    SemiExpendablePropertyId = table.Column<Guid>(type: "uuid", nullable: true),
                    PropertyNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Result = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Condition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QuantityOnHand = table.Column<int>(type: "integer", nullable: false),
                    ScannedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PhotoPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalCountEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhysicalCountEntries_PPEItems_PPEItemId",
                        column: x => x.PPEItemId,
                        principalSchema: "am",
                        principalTable: "PPEItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhysicalCountEntries_PhysicalCountSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "am",
                        principalTable: "PhysicalCountSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhysicalCountEntries_SemiExpendableProperties_SemiExpendabl~",
                        column: x => x.SemiExpendablePropertyId,
                        principalSchema: "am",
                        principalTable: "SemiExpendableProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_PPEItemId",
                schema: "am",
                table: "PhysicalCountEntries",
                column: "PPEItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_Result",
                schema: "am",
                table: "PhysicalCountEntries",
                column: "Result");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_SemiExpendablePropertyId",
                schema: "am",
                table: "PhysicalCountEntries",
                column: "SemiExpendablePropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_SessionId",
                schema: "am",
                table: "PhysicalCountEntries",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_SessionId_PPEItemId",
                schema: "am",
                table: "PhysicalCountEntries",
                columns: new[] { "SessionId", "PPEItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountEntries_SessionId_SemiExpendablePropertyId",
                schema: "am",
                table: "PhysicalCountEntries",
                columns: new[] { "SessionId", "SemiExpendablePropertyId" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountSessions_CountDate",
                schema: "am",
                table: "PhysicalCountSessions",
                column: "CountDate");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountSessions_SessionNo",
                schema: "am",
                table: "PhysicalCountSessions",
                column: "SessionNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalCountSessions_Status",
                schema: "am",
                table: "PhysicalCountSessions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhysicalCountEntries",
                schema: "am");

            migrationBuilder.DropTable(
                name: "PhysicalCountSessions",
                schema: "am");
        }
    }
}
