using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Finance
{
    /// <inheritdoc />
    public partial class Finance_AddNumberSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BurNumberSequences",
                schema: "finance",
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
                name: "DvNumberSequences",
                schema: "finance",
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

            migrationBuilder.CreateIndex(
                name: "IX_BurNumberSequences_Year",
                schema: "finance",
                table: "BurNumberSequences",
                column: "Year",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DvNumberSequences_Year",
                schema: "finance",
                table: "DvNumberSequences",
                column: "Year",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BurNumberSequences",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "DvNumberSequences",
                schema: "finance");
        }
    }
}
