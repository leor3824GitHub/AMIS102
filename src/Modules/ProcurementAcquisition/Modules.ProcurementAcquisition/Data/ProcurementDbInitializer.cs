using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.ProcurementAcquisition.Data;

internal sealed class ProcurementDbInitializer(
    ILogger<ProcurementDbInitializer> logger,
    ProcurementDbContext context) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] applied database migrations for procurement module", context.TenantInfo?.Identifier);
        }
    }

    public Task SeedAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("[{Tenant}] seeded procurement data.", context.TenantInfo?.Identifier);
        return Task.CompletedTask;
    }
}
