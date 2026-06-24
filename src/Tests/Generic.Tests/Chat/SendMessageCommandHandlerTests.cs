using System.Security.Claims;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Eventing.Abstractions;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Framework.Web.Realtime;
using AMIS.Modules.Chat.Contracts.Events;
using AMIS.Modules.Chat.Contracts.v1;
using AMIS.Modules.Chat.Contracts.v1.Messages;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Domain;
using AMIS.Modules.Chat.Features.v1.Messages.SendMessage;
using AMIS.Modules.Chat.Services;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Generic.Tests.Chat;

/// <summary>
/// Spine test for the most complex Chat slice. Verifies the SendMessage flow end to end against an
/// in-memory ChatDbContext: membership is required, <c>@username</c> tokens are resolved, the sender's
/// own mention and unresolved mentions are dropped, the message + surviving mentions are persisted in
/// one save, and exactly one mention integration event is published (per distinct resolved recipient).
/// </summary>
public sealed class SendMessageCommandHandlerTests
{
    [Fact]
    public async Task SendMessage_DropsSelfAndUnresolved_PersistsRealMention_AndPublishesOneEvent()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var realUserId = Guid.NewGuid();

        var (seedDb, conn) = CreateDbContext();
        await using var _ = seedDb;
        await using var __ = conn;

        // Office channel; the creator is added as an Admin member (mirrors CreateChannelCommandHandler), so membership passes.
        var channel = ChatChannel.CreateChannel("root", "general", topic: null, createdBy: senderId.ToString());
        channel.AddMember(senderId.ToString(), ChannelMemberRole.Admin);
        seedDb.Channels.Add(channel);
        await seedDb.SaveChangesAsync();

        var db = CreateDbContext(conn);
        await using var ___ = db;

        // Resolver resolves @self (the sender) and @realuser; @stranger stays unresolved (not returned).
        var resolver = new StubMentionResolver(
        [
            new ResolvedMention(senderId.ToString(), "self"),
            new ResolvedMention(realUserId.ToString(), "realuser"),
        ]);
        var eventBus = new RecordingEventBus();

        var handler = new SendMessageCommandHandler(
            db,
            new StubCurrentUser(senderId),
            resolver,
            new StubHubContext(),
            eventBus,
            NullLogger<SendMessageCommandHandler>.Instance);

        var command = new SendMessageCommand(channel.Id, "@self @stranger @realuser hello team");

        // Act
        var dto = await handler.Handle(command, CancellationToken.None);

        // Assert — message persisted with its content
        var saved = await db.Messages.Include(m => m.Mentions).ToListAsync();
        saved.Count.ShouldBe(1);
        saved[0].Content.ShouldBe("@self @stranger @realuser hello team");
        saved[0].SenderName.ShouldBe("Test Sender");   // resolved + denormalized at send time
        dto.Id.ShouldBe(saved[0].Id);
        dto.SenderName.ShouldBe("Test Sender");

        // Assert — only @realuser survives (self + unresolved dropped)
        saved[0].Mentions.Count.ShouldBe(1);
        saved[0].Mentions.Single().MentionedUserId.ShouldBe(realUserId.ToString());

        // Assert — exactly one mention integration event, addressed to @realuser
        eventBus.Published.Count.ShouldBe(1);
        var evt = eventBus.Published.Single().ShouldBeOfType<MentionedInChannelIntegrationEvent>();
        evt.MentionedUserId.ShouldBe(realUserId.ToString());
        evt.SenderId.ShouldBe(senderId.ToString());
        evt.ChannelId.ShouldBe(channel.Id);
        evt.CorrelationId.ShouldNotBeNullOrWhiteSpace();
        evt.Source.ShouldBe("Chat");

        // Assert — the channel's last-message pointer was bumped
        var savedChannel = await db.Channels.FirstAsync(c => c.Id == channel.Id);
        savedChannel.LastMessageId.ShouldBe(saved[0].Id);
    }

    [Fact]
    public async Task SendMessage_NonMember_ThrowsAndPersistsNothing()
    {
        // Arrange — a channel the caller is NOT a member of
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();

        var (seedDb, conn) = CreateDbContext();
        await using var _ = seedDb;
        await using var __ = conn;

        var channel = ChatChannel.CreateChannel("root", "private", topic: null, createdBy: ownerId.ToString());
        seedDb.Channels.Add(channel);
        await seedDb.SaveChangesAsync();

        var db = CreateDbContext(conn);
        await using var ___ = db;

        var handler = new SendMessageCommandHandler(
            db,
            new StubCurrentUser(outsiderId),
            new StubMentionResolver([]),
            new StubHubContext(),
            new RecordingEventBus(),
            NullLogger<SendMessageCommandHandler>.Instance);

        // Act + Assert — non-members get 404 (NotFound), and nothing is written.
        await Should.ThrowAsync<AMIS.Framework.Core.Exceptions.NotFoundException>(async () =>
            await handler.Handle(new SendMessageCommand(channel.Id, "hi"), CancellationToken.None));

        (await db.Messages.CountAsync()).ShouldBe(0);
    }

    // ── DbContext (SQLite in-memory, mirrors ConsolidatePpmpsHandlerTests) ──────────────

    private static (ChatDbContext Db, SqliteConnection Connection) CreateDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return (CreateDbContext(connection), connection);
    }

    private static ChatDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseSqlite(connection)
            .Options;
        var tenant = new AppTenantInfo("root", "root", "Root Tenant")
        {
            IsActive = true,
            ValidUpto = DateTime.UtcNow.AddYears(1)
        };
        var accessor = new StubTenantAccessor(new MultiTenantContext<AppTenantInfo>(tenant));
        var dbOptions = Options.Create(new DatabaseOptions
        {
            Provider = DbProviders.PostgreSQL,
            MigrationsAssembly = "Generic.Tests"
        });
        var db = new ChatDbContext(accessor, options, dbOptions, new StubHostEnvironment());
        db.Database.EnsureCreated();
        return db;
    }

    // ── Stubs ───────────────────────────────────────────────────────────────────────────

    private sealed class StubMentionResolver(IReadOnlyList<ResolvedMention> result) : IMentionResolver
    {
        public Task<IReadOnlyList<ResolvedMention>> ResolveAsync(
            IReadOnlyCollection<string> usernames, CancellationToken cancellationToken) => Task.FromResult(result);

        public Task<string?> ResolveDisplayNameAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>("Test Sender");
    }

    private sealed class RecordingEventBus : IEventBus
    {
        public List<IIntegrationEvent> Published { get; } = [];

        public Task PublishAsync(IIntegrationEvent @event, CancellationToken ct = default)
        {
            Published.Add(@event);
            return Task.CompletedTask;
        }

        public Task PublishAsync(IEnumerable<IIntegrationEvent> events, CancellationToken ct = default)
        {
            Published.AddRange(events);
            return Task.CompletedTask;
        }
    }

    private sealed class StubCurrentUser(Guid userId) : ICurrentUser
    {
        public string? Name => "Test";
        public Guid GetUserId() => userId;
        public string? GetUserEmail() => "test@example.com";
        public string? GetTenant() => "root";
        public bool IsAuthenticated() => true;
        public bool IsInRole(string role) => false;
        public IEnumerable<Claim>? GetUserClaims() => [];
    }

    private sealed class StubTenantAccessor(IMultiTenantContext<AppTenantInfo> ctx)
        : IMultiTenantContextAccessor<AppTenantInfo>
    {
        public IMultiTenantContext MultiTenantContext { get; } = ctx;
        IMultiTenantContext<AppTenantInfo> IMultiTenantContextAccessor<AppTenantInfo>.MultiTenantContext =>
            (IMultiTenantContext<AppTenantInfo>)MultiTenantContext;
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = nameof(SendMessageCommandHandlerTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } =
            new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }

    // No-op SignalR hub context — the handler broadcasts after commit but guards every hub call,
    // so a no-op proxy is sufficient to drive the persistence + eventing assertions.
    private sealed class StubHubContext : IHubContext<AppHub>
    {
        public IHubClients Clients { get; } = new StubClients();
        public IGroupManager Groups { get; } = new StubGroups();
    }

    private sealed class StubClients : IHubClients
    {
        private static readonly IClientProxy Proxy = new StubClientProxy();
        public IClientProxy All => Proxy;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => Proxy;
        public IClientProxy Client(string connectionId) => Proxy;
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => Proxy;
        public IClientProxy Group(string groupName) => Proxy;
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => Proxy;
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => Proxy;
        public IClientProxy User(string userId) => Proxy;
        public IClientProxy Users(IReadOnlyList<string> userIds) => Proxy;
    }

    private sealed class StubClientProxy : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubGroups : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
