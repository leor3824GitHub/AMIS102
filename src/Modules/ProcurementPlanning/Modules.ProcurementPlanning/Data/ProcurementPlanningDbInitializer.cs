using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.ProcurementPlanning.Data;

internal sealed class ProcurementPlanningDbInitializer(
    ILogger<ProcurementPlanningDbInitializer> logger,
    ProcurementPlanningDbContext context) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] applied database migrations for procurement planning module", context.TenantInfo?.Identifier);
        }
    }

    public Task SeedAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("[{Tenant}] seeded procurement planning data.", context.TenantInfo?.Identifier);
        return Task.CompletedTask;
    }
}
