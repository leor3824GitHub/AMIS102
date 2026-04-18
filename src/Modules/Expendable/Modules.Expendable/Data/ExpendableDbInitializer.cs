using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FSH.Modules.Expendable.Domain.Products;
using FSH.Framework.Shared.Multitenancy;

namespace FSH.Modules.Expendable.Data;

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
            // Backfill empty/null Version bytes for all concurrency-token columns so EF WHERE checks always match
            foreach (var table in new[] { "Products", "ProductInventory", "SupplyRequests", "EmployeeInventory", "Purchases", "RejectedInventory" })
            {
                await context.Database.ExecuteSqlRawAsync(
                    $"UPDATE expendable.\"{table}\" SET \"Version\" = gen_random_bytes(8) WHERE \"Version\" IS NULL OR \"Version\" = '\\x';",
                    cancellationToken).ConfigureAwait(false);
                await context.Database.ExecuteSqlRawAsync(
                    $"ALTER TABLE expendable.\"{table}\" ALTER COLUMN \"Version\" SET DEFAULT gen_random_bytes(8);",
                    cancellationToken).ConfigureAwait(false);
            }
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

            var products = new[]
            {
                Product.Create(tenantId, "PRD-001", "Bond Paper A4", "High quality A4 bond paper, 80gsm", 5.50m, "UOM-RIM", 10, 50, imageUrl: "/images/products/product1.svg"),
                Product.Create(tenantId, "PRD-002", "Ink Cartridge Black", "Black ink cartridge for model X", 12.99m, "UOM-PCS", 5, 20, imageUrl: "/images/products/product2.svg"),
                Product.Create(tenantId, "PRD-003", "Stapler", "Standard office stapler", 7.25m, "UOM-PCS", 5, 15, imageUrl: "/images/products/product3.svg"),
                Product.Create(tenantId, "PRD-004", "Notebook A5", "Ruled A5 notebook, 80 pages", 3.75m, "UOM-PCS", 20, 100, imageUrl: "/images/products/product4.svg"),
                Product.Create(tenantId, "PRD-005", "Ballpoint Pen (Blue)", "Smooth-writing blue ballpoint pen", 0.99m, "UOM-PCS", 50, 200, imageUrl: "/images/products/product5.svg"),
                Product.Create(tenantId, "PRD-006", "Calculator", "Basic desktop calculator", 15.00m, "UOM-PCS", 5, 25, imageUrl: "/images/products/product6.svg"),
                Product.Create(tenantId, "PRD-007", "Packing Tape", "Clear packing tape 48mm x 50m", 4.50m, "UOM-BOX", 30, 120, imageUrl: "/images/products/product7.svg"),
                Product.Create(tenantId, "PRD-008", "USB Flash Drive 32GB", "32GB USB-A flash drive", 9.99m, "UOM-PCS", 10, 40, imageUrl: "/images/products/product8.svg"),
                Product.Create(tenantId, "PRD-009", "Desk Lamp", "LED desk lamp with adjustable arm", 29.99m, "UOM-PCS", 3, 10, imageUrl: "/images/products/product9.svg"),
                Product.Create(tenantId, "PRD-010", "Whiteboard Marker (Black)", "Dry erase marker, black", 1.25m, "UOM-PCS", 40, 160, imageUrl: "/images/products/product10.svg"),
            };

            await context.Products.AddRangeAsync(products, cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("[{Tenant}] seeded expendable products.", context.TenantInfo?.Identifier);
    }
}
