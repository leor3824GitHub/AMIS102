using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Expendable
{
    /// <inheritdoc />
    public partial class Expendable_ConcurrencyXminTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Replaces the hand-rolled bytea 'Version' concurrency token on the transactional aggregates
            // with the Postgres system column 'xmin' (mirrors AssetRegistry/Product). Only the physical
            // 'Version' columns are dropped here.
            migrationBuilder.DropColumn(
                name: "Version",
                schema: "expendable",
                table: "SupplyRequests");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "expendable",
                table: "ProductInventory");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "expendable",
                table: "EmployeeShoppingCarts");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "expendable",
                table: "EmployeeInventory");

            // NOTE: 'xmin' is a Postgres system column that already exists on every table — it is the
            // optimistic-concurrency token (mapped in the *Configuration classes) and requires no DDL. EF
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
                schema: "expendable",
                table: "SupplyRequests",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "expendable",
                table: "ProductInventory",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "expendable",
                table: "EmployeeShoppingCarts",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "expendable",
                table: "EmployeeInventory",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
