using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMIS.Playground.Migrations.PostgreSQL.AssetRegister
{
    /// <inheritdoc />
    public partial class AssetRegister_AddTenantIdToChildren : Migration
    {
        // Denormalize TenantId onto every child row so Finbuckle's global query filter
        // applies on direct queries (defense-in-depth — children are normally reached
        // via parent navigation, but a `Set<PropertyAccountabilityLine>()` style query
        // would otherwise leak rows across tenants).
        //
        // Pattern per table: ADD COLUMN nullable → backfill from parent → SET NOT NULL.
        // Works correctly whether the table is empty (pre-prod) or already populated.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddTenantIdViaParent(
                migrationBuilder,
                child: "PropertyAccountabilityLines",
                parent: "PropertyAccountabilities",
                childFk: "AccountabilityId");

            AddTenantIdViaParent(
                migrationBuilder,
                child: "PhysicalCountEntries",
                parent: "PhysicalCountSessions",
                childFk: "SessionId");

            AddTenantIdViaParent(
                migrationBuilder,
                child: "PropertyIncidentItems",
                parent: "PropertyIncidentReports",
                childFk: "ReportId");

            AddTenantIdViaParent(
                migrationBuilder,
                child: "PropertyIssuanceReportLines",
                parent: "PropertyIssuanceReports",
                childFk: "ReportId");

            AddTenantIdViaParent(
                migrationBuilder,
                child: "ReceivingReportItems",
                parent: "ReceivingReports",
                childFk: "ReportId");

            AddTenantIdViaParent(
                migrationBuilder,
                child: "UnserviceablePropertyItems",
                parent: "UnserviceablePropertyReports",
                childFk: "ReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "asset_register",
                table: "UnserviceablePropertyItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "asset_register",
                table: "ReceivingReportItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "asset_register",
                table: "PropertyIssuanceReportLines");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "asset_register",
                table: "PropertyIncidentItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "asset_register",
                table: "PropertyAccountabilityLines");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "asset_register",
                table: "PhysicalCountEntries");
        }

        private static void AddTenantIdViaParent(
            MigrationBuilder migrationBuilder,
            string child,
            string parent,
            string childFk)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "asset_register",
                table: child,
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql(
                $@"UPDATE asset_register.""{child}"" c
                   SET ""TenantId"" = p.""TenantId""
                   FROM asset_register.""{parent}"" p
                   WHERE c.""{childFk}"" = p.""Id"";");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                schema: "asset_register",
                table: child,
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
