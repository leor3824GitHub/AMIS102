using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.MasterData;

/// <inheritdoc />
public partial class AddSupplierTaxFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "BusinessTaxType",
            schema: "employee",
            table: "Suppliers",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "NON-VAT");

        migrationBuilder.AddColumn<string>(
            name: "TinNo",
            schema: "employee",
            table: "Suppliers",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "BusinessTaxType",
            schema: "employee",
            table: "Suppliers");

        migrationBuilder.DropColumn(
            name: "TinNo",
            schema: "employee",
            table: "Suppliers");
    }
}
