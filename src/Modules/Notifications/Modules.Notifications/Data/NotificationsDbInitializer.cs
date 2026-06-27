using AMIS.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Notifications.Data;

/// <summary>Migrates the Notifications schema. No seed data — inbox rows are produced at runtime.</summary>
internal sealed class NotificationsDbInitializer(
    ILogger<NotificationsDbInitializer> logger,
    NotificationsDbContext context) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            var tenant = context.TenantInfo?.Identifier;
            logger.LogInformation("[{Tenant}] applied database migrations for notifications module", tenant);
        }
    }

    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
