using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.AssetManagement
{
    /// <inheritdoc />
    public partial class LinkSMRRItemsToTangibleItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- am.SMRRItems: swap SemiExpendablePropertyId → TangibleItemId ---

            migrationBuilder.DropIndex(
                name: "IX_SMRRItems_SemiExpendablePropertyId",
                schema: "am",
                table: "SMRRItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SMRRItems_SemiExpendableProperties_SemiExpendablePropertyId",
                schema: "am",
                table: "SMRRItems");

            migrationBuilder.DropColumn(
                name: "SemiExpendablePropertyId",
                schema: "am",
                table: "SMRRItems");

            migrationBuilder.AddColumn<Guid>(
                name: "TangibleItemId",
                schema: "am",
                table: "SMRRItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddForeignKey(
                name: "FK_SMRRItems_TangibleItems_TangibleItemId",
                schema: "am",
                table: "SMRRItems",
                column: "TangibleItemId",
                principalSchema: "am",
                principalTable: "TangibleItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.CreateIndex(
                name: "IX_SMRRItems_TangibleItemId",
                schema: "am",
                table: "SMRRItems",
                column: "TangibleItemId",
                unique: true);

            // --- am.TangibleItems: add soft back-reference column ---

            migrationBuilder.AddColumn<Guid>(
                name: "SMRRItemId",
                schema: "am",
                table: "TangibleItems",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // --- am.TangibleItems: remove soft back-reference column ---

            migrationBuilder.DropColumn(
                name: "SMRRItemId",
                schema: "am",
                table: "TangibleItems");

            // --- am.SMRRItems: restore SemiExpendablePropertyId ---

            migrationBuilder.DropIndex(
                name: "IX_SMRRItems_TangibleItemId",
                schema: "am",
                table: "SMRRItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SMRRItems_TangibleItems_TangibleItemId",
                schema: "am",
                table: "SMRRItems");

            migrationBuilder.DropColumn(
                name: "TangibleItemId",
                schema: "am",
                table: "SMRRItems");

            migrationBuilder.AddColumn<Guid>(
                name: "SemiExpendablePropertyId",
                schema: "am",
                table: "SMRRItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddForeignKey(
                name: "FK_SMRRItems_SemiExpendableProperties_SemiExpendablePropertyId",
                schema: "am",
                table: "SMRRItems",
                column: "SemiExpendablePropertyId",
                principalSchema: "am",
                principalTable: "SemiExpendableProperties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.CreateIndex(
                name: "IX_SMRRItems_SemiExpendablePropertyId",
                schema: "am",
                table: "SMRRItems",
                column: "SemiExpendablePropertyId",
                unique: true);
        }
    }
}
