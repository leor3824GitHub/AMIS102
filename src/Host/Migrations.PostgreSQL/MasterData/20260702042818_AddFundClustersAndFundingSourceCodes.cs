using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.MasterData
{
    /// <inheritdoc />
    public partial class AddFundClustersAndFundingSourceCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FundClusters",
                schema: "masterdata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_FundClusters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FundingSourceCodes",
                schema: "masterdata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FundClusterCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    FinancingSource = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Authorization = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    FundCategory = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    FundSubCategory = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DepartmentName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    AgencyName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
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
                    table.PrimaryKey("PK_FundingSourceCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FundClusters_Code",
                schema: "masterdata",
                table: "FundClusters",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundingSourceCodes_Code",
                schema: "masterdata",
                table: "FundingSourceCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundingSourceCodes_FundClusterCode",
                schema: "masterdata",
                table: "FundingSourceCodes",
                column: "FundClusterCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundClusters",
                schema: "masterdata");

            migrationBuilder.DropTable(
                name: "FundingSourceCodes",
                schema: "masterdata");
        }
    }
}
