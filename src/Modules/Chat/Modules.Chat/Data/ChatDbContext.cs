using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Eventing.Outbox;
using AMIS.Framework.Persistence.Context;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AMIS.Modules.Chat.Data;

public class ChatDbContext : BaseDbContext
{
    // Captured once per context instance; the ChatChannel query filter reads it per query (canonical
    // EF multitenancy pattern), so office channels are scoped without Finbuckle's .IsMultiTenant().
    private readonly string? _currentTenantId;

    public DbSet<ChatChannel> Channels => Set<ChatChannel>();
    public DbSet<ChannelMember> ChannelMembers => Set<ChannelMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageMention> MessageMentions => Set<MessageMention>();
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public ChatDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<ChatDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment)
        : base(multiTenantContextAccessor, options, settings, environment)
    {
        ArgumentNullException.ThrowIfNull(multiTenantContextAccessor);
        ArgumentNullException.ThrowIfNull(settings);

        _currentTenantId = multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Identifier;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(ChatModuleConstants.SchemaName));

        // ChatChannel is ISoftDeletable, so BaseDbContext already appended an anonymous "!IsDeleted" filter.
        // Replace it with the combined office-or-global + soft-delete filter (kept anonymous to avoid the
        // EF Core 10 named/anonymous-filter clash). Referencing the context field _currentTenantId makes EF
        // re-evaluate the tenant per query. Child tables are intentionally NOT filtered — channel membership
        // is the authorization boundary. See CHAT-PORT-PLAN.md gotcha #1.
        modelBuilder.Entity<ChatChannel>().HasQueryFilter(
            c => (c.TenantId == _currentTenantId || c.TenantId == null) && !c.IsDeleted);
    }
}
