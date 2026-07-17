using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.BudgetDisbursement
{
    /// <inheritdoc />
    public partial class UnifyNumberSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create the unified counter table (replaces DvNumberSequences / BurNumberSequences).
            migrationBuilder.CreateTable(
                name: "NumberSequences",
                schema: "budgetdisbursement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SequenceKey = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    LastSerial = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumberSequences", x => x.Id);
                });

            // 2. Copy live counters across, preserving LastSerial so numbering continues without a reset.
            //    DV/BUR are global year-only sequences: TenantId = '' and Month = 0. Fresh Ids are minted;
            //    xmin is a system column managed by PostgreSQL.
            migrationBuilder.Sql(@"
INSERT INTO budgetdisbursement.""NumberSequences"" (""Id"", ""TenantId"", ""SequenceKey"", ""Year"", ""Month"", ""LastSerial"")
SELECT gen_random_uuid(), '', 'DV', ""Year"", 0, ""LastSerial"" FROM budgetdisbursement.""DvNumberSequences"";");

            migrationBuilder.Sql(@"
INSERT INTO budgetdisbursement.""NumberSequences"" (""Id"", ""TenantId"", ""SequenceKey"", ""Year"", ""Month"", ""LastSerial"")
SELECT gen_random_uuid(), '', 'BUR', ""Year"", 0, ""LastSerial"" FROM budgetdisbursement.""BurNumberSequences"";");

            // 3. Enforce one row per (TenantId, SequenceKey, Year, Month).
            migrationBuilder.CreateIndex(
                name: "IX_NumberSequences_TenantId_SequenceKey_Year_Month",
                schema: "budgetdisbursement",
                table: "NumberSequences",
                columns: new[] { "TenantId", "SequenceKey", "Year", "Month" },
                unique: true);

            // 4. Drop the superseded per-document counter tables now their data has been copied.
            migrationBuilder.DropTable(name: "BurNumberSequences", schema: "budgetdisbursement");
            migrationBuilder.DropTable(name: "DvNumberSequences", schema: "budgetdisbursement");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Recreate the per-document counter tables.
            migrationBuilder.CreateTable(
                name: "BurNumberSequences",
                schema: "budgetdisbursement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LastSerial = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BurNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DvNumberSequences",
                schema: "budgetdisbursement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LastSerial = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DvNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BurNumberSequences_Year",
                schema: "budgetdisbursement",
                table: "BurNumberSequences",
                column: "Year",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DvNumberSequences_Year",
                schema: "budgetdisbursement",
                table: "DvNumberSequences",
                column: "Year",
                unique: true);

            // 2. Copy counters back into their per-document tables, filtered by SequenceKey (dropping the
            //    global TenantId/Month, which those tables don't carry).
            migrationBuilder.Sql(@"
INSERT INTO budgetdisbursement.""DvNumberSequences"" (""Id"", ""Year"", ""LastSerial"")
SELECT gen_random_uuid(), ""Year"", ""LastSerial"" FROM budgetdisbursement.""NumberSequences"" WHERE ""SequenceKey"" = 'DV';");

            migrationBuilder.Sql(@"
INSERT INTO budgetdisbursement.""BurNumberSequences"" (""Id"", ""Year"", ""LastSerial"")
SELECT gen_random_uuid(), ""Year"", ""LastSerial"" FROM budgetdisbursement.""NumberSequences"" WHERE ""SequenceKey"" = 'BUR';");

            // 3. Drop the unified table.
            migrationBuilder.DropTable(name: "NumberSequences", schema: "budgetdisbursement");
        }
    }
}
