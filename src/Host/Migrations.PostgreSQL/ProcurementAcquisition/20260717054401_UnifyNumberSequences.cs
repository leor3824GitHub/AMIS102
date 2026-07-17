using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementAcquisition
{
    /// <inheritdoc />
    public partial class UnifyNumberSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create the unified counter table (replaces PrNumberSequences / PoNumberSequences /
            //    JoNumberSequences / RivNumberSequences / IarNumberSequences).
            migrationBuilder.CreateTable(
                name: "NumberSequences",
                schema: "procurement",
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
            //    Year-only sequences map to Month = 0; month-scoped sequences (PO, JO) carry their Month.
            //    Fresh Ids are minted; xmin is a system column and is managed by PostgreSQL.
            migrationBuilder.Sql(@"
INSERT INTO procurement.""NumberSequences"" (""Id"", ""TenantId"", ""SequenceKey"", ""Year"", ""Month"", ""LastSerial"")
SELECT gen_random_uuid(), ""TenantId"", 'PR', ""Year"", 0, ""LastSerial"" FROM procurement.""PrNumberSequences"";");

            migrationBuilder.Sql(@"
INSERT INTO procurement.""NumberSequences"" (""Id"", ""TenantId"", ""SequenceKey"", ""Year"", ""Month"", ""LastSerial"")
SELECT gen_random_uuid(), ""TenantId"", 'PO', ""Year"", ""Month"", ""LastSerial"" FROM procurement.""PoNumberSequences"";");

            migrationBuilder.Sql(@"
INSERT INTO procurement.""NumberSequences"" (""Id"", ""TenantId"", ""SequenceKey"", ""Year"", ""Month"", ""LastSerial"")
SELECT gen_random_uuid(), ""TenantId"", 'JO', ""Year"", ""Month"", ""LastSerial"" FROM procurement.""JoNumberSequences"";");

            migrationBuilder.Sql(@"
INSERT INTO procurement.""NumberSequences"" (""Id"", ""TenantId"", ""SequenceKey"", ""Year"", ""Month"", ""LastSerial"")
SELECT gen_random_uuid(), ""TenantId"", 'RIV', ""Year"", 0, ""LastSerial"" FROM procurement.""RivNumberSequences"";");

            migrationBuilder.Sql(@"
INSERT INTO procurement.""NumberSequences"" (""Id"", ""TenantId"", ""SequenceKey"", ""Year"", ""Month"", ""LastSerial"")
SELECT gen_random_uuid(), ""TenantId"", 'IAR', ""Year"", 0, ""LastSerial"" FROM procurement.""IarNumberSequences"";");

            // 3. Enforce one row per (TenantId, SequenceKey, Year, Month).
            migrationBuilder.CreateIndex(
                name: "IX_NumberSequences_TenantId_SequenceKey_Year_Month",
                schema: "procurement",
                table: "NumberSequences",
                columns: new[] { "TenantId", "SequenceKey", "Year", "Month" },
                unique: true);

            // 4. Drop the superseded per-document counter tables now their data has been copied.
            migrationBuilder.DropTable(name: "IarNumberSequences", schema: "procurement");
            migrationBuilder.DropTable(name: "JoNumberSequences", schema: "procurement");
            migrationBuilder.DropTable(name: "PoNumberSequences", schema: "procurement");
            migrationBuilder.DropTable(name: "PrNumberSequences", schema: "procurement");
            migrationBuilder.DropTable(name: "RivNumberSequences", schema: "procurement");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Recreate the per-document counter tables.
            migrationBuilder.CreateTable(
                name: "IarNumberSequences",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LastSerial = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IarNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JoNumberSequences",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LastSerial = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JoNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PoNumberSequences",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LastSerial = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrNumberSequences",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LastSerial = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RivNumberSequences",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LastSerial = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RivNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IarNumberSequences_TenantId_Year",
                schema: "procurement",
                table: "IarNumberSequences",
                columns: new[] { "TenantId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JoNumberSequences_TenantId_Year_Month",
                schema: "procurement",
                table: "JoNumberSequences",
                columns: new[] { "TenantId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoNumberSequences_TenantId_Year_Month",
                schema: "procurement",
                table: "PoNumberSequences",
                columns: new[] { "TenantId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrNumberSequences_TenantId_Year",
                schema: "procurement",
                table: "PrNumberSequences",
                columns: new[] { "TenantId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RivNumberSequences_TenantId_Year",
                schema: "procurement",
                table: "RivNumberSequences",
                columns: new[] { "TenantId", "Year" },
                unique: true);

            // 2. Copy counters back into their per-document tables, filtered by SequenceKey.
            migrationBuilder.Sql(@"
INSERT INTO procurement.""PrNumberSequences"" (""Id"", ""TenantId"", ""Year"", ""LastSerial"")
SELECT gen_random_uuid(), ""TenantId"", ""Year"", ""LastSerial"" FROM procurement.""NumberSequences"" WHERE ""SequenceKey"" = 'PR';");

            migrationBuilder.Sql(@"
INSERT INTO procurement.""PoNumberSequences"" (""Id"", ""TenantId"", ""Year"", ""Month"", ""LastSerial"")
SELECT gen_random_uuid(), ""TenantId"", ""Year"", ""Month"", ""LastSerial"" FROM procurement.""NumberSequences"" WHERE ""SequenceKey"" = 'PO';");

            migrationBuilder.Sql(@"
INSERT INTO procurement.""JoNumberSequences"" (""Id"", ""TenantId"", ""Year"", ""Month"", ""LastSerial"")
SELECT gen_random_uuid(), ""TenantId"", ""Year"", ""Month"", ""LastSerial"" FROM procurement.""NumberSequences"" WHERE ""SequenceKey"" = 'JO';");

            migrationBuilder.Sql(@"
INSERT INTO procurement.""RivNumberSequences"" (""Id"", ""TenantId"", ""Year"", ""LastSerial"")
SELECT gen_random_uuid(), ""TenantId"", ""Year"", ""LastSerial"" FROM procurement.""NumberSequences"" WHERE ""SequenceKey"" = 'RIV';");

            migrationBuilder.Sql(@"
INSERT INTO procurement.""IarNumberSequences"" (""Id"", ""TenantId"", ""Year"", ""LastSerial"")
SELECT gen_random_uuid(), ""TenantId"", ""Year"", ""LastSerial"" FROM procurement.""NumberSequences"" WHERE ""SequenceKey"" = 'IAR';");

            // 3. Drop the unified table.
            migrationBuilder.DropTable(name: "NumberSequences", schema: "procurement");
        }
    }
}
