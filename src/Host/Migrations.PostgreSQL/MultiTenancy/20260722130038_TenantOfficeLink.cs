using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.MultiTenancy
{
    /// <inheritdoc />
    public partial class TenantOfficeLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OfficeCode",
                schema: "tenant",
                table: "Tenants",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OfficeId",
                schema: "tenant",
                table: "Tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_OfficeId",
                schema: "tenant",
                table: "Tenants",
                column: "OfficeId",
                unique: true,
                filter: "\"OfficeId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_OfficeId",
                schema: "tenant",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "OfficeCode",
                schema: "tenant",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "OfficeId",
                schema: "tenant",
                table: "Tenants");
        }
    }
}
