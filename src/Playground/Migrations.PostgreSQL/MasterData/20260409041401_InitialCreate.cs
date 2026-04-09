using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.MasterData
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "employee");

            migrationBuilder.CreateTable(
                name: "CapitalizationThresholds",
                schema: "employee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CircularName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CapitalizationAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SemiExpendableLowValueThreshold = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EffectivityDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapitalizationThresholds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "employee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                schema: "employee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    FundCluster = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Offices",
                schema: "employee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationProfiles",
                schema: "employee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Address = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                schema: "employee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportSignatories",
                schema: "employee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReportType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSignatories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "employee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    ContactPerson = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitOfMeasures",
                schema: "employee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitOfMeasures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeProfiles",
                schema: "employee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EmployeeNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IdentityUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FirstName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LastName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    WorkEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    OfficeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultUnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeProfiles_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "employee",
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeProfiles_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalSchema: "employee",
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeProfiles_Positions_PositionId",
                        column: x => x.PositionId,
                        principalSchema: "employee",
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeProfiles_UnitOfMeasures_DefaultUnitOfMeasureId",
                        column: x => x.DefaultUnitOfMeasureId,
                        principalSchema: "employee",
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CapitalizationThresholds_TenantId",
                schema: "employee",
                table: "CapitalizationThresholds",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CapitalizationThresholds_TenantId_IsActive",
                schema: "employee",
                table: "CapitalizationThresholds",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Code",
                schema: "employee",
                table: "Categories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                schema: "employee",
                table: "Categories",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code",
                schema: "employee",
                table: "Departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Name",
                schema: "employee",
                table: "Departments",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_DefaultUnitOfMeasureId",
                schema: "employee",
                table: "EmployeeProfiles",
                column: "DefaultUnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_DepartmentId",
                schema: "employee",
                table: "EmployeeProfiles",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_OfficeId",
                schema: "employee",
                table: "EmployeeProfiles",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_PositionId",
                schema: "employee",
                table: "EmployeeProfiles",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_TenantId_DepartmentId",
                schema: "employee",
                table: "EmployeeProfiles",
                columns: new[] { "TenantId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_TenantId_EmployeeNumber",
                schema: "employee",
                table: "EmployeeProfiles",
                columns: new[] { "TenantId", "EmployeeNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_TenantId_IdentityUserId",
                schema: "employee",
                table: "EmployeeProfiles",
                columns: new[] { "TenantId", "IdentityUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_TenantId_OfficeId",
                schema: "employee",
                table: "EmployeeProfiles",
                columns: new[] { "TenantId", "OfficeId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_TenantId_PositionId",
                schema: "employee",
                table: "EmployeeProfiles",
                columns: new[] { "TenantId", "PositionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Offices_Code",
                schema: "employee",
                table: "Offices",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offices_Name",
                schema: "employee",
                table: "Offices",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationProfiles_TenantId",
                schema: "employee",
                table: "OrganizationProfiles",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Positions_Code",
                schema: "employee",
                table: "Positions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Positions_Name",
                schema: "employee",
                table: "Positions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSignatories_TenantId_ReportType",
                schema: "employee",
                table: "ReportSignatories",
                columns: new[] { "TenantId", "ReportType" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportSignatories_TenantId_ReportType_SortOrder",
                schema: "employee",
                table: "ReportSignatories",
                columns: new[] { "TenantId", "ReportType", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Code",
                schema: "employee",
                table: "Suppliers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Name",
                schema: "employee",
                table: "Suppliers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_UnitOfMeasures_Code",
                schema: "employee",
                table: "UnitOfMeasures",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitOfMeasures_Name",
                schema: "employee",
                table: "UnitOfMeasures",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CapitalizationThresholds",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "EmployeeProfiles",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "OrganizationProfiles",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "ReportSignatories",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "Departments",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "Offices",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "Positions",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "UnitOfMeasures",
                schema: "employee");
        }
    }
}
