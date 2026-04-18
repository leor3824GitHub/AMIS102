using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.MasterData
{
    /// <inheritdoc />
    public partial class AddOfficeCodeToMasterDataEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeeProfiles_TenantId_DepartmentId",
                schema: "masterdata",
                table: "EmployeeProfiles");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProfiles_TenantId_EmployeeNumber",
                schema: "masterdata",
                table: "EmployeeProfiles");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProfiles_TenantId_IdentityUserId",
                schema: "masterdata",
                table: "EmployeeProfiles");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProfiles_TenantId_OfficeId",
                schema: "masterdata",
                table: "EmployeeProfiles");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProfiles_TenantId_PositionId",
                schema: "masterdata",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "masterdata",
                table: "EmployeeProfiles");

            migrationBuilder.AddColumn<string>(
                name: "OfficeCode",
                schema: "masterdata",
                table: "UnitOfMeasures",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficeCode",
                schema: "masterdata",
                table: "Suppliers",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficeCode",
                schema: "masterdata",
                table: "Positions",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficeCode",
                schema: "masterdata",
                table: "Offices",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficeCode",
                schema: "masterdata",
                table: "EmployeeProfiles",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficeCode",
                schema: "masterdata",
                table: "Departments",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficeCode",
                schema: "masterdata",
                table: "Categories",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitOfMeasures_OfficeCode",
                schema: "masterdata",
                table: "UnitOfMeasures",
                column: "OfficeCode");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_OfficeCode",
                schema: "masterdata",
                table: "Suppliers",
                column: "OfficeCode");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_OfficeCode",
                schema: "masterdata",
                table: "Positions",
                column: "OfficeCode");

            migrationBuilder.CreateIndex(
                name: "IX_Offices_OfficeCode",
                schema: "masterdata",
                table: "Offices",
                column: "OfficeCode");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_EmployeeNumber",
                schema: "masterdata",
                table: "EmployeeProfiles",
                column: "EmployeeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_IdentityUserId",
                schema: "masterdata",
                table: "EmployeeProfiles",
                column: "IdentityUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_OfficeCode",
                schema: "masterdata",
                table: "EmployeeProfiles",
                column: "OfficeCode");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_OfficeCode",
                schema: "masterdata",
                table: "Departments",
                column: "OfficeCode");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_OfficeCode",
                schema: "masterdata",
                table: "Categories",
                column: "OfficeCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnitOfMeasures_OfficeCode",
                schema: "masterdata",
                table: "UnitOfMeasures");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_OfficeCode",
                schema: "masterdata",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Positions_OfficeCode",
                schema: "masterdata",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Offices_OfficeCode",
                schema: "masterdata",
                table: "Offices");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProfiles_EmployeeNumber",
                schema: "masterdata",
                table: "EmployeeProfiles");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProfiles_IdentityUserId",
                schema: "masterdata",
                table: "EmployeeProfiles");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProfiles_OfficeCode",
                schema: "masterdata",
                table: "EmployeeProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Departments_OfficeCode",
                schema: "masterdata",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Categories_OfficeCode",
                schema: "masterdata",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "OfficeCode",
                schema: "masterdata",
                table: "UnitOfMeasures");

            migrationBuilder.DropColumn(
                name: "OfficeCode",
                schema: "masterdata",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "OfficeCode",
                schema: "masterdata",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "OfficeCode",
                schema: "masterdata",
                table: "Offices");

            migrationBuilder.DropColumn(
                name: "OfficeCode",
                schema: "masterdata",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "OfficeCode",
                schema: "masterdata",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "OfficeCode",
                schema: "masterdata",
                table: "Categories");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "masterdata",
                table: "EmployeeProfiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_TenantId_DepartmentId",
                schema: "masterdata",
                table: "EmployeeProfiles",
                columns: new[] { "TenantId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_TenantId_EmployeeNumber",
                schema: "masterdata",
                table: "EmployeeProfiles",
                columns: new[] { "TenantId", "EmployeeNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_TenantId_IdentityUserId",
                schema: "masterdata",
                table: "EmployeeProfiles",
                columns: new[] { "TenantId", "IdentityUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_TenantId_OfficeId",
                schema: "masterdata",
                table: "EmployeeProfiles",
                columns: new[] { "TenantId", "OfficeId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_TenantId_PositionId",
                schema: "masterdata",
                table: "EmployeeProfiles",
                columns: new[] { "TenantId", "PositionId" });
        }
    }
}
