using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class AssetImageFileStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ImageUrl already exists (20260705060628_AddAssetRegistryImageUrl) as a 10 MB base64 blob
            // column. It now holds a short storage key, so shrink it and add the thumbnail-key column.
            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                schema: "asset_register",
                table: "AssetRegistries",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10000000)",
                oldMaxLength: 10000000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                schema: "asset_register",
                table: "AssetRegistries",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                schema: "asset_register",
                table: "AssetRegistries");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                schema: "asset_register",
                table: "AssetRegistries",
                type: "character varying(10000000)",
                maxLength: 10000000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);
        }
    }
}
