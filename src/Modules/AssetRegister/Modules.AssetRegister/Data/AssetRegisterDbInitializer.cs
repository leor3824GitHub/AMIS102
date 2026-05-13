using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetRegister.Data;

internal sealed class AssetRegisterDbInitializer(
    ILogger<AssetRegisterDbInitializer> logger,
    AssetRegisterDbContext context) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] applied database migrations for asset register module", context.TenantInfo?.Identifier);
        }
    }

    public Task SeedAsync(CancellationToken cancellationToken)
    {
        return SeedCatalogAsync(cancellationToken);
    }

    private async Task SeedCatalogAsync(CancellationToken cancellationToken)
    {
        if (bool.Parse(Environment.GetEnvironmentVariable("DISABLE_ASSETREGISTER_SEEDING") ?? "false"))
        {
            logger.LogInformation("[{Tenant}] asset register seeding disabled by environment variable", context.TenantInfo?.Identifier);
            return;
        }

        var tenantId = context.TenantInfo?.Identifier ?? MultitenancyConstants.Root.Id;

        var hasCatalog = await context.PropertyItemCatalogs
            .IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false);

        if (hasCatalog)
        {
            return;
        }

        var seedItems = new[]
        {
            PropertyItemCatalog.Create(tenantId, "AR-SE-CHAIR", "Office Chair", "FURN", "SE-FURN", "piece", "50203010", 5),
            PropertyItemCatalog.Create(tenantId, "AR-SE-TABLE", "Office Table", "FURN", "SE-FURN", "piece", "50203010", 10),
            PropertyItemCatalog.Create(tenantId, "AR-SE-LAPTOP", "Laptop Computer", "ICT", "SE-ICT", "unit", "50215030", 3),
            PropertyItemCatalog.Create(tenantId, "AR-PPE-DESKTOP", "Desktop Computer Set", "ICT", "PPE-ICT", "set", "10605030", 5),
            PropertyItemCatalog.Create(tenantId, "AR-PPE-PRINTER", "Network Printer", "ICT", "PPE-ICT", "unit", "10605030", 5),
            PropertyItemCatalog.Create(tenantId, "AR-PPE-AIRCON", "Split-type Air Conditioner", "EQUIP", "PPE-EQ", "unit", "10604010", 10)
        };

        await context.PropertyItemCatalogs.AddRangeAsync(seedItems, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("[{Tenant}] seeded asset register property item catalog with {Count} defaults", tenantId, seedItems.Length);
    }
}

