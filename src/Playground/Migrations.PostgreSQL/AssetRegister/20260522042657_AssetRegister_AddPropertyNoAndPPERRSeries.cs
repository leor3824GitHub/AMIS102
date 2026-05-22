using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class AssetRegister_AddPropertyNoAndPPERRSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add PropertyNo to existing ReceivingReportItems rows.
            // Idempotent: IF NOT EXISTS guards against re-running on a database that already
            // has the column from a prior manual or partial migration run.
            migrationBuilder.Sql(@"
                ALTER TABLE asset_register.""ReceivingReportItems""
                    ADD COLUMN IF NOT EXISTS ""PropertyNo"" character varying(64) NULL;

                UPDATE asset_register.""ReceivingReportItems""
                    SET ""PropertyNo"" = ''
                    WHERE ""PropertyNo"" IS NULL;

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'asset_register'
                          AND table_name   = 'ReceivingReportItems'
                          AND column_name  = 'PropertyNo'
                          AND is_nullable  = 'YES'
                    ) THEN
                        ALTER TABLE asset_register.""ReceivingReportItems""
                            ALTER COLUMN ""PropertyNo"" SET NOT NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.CreateTable(
                name: "PPERRFormSeries",
                schema: "asset_register",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartSerial = table.Column<int>(type: "integer", nullable: false),
                    EndSerial = table.Column<int>(type: "integer", nullable: false),
                    NextSerial = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PPERRFormSeries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PPERRFormSeries_TenantId_IsActive",
                schema: "asset_register",
                table: "PPERRFormSeries",
                columns: new[] { "TenantId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PPERRFormSeries",
                schema: "asset_register");

            migrationBuilder.DropColumn(
                name: "PropertyNo",
                schema: "asset_register",
                table: "ReceivingReportItems");
        }
    }
}
