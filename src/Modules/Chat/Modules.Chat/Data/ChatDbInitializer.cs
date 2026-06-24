using AMIS.Framework.Persistence;
using AMIS.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Chat.Data;

/// <summary>
/// Migrates the Chat schema and seeds the single NFA-wide global channel.
/// </summary>
internal sealed class ChatDbInitializer(
    ILogger<ChatDbInitializer> logger,
    ChatDbContext context) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            var tenant = context.TenantInfo?.Identifier;
            logger.LogInformation("[{Tenant}] applied database migrations for chat module", tenant);
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        // Fixed-id existence check makes this idempotent and re-entrant across tenants: FindAsync bypasses
        // query filters, so the global channel is created exactly once regardless of how many offices seed.
        var existing = await context.Channels
            .FindAsync([ChatModuleConstants.GlobalChannelId], cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var global = ChatChannel.CreateGlobal(
                ChatModuleConstants.GlobalChannelName,
                "NFA-wide announcements and chat.");

            await context.Channels.AddAsync(global, cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var tenant = context.TenantInfo?.Identifier;
            logger.LogInformation("[{Tenant}] seeded the global chat channel.", tenant);
        }
    }
}
