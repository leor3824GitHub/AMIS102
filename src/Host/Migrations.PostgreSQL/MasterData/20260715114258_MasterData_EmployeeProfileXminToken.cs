using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.MasterData
{
    /// <inheritdoc />
    public partial class MasterData_EmployeeProfileXminToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Replaces EmployeeProfile's hand-rolled bytea 'Version' token with the Postgres system
            // column 'xmin' (mirrors AssetRegistry/Product). Only the physical 'Version' column is dropped.
            migrationBuilder.DropColumn(
                name: "Version",
                schema: "masterdata",
                table: "EmployeeProfiles");

            // NOTE: 'xmin' is a Postgres system column that already exists on every table — it is the
            // optimistic-concurrency token (mapped in EmployeeProfileConfiguration) and requires no DDL. EF
            // scaffolds an AddColumn for it, but issuing 'ADD COLUMN xmin' fails ("conflicts with a
            // system column"). The model snapshot still carries the property; only the physical op is
            // removed here. (Matches how AssetRegister's/Product's xmin is a no-op at the SQL layer.)
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 'xmin' is a system column — never physically added, so nothing to drop here (see Up).
            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "masterdata",
                table: "EmployeeProfiles",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
