using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.AssetManagement
{
    /// <inheritdoc />
    public partial class AddPropertyClassAndCategoryCodeToTangibleItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategoryCode",
                schema: "am",
                table: "TangibleItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PropertyClass",
                schema: "am",
                table: "TangibleItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TangibleItems_TenantId_PropertyClass_CategoryCode",
                schema: "am",
                table: "TangibleItems",
                columns: new[] { "TenantId", "PropertyClass", "CategoryCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TangibleItems_TenantId_PropertyClass_CategoryCode",
                schema: "am",
                table: "TangibleItems");

            migrationBuilder.DropColumn(
                name: "CategoryCode",
                schema: "am",
                table: "TangibleItems");

            migrationBuilder.DropColumn(
                name: "PropertyClass",
                schema: "am",
                table: "TangibleItems");
        }
    }
}
