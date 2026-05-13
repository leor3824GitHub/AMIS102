using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetManagement.Data;

internal sealed class AssetManagementDbInitializer(
    ILogger<AssetManagementDbInitializer> logger,
    AssetManagementDbContext context) : IDbInitializer
{
    private static readonly string[] CatalogPrefixes =
    [
        "Office",
        "Industrial",
        "Digital",
        "Portable",
        "Heavy-Duty",
        "Ergonomic",
        "Network",
        "Modular",
        "Compact",
        "Executive"
    ];

    private static readonly string[] CatalogItemTypes =
    [
        "Laptop",
        "Desktop Computer",
        "Printer",
        "Scanner",
        "Projector",
        "File Cabinet",
        "Office Chair",
        "Conference Table",
        "Air Conditioner",
        "Generator",
        "UPS",
        "Server Rack",
        "Monitor",
        "Network Switch",
        "Document Camera"
    ];

    private static readonly string[] Units = ["pc", "set", "unit", "pair"];

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("[{Tenant}] applied database migrations for asset management module", context.TenantInfo?.Identifier);
            }
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (bool.Parse(Environment.GetEnvironmentVariable("DISABLE_ASSETMANAGEMENT_SEEDING") ?? "false"))
        {
            logger.LogInformation("[{Tenant}] asset management seeding disabled by environment variable.", context.TenantInfo?.Identifier);
            return;
        }

        if (!await context.PropertyItemCatalog.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var tenantId = context.TenantInfo?.Identifier ?? MultitenancyConstants.Root.Id;
            var random = new Random();
            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var items = new List<PropertyItemCatalog>(capacity: 30);

            while (items.Count < 30)
            {
                var code = $"PIC-{random.Next(1000, 9999)}";
                if (!codes.Add(code))
                {
                    continue;
                }

                var name = $"{CatalogPrefixes[random.Next(CatalogPrefixes.Length)]} {CatalogItemTypes[random.Next(CatalogItemTypes.Length)]}";
                var uacs = $"1.{random.Next(1, 99):00}.{random.Next(1, 99):00}";
                var unit = Units[random.Next(Units.Length)];
                int? usefulLife = random.NextDouble() < 0.8 ? random.Next(2, 16) : null;

                var item = PropertyItemCatalog.Create(
                    tenantId,
                    code,
                    name,
                    $"Seeded random catalog item for {name.ToLowerInvariant()}.",
                    uacs,
                    unit,
                    usefulLife);

                item.CreatedBy = "seed-random";
                items.Add(item);
            }

            await context.PropertyItemCatalog.AddRangeAsync(items, cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] seeded {Count} random property item catalog records.", context.TenantInfo?.Identifier, items.Count);
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("[{Tenant}] seeded asset management data.", context.TenantInfo?.Identifier);
        }
    }
}

