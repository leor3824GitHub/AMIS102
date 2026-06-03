using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.ProcurementAcquisition
{
    /// <inheritdoc />
    public partial class ProcurementAcquisition_RenameInspectionAcceptanceReportPermissionClaims : Migration
    {
        // Data-only migration. The IAR permission keys were renamed from
        // "Permissions.Procurement.AssetIARs.*" to "Permissions.Procurement.InspectionAcceptanceReports.*".
        // Existing role grants live in identity."RoleClaims" (same physical database), so this migration
        // renames them in place. It is intentionally order-independent w.r.t. the startup seeder: step 1
        // removes any new-form rows the seeder may have already added for a role, step 2 renames the
        // legacy rows — leaving exactly one claim per role, with no duplicates and no orphans.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM identity."RoleClaims" legacy
                WHERE legacy."ClaimType" = 'permission'
                  AND legacy."ClaimValue" LIKE 'Permissions.Procurement.AssetIARs.%'
                  AND EXISTS (
                      SELECT 1 FROM identity."RoleClaims" renamed
                      WHERE renamed."RoleId" = legacy."RoleId"
                        AND renamed."ClaimType" = 'permission'
                        AND renamed."ClaimValue" = replace(
                              legacy."ClaimValue",
                              'Permissions.Procurement.AssetIARs.',
                              'Permissions.Procurement.InspectionAcceptanceReports.'));

                UPDATE identity."RoleClaims"
                SET "ClaimValue" = replace(
                      "ClaimValue",
                      'Permissions.Procurement.AssetIARs.',
                      'Permissions.Procurement.InspectionAcceptanceReports.')
                WHERE "ClaimType" = 'permission'
                  AND "ClaimValue" LIKE 'Permissions.Procurement.AssetIARs.%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE identity."RoleClaims"
                SET "ClaimValue" = replace(
                      "ClaimValue",
                      'Permissions.Procurement.InspectionAcceptanceReports.',
                      'Permissions.Procurement.AssetIARs.')
                WHERE "ClaimType" = 'permission'
                  AND "ClaimValue" LIKE 'Permissions.Procurement.InspectionAcceptanceReports.%';
                """);
        }
    }
}
