using System.Security.Claims;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Framework.Web.Realtime;
using AMIS.Modules.Notifications.Contracts.Events;
using AMIS.Modules.Notifications.Contracts.v1.Enums;
using AMIS.Modules.Notifications.Contracts.v1.Notifications;
using AMIS.Modules.Notifications.Data;
using AMIS.Modules.Notifications.Features.v1.ListMyNotifications;
using AMIS.Modules.Notifications.Features.v1.MarkRead;
using AMIS.Modules.Notifications.Services;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Generic.Tests.Notifications;

/// <summary>
/// Verifies the Notifications spine against an in-memory NotificationsDbContext: the writer persists a
/// denormalized row and pushes <c>NotificationCreated</c> once; redelivery of the same
/// (CorrelationId, recipient, type) is idempotent; an unparseable recipient is skipped; and the inbox
/// queries are scoped to the calling user.
/// </summary>
public sealed class NotificationWriterTests
{
    private const string Recipient = "11111111-1111-1111-1111-111111111111";

    [Fact]
    public async Task WriteAsync_PersistsDenormalizedRow_AndPushesOnce()
    {
        var (db, conn) = CreateDbContext();
        await using var _ = db;
        await using var __ = conn;

        var hub = new RecordingHubContext();
        var writer = new NotificationWriter(db, hub, NullLogger<NotificationWriter>.Instance);

        await writer.WriteAsync(Request(correlationId: "JO-1"), CancellationToken.None);

        var saved = await db.Notifications.ToListAsync();
        saved.Count.ShouldBe(1);
        saved[0].RecipientUserId.ShouldBe(Recipient);
        saved[0].Type.ShouldBe(NotificationType.InspectionRequested);
        saved[0].Title.ShouldBe("Inspection requested");
        saved[0].Link.ShouldBe("/procurement/job-orders/1");
        saved[0].IsRead.ShouldBeFalse();

        // Exactly one push of NotificationCreated to the recipient's user group.
        hub.Sends.Count.ShouldBe(1);
        hub.Sends[0].Method.ShouldBe("NotificationCreated");
        hub.Sends[0].Group.ShouldBe(AppHub.UserGroup(Guid.Parse(Recipient)));
    }

    [Fact]
    public async Task WriteAsync_DuplicateCorrelation_IsIdempotent()
    {
        var (db, conn) = CreateDbContext();
        await using var _ = db;
        await using var __ = conn;

        var hub = new RecordingHubContext();
        var writer = new NotificationWriter(db, hub, NullLogger<NotificationWriter>.Instance);

        await writer.WriteAsync(Request(correlationId: "JO-9"), CancellationToken.None);
        await writer.WriteAsync(Request(correlationId: "JO-9"), CancellationToken.None);

        (await db.Notifications.CountAsync()).ShouldBe(1);
        hub.Sends.Count.ShouldBe(1); // second delivery short-circuits before persist + push
    }

    [Fact]
    public async Task WriteAsync_UnparseableRecipient_PersistsNothing()
    {
        var (db, conn) = CreateDbContext();
        await using var _ = db;
        await using var __ = conn;

        var hub = new RecordingHubContext();
        var writer = new NotificationWriter(db, hub, NullLogger<NotificationWriter>.Instance);

        await writer.WriteAsync(Request(correlationId: "JO-2", recipient: "not-a-guid"), CancellationToken.None);

        (await db.Notifications.CountAsync()).ShouldBe(0);
        hub.Sends.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ListAndMarkRead_AreScopedToCaller()
    {
        var (db, conn) = CreateDbContext();
        await using var _ = db;
        await using var __ = conn;

        var hub = new RecordingHubContext();
        var writer = new NotificationWriter(db, hub, NullLogger<NotificationWriter>.Instance);
        var other = "22222222-2222-2222-2222-222222222222";

        await writer.WriteAsync(Request(correlationId: "A"), CancellationToken.None);
        await writer.WriteAsync(Request(correlationId: "B", recipient: other), CancellationToken.None);

        var caller = new StubCurrentUser(Guid.Parse(Recipient));

        // List returns only the caller's row.
        var listed = await new ListMyNotificationsQueryHandler(db, caller)
            .Handle(new ListMyNotificationsQuery(), CancellationToken.None);
        listed.Count.ShouldBe(1);
        var id = listed[0].Id;

        // Mark read flips the caller's row only.
        await new MarkNotificationReadCommandHandler(db, caller)
            .Handle(new MarkNotificationReadCommand(id), CancellationToken.None);

        var mine = await db.Notifications.FirstAsync(n => n.Id == id);
        mine.IsRead.ShouldBeTrue();
        var theirs = await db.Notifications.FirstAsync(n => n.RecipientUserId == other);
        theirs.IsRead.ShouldBeFalse();
    }

    [Fact]
    public async Task MarkReadAsync_FlipsRow_PushesOnce_AndRedeliveryIsIdempotent()
    {
        var (db, conn) = CreateDbContext();
        await using var _ = db;
        await using var __ = conn;

        var hub = new RecordingHubContext();
        var writer = new NotificationWriter(db, hub, NullLogger<NotificationWriter>.Instance);

        await writer.WriteAsync(Request(correlationId: "ICS-1"), CancellationToken.None);

        await writer.MarkReadAsync(ReadRequest("ICS-1"), CancellationToken.None);

        var row = await db.Notifications.SingleAsync();
        row.IsRead.ShouldBeTrue();
        row.ReadOnUtc.ShouldNotBeNull();

        hub.Sends.Count.ShouldBe(2); // NotificationCreated + NotificationRead
        hub.Sends[1].Method.ShouldBe("NotificationRead");
        hub.Sends[1].Group.ShouldBe(AppHub.UserGroup(Guid.Parse(Recipient)));

        // Redelivery of the same read-request and an unknown correlation are silent no-ops.
        await writer.MarkReadAsync(ReadRequest("ICS-1"), CancellationToken.None);
        await writer.MarkReadAsync(ReadRequest("ICS-404"), CancellationToken.None);
        hub.Sends.Count.ShouldBe(2);
    }

    private static NotificationReadRequestedIntegrationEvent ReadRequest(string correlationId, string? recipient = null) =>
        new(
            RecipientUserId: recipient ?? Recipient,
            Type: NotificationType.InspectionRequested,
            Source: "ProcurementAcquisition",
            CorrelationId: correlationId,
            TenantId: "root");

    private static NotificationRequestedIntegrationEvent Request(string correlationId, string? recipient = null) =>
        new(
            RecipientUserId: recipient ?? Recipient,
            Type: NotificationType.InspectionRequested,
            Title: "Inspection requested",
            Body: "Job Order JO-1 is issued and ready for your inspection.",
            Link: "/procurement/job-orders/1",
            Source: "ProcurementAcquisition",
            MetadataJson: null,
            TenantId: "root",
            CorrelationId: correlationId);

    // ── DbContext (SQLite in-memory, mirrors SendMessageCommandHandlerTests) ──────────────

    private static (NotificationsDbContext Db, SqliteConnection Connection) CreateDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
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
        var db = new NotificationsDbContext(accessor, options, dbOptions, new StubHostEnvironment());
        db.Database.EnsureCreated();
        return (db, connection);
    }

    // ── Stubs ─────────────────────────────────────────────────────────────────────────────

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
        public string ApplicationName { get; set; } = nameof(NotificationWriterTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } =
            new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }

    // Records each SendAsync (method + target group) so the push can be asserted.
    private sealed record SentMessage(string Group, string Method);

    private sealed class RecordingHubContext : IHubContext<AppHub>
    {
        public List<SentMessage> Sends { get; } = [];
        public IHubClients Clients => new RecordingClients(Sends);
        public IGroupManager Groups { get; } = new StubGroups();
    }

    private sealed class RecordingClients(List<SentMessage> sends) : IHubClients
    {
        public IClientProxy All => new StubClientProxy(sends, "all");
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => new StubClientProxy(sends, "all");
        public IClientProxy Client(string connectionId) => new StubClientProxy(sends, connectionId);
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => new StubClientProxy(sends, "clients");
        public IClientProxy Group(string groupName) => new StubClientProxy(sends, groupName);
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => new StubClientProxy(sends, groupName);
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => new StubClientProxy(sends, "groups");
        public IClientProxy User(string userId) => new StubClientProxy(sends, userId);
        public IClientProxy Users(IReadOnlyList<string> userIds) => new StubClientProxy(sends, "users");
    }

    private sealed class StubClientProxy(List<SentMessage> sends, string group) : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            sends.Add(new SentMessage(group, method));
            return Task.CompletedTask;
        }
    }

    private sealed class StubGroups : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
