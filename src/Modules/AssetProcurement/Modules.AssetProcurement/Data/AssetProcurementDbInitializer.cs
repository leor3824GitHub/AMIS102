using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.AssetProcurement.Data;

internal sealed class AssetProcurementDbInitializer(
    ILogger<AssetProcurementDbInitializer> logger,
    AssetProcurementDbContext context) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] applied database migrations for asset procurement module", context.TenantInfo?.Identifier);
        }
    }

    public Task SeedAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("[{Tenant}] seeded asset procurement data.", context.TenantInfo?.Identifier);
        return Task.CompletedTask;
    }
}
