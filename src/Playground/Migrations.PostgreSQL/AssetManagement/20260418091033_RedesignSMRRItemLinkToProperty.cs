using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.AssetManagement
{
    /// <inheritdoc />
    public partial class RedesignSMRRItemLinkToProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PropertyNo",
                schema: "am",
                table: "SMRRItems",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SemiExpendablePropertyId",
                schema: "am",
                table: "SMRRItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_SMRRItems_SemiExpendablePropertyId",
                schema: "am",
                table: "SMRRItems",
                column: "SemiExpendablePropertyId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SMRRItems_SemiExpendableProperties_SemiExpendablePropertyId",
                schema: "am",
                table: "SMRRItems",
                column: "SemiExpendablePropertyId",
                principalSchema: "am",
                principalTable: "SemiExpendableProperties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SMRRItems_SemiExpendableProperties_SemiExpendablePropertyId",
                schema: "am",
                table: "SMRRItems");

            migrationBuilder.DropIndex(
                name: "IX_SMRRItems_SemiExpendablePropertyId",
                schema: "am",
                table: "SMRRItems");

            migrationBuilder.DropColumn(
                name: "PropertyNo",
                schema: "am",
                table: "SMRRItems");

            migrationBuilder.DropColumn(
                name: "SemiExpendablePropertyId",
                schema: "am",
                table: "SMRRItems");
        }
    }
}
