using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.AssetManagement.Data;

internal sealed class AssetManagementDbInitializer(
    ILogger<AssetManagementDbInitializer> logger,
    AssetManagementDbContext context) : IDbInitializer
{
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

    public Task SeedAsync(CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("[{Tenant}] seeded asset management data.", context.TenantInfo?.Identifier);
        }

        return Task.CompletedTask;
    }
}
