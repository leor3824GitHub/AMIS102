using AMIS.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AMIS.Modules.Expendable.Domain.Products;
using AMIS.Framework.Shared.Multitenancy;

namespace AMIS.Modules.Expendable.Data;

/// <summary>
/// Initializes the Expendable module database context.
/// Handles migrations and seeding for the expendable business domain.
/// </summary>
internal sealed class ExpendableDbInitializer(
    ILogger<ExpendableDbInitializer> logger,
    ExpendableDbContext context) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] applied database migrations for expendable module", context.TenantInfo?.Identifier);
        }

        // Ensure Postgres pgcrypto and Version defaults are set so row-version bytea is never NULL
        try
        {
            await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS pgcrypto;", cancellationToken).ConfigureAwait(false);
            // Backfill empty/null Version bytes for every bytea row-version column so EF WHERE checks always match.
            // Discover tables dynamically so the list never drifts as entities are added/removed.
            // Constrain to bytea columns: an unrelated integer column literally named "Version" (e.g. a stray
            // ProductInventoryBatches column) would otherwise fail casting the '\x' bytea literal to integer.
            await context.Database.ExecuteSqlRawAsync(
                """
                DO $$
                DECLARE r record;
                BEGIN
                    FOR r IN
                        SELECT table_name FROM information_schema.columns
                        WHERE table_schema = 'expendable' AND column_name = 'Version' AND data_type = 'bytea'
                    LOOP
                        EXECUTE format('UPDATE expendable.%I SET "Version" = gen_random_bytes(8) WHERE "Version" IS NULL OR "Version" = ''\x'';', r.table_name);
                        EXECUTE format('ALTER TABLE expendable.%I ALTER COLUMN "Version" SET DEFAULT gen_random_bytes(8);', r.table_name);
                    END LOOP;
                END $$;
                """,
                cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] ensured Version defaults for all expendable tables.", context.TenantInfo?.Identifier);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[{Tenant}] could not ensure Version defaults (non-fatal).", context.TenantInfo?.Identifier);
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (bool.Parse(Environment.GetEnvironmentVariable("DISABLE_EXPENDABLE_SEEDING") ?? "false"))
        {
            logger.LogInformation("[{Tenant}] product seeding disabled by environment variable.", context.TenantInfo?.Identifier);
            return;
        }

        // Seed products with images (10 sample products)
        // Use IgnoreQueryFilters to avoid multi-tenant query filters that rely on a TenantInfo being present during startup initialization
        if (!await context.Products.IgnoreQueryFilters().AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var tenantId = context.TenantInfo?.Identifier ?? MultitenancyConstants.Root.Id;

            // Seeded without photos — images are now stored as files via ProductImageStorage, not
            // wwwroot paths. The UI falls back to an avatar/placeholder when a product has no image.
            var products = new[]
            {
                Product.Create(tenantId, "PRD-001", "Paper", "Bond Paper A4", "High quality A4 bond paper, 80gsm", 5.50m, "RIM", 10, 50),
                Product.Create(tenantId, "PRD-002", "Ink Cartridge", "Ink Cartridge Black", "Black ink cartridge for model X", 12.99m, "PCS", 5, 20),
                Product.Create(tenantId, "PRD-003", "Stapler", "Stapler", "Standard office stapler", 7.25m, "PCS", 5, 15),
                Product.Create(tenantId, "PRD-004", "Notebook", "Notebook A5", "Ruled A5 notebook, 80 pages", 3.75m, "PCS", 20, 100),
                Product.Create(tenantId, "PRD-005", "Pen", "Ballpoint Pen (Blue)", "Smooth-writing blue ballpoint pen", 0.99m, "PCS", 50, 200),
                Product.Create(tenantId, "PRD-006", "Calculator", "Calculator", "Basic desktop calculator", 15.00m, "PCS", 5, 25),
                Product.Create(tenantId, "PRD-007", "Tape", "Packing Tape", "Clear packing tape 48mm x 50m", 4.50m, "BOX", 30, 120),
                Product.Create(tenantId, "PRD-008", "Flash Drive", "USB Flash Drive 32GB", "32GB USB-A flash drive", 9.99m, "PCS", 10, 40),
                Product.Create(tenantId, "PRD-009", "Lamp", "Desk Lamp", "LED desk lamp with adjustable arm", 29.99m, "PCS", 3, 10),
                Product.Create(tenantId, "PRD-010", "Marker", "Whiteboard Marker (Black)", "Dry erase marker, black", 1.25m, "PCS", 40, 160),
            };

            await context.Products.AddRangeAsync(products, cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("[{Tenant}] seeded expendable products.", context.TenantInfo?.Identifier);
    }
}

