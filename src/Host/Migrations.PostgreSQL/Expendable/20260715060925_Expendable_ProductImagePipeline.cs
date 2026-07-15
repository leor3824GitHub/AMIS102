using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.Expendable
{
    /// <inheritdoc />
    public partial class Expendable_ProductImagePipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                schema: "expendable",
                table: "Products");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                schema: "expendable",
                table: "Products",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10000000)",
                oldMaxLength: 10000000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                schema: "expendable",
                table: "Products",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            // NOTE: 'xmin' is a Postgres system column that already exists on every table — it is the
            // optimistic-concurrency token (mapped in ProductConfiguration) and requires no DDL. EF
            // scaffolds an AddColumn for it, but issuing 'ADD COLUMN xmin' fails ("conflicts with a
            // system column"). The model snapshot still carries the property; only the physical op is
            // removed here. (Matches how AssetRegister's xmin is a no-op at the SQL layer.)

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_CategoryId",
                schema: "expendable",
                table: "Products",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_Name",
                schema: "expendable",
                table: "Products",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_SupplierId",
                schema: "expendable",
                table: "Products",
                columns: new[] { "TenantId", "SupplierId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_CategoryId",
                schema: "expendable",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_Name",
                schema: "expendable",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_SupplierId",
                schema: "expendable",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                schema: "expendable",
                table: "Products");

            // 'xmin' is a system column — never physically added, so nothing to drop here (see Up).

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                schema: "expendable",
                table: "Products",
                type: "character varying(10000000)",
                maxLength: 10000000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "expendable",
                table: "Products",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
