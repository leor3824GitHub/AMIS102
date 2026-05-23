using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class Phase35_AddAdHocSourceFieldsToReceivingReportItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "OriginalAcquisitionDate",
                schema: "asset_register",
                table: "ReceivingReportItems",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceAgencyName",
                schema: "asset_register",
                table: "ReceivingReportItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceDocumentRef",
                schema: "asset_register",
                table: "ReceivingReportItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourcePropertyNo",
                schema: "asset_register",
                table: "ReceivingReportItems",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalAcquisitionDate",
                schema: "asset_register",
                table: "ReceivingReportItems");

            migrationBuilder.DropColumn(
                name: "SourceAgencyName",
                schema: "asset_register",
                table: "ReceivingReportItems");

            migrationBuilder.DropColumn(
                name: "SourceDocumentRef",
                schema: "asset_register",
                table: "ReceivingReportItems");

            migrationBuilder.DropColumn(
                name: "SourcePropertyNo",
                schema: "asset_register",
                table: "ReceivingReportItems");
        }
    }
}
