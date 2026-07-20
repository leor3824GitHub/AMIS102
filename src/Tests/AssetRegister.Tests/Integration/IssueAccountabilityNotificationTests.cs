using AMIS.Framework.Eventing.Abstractions;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using AMIS.Modules.AssetRegister.Domain.Services;
using AMIS.Modules.AssetRegister.Features.v1.Accountability.IssueAccountability;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.Notifications.Contracts.Events;
using AMIS.Modules.Notifications.Contracts.v1.Enums;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Integration;

/// <summary>
/// Guards the producer side of the acceptance notification: when an ICS/PAR is issued,
/// <see cref="IssueAccountabilityCommandHandler"/> must publish a <see cref="NotificationRequestedIntegrationEvent"/>
/// to the receiving (accountable) employee — but only if that employee has a linked login (<c>IdentityUserId</c>).
/// The publish is best-effort: a bus failure must never roll back the issuance.
/// </summary>
public sealed class IssueAccountabilityNotificationTests
{
    private const string Tenant = "test-tenant";

    [Fact]
    public async Task Handle_PublishesAccountabilityIssuedEvent_WhenReceiverHasLinkedLogin()
    {
        using var db = CreateDbContext();
        var asset = await SeedAvailablePpeAsync(db);
        var receiverId = Guid.NewGuid();
        const string receiverLogin = "9f1b3c2d-0000-4444-8888-aaaaaaaaaaaa";

        var eventBus = Substitute.For<IEventBus>();
        NotificationRequestedIntegrationEvent? published = null;
        eventBus.PublishAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => published = ci.Arg<IIntegrationEvent>() as NotificationRequestedIntegrationEvent);

        var handler = NewHandler(db, MediatorWithReceiver(receiverId, receiverLogin), eventBus);

        var result = await handler.Handle(IssueParCommand(asset.Id, receiverId), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Status.ShouldBe(AccountabilityStatus.PendingAcceptance);

        await eventBus.Received(1).PublishAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>());
        published.ShouldNotBeNull();
        published!.RecipientUserId.ShouldBe(receiverLogin);
        published.Type.ShouldBe(NotificationType.AccountabilityIssued);
        published.Source.ShouldBe("AssetRegister");
        published.CorrelationId.ShouldBe(result.Id.ToString()); // idempotency key = accountability id
        published.TenantId.ShouldBe(Tenant);
        published.Link.ShouldBe("/asset-register/my-accountability");
        published.Body.ShouldContain(result.DocumentNo);
        published.Title.ShouldContain("PAR");
    }

    [Fact]
    public async Task Handle_SkipsPublish_WhenReceiverHasNoLinkedLogin()
    {
        using var db = CreateDbContext();
        var asset = await SeedAvailablePpeAsync(db);
        var receiverId = Guid.NewGuid();

        var eventBus = Substitute.For<IEventBus>();
        // Receiver is a directory-only employee with no identity account → nothing to notify.
        var handler = NewHandler(db, MediatorWithReceiver(receiverId, identityUserId: null), eventBus);

        var result = await handler.Handle(IssueParCommand(asset.Id, receiverId), CancellationToken.None);

        // The issuance still succeeds; only the notification is skipped.
        result.ShouldNotBeNull();
        result.Status.ShouldBe(AccountabilityStatus.PendingAcceptance);
        await eventBus.DidNotReceive().PublishAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotThrow_AndStillIssues_WhenPublishFails()
    {
        using var db = CreateDbContext();
        var asset = await SeedAvailablePpeAsync(db);
        var receiverId = Guid.NewGuid();

        var eventBus = Substitute.For<IEventBus>();
        eventBus.When(b => b.PublishAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("event bus unavailable"));

        var handler = NewHandler(db, MediatorWithReceiver(receiverId, "9f1b3c2d-0000-4444-8888-aaaaaaaaaaaa"), eventBus);

        PropertyAccountabilityDto? result = null;
        await Should.NotThrowAsync(async () =>
            result = await handler.Handle(IssueParCommand(asset.Id, receiverId), CancellationToken.None));

        // Notification failure must not roll back the issuance.
        result.ShouldNotBeNull();
        result!.Status.ShouldBe(AccountabilityStatus.PendingAcceptance);
        (await db.PropertyAccountabilities.CountAsync()).ShouldBe(1);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    private static IssueAccountabilityCommandHandler NewHandler(
        AssetRegisterDbContext db, IMediator mediator, IEventBus eventBus)
    {
        var numbers = Substitute.For<IAccountabilityNumberGenerator>();
        numbers.NextParAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns("PAR-2026-06-0001");

        var freezeGuard = Substitute.For<ICountFreezeGuard>();
        freezeGuard.EnsureMovementAllowedAsync(Arg.Any<IReadOnlyCollection<AssetRegistry>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        return new IssueAccountabilityCommandHandler(
            db, numbers, freezeGuard, mediator, eventBus, NullLogger<IssueAccountabilityCommandHandler>.Instance);
    }

    private static IssueAccountabilityCommand IssueParCommand(Guid assetId, Guid receiverId) => new(
        AccountabilityType: AccountabilityType.PPE_PAR,
        FundCluster: "01",
        IssuedBy: new EmployeeRefDto(Guid.NewGuid(), "Supply Officer", "Officer"),
        ReceivedBy: new EmployeeRefDto(receiverId, "End User", "Engineer"),
        IssuedOn: new DateOnly(2026, 6, 16),
        ExpiresOn: null,
        Lines: [new IssueAccountabilityLineRequest(assetId, "1", Guid.NewGuid(), null, null, null, null, null)]);

    private static IMediator MediatorWithReceiver(Guid receiverId, string? identityUserId)
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Is<GetEmployeeReferenceByIdQuery>(q => q.Id == receiverId), Arg.Any<CancellationToken>())
            .Returns(Receiver(receiverId, identityUserId));
        return mediator;
    }

    private static EmployeeReferenceDto Receiver(Guid id, string? identityUserId) => new(
        id, "EMP-RECV", identityUserId, "End", "User", null,
        Guid.NewGuid(), "OFF", "Office", "Office Address", Guid.NewGuid(), "DEP", "Dept",
        Guid.NewGuid(), "POS", "Engineer", true);

    private static async Task<AssetRegistry> SeedAvailablePpeAsync(AssetRegisterDbContext db)
    {
        var catalog = PropertyItemCatalog.Create(
            Tenant, "DESK-001", "Office Desk", "07-PPE", "DSK", "pc", "10405030", 10);
        var asset = AssetRegistry.Register(
            Tenant, catalog, AssetType.PPE, AssetCategory.PPE,
            PropertyNumber.Create("2026-NFA-00B-07-DSK-001"), "Office Desk",
            null, null, null, "01", new DateOnly(2026, 1, 15), 5000m, null, null);

        db.AssetRegistries.Add(asset);
        await db.SaveChangesAsync();
        return asset;
    }

    private static AssetRegisterDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AssetRegisterDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var tenant = new AppTenantInfo(Tenant, Tenant, "Test Tenant")
        {
            IsActive = true,
            ValidUpto = DateTime.UtcNow.AddYears(1)
        };
        var tenantAccessor = new TestTenantContextAccessor(new MultiTenantContext<AppTenantInfo>(tenant));
        var databaseOptions = Options.Create(new DatabaseOptions { Provider = "InMemory" });

        return new AssetRegisterDbContext(tenantAccessor, options, databaseOptions, new TestHostEnvironment());
    }

    private sealed class TestTenantContextAccessor(IMultiTenantContext<AppTenantInfo> multiTenantContext)
        : IMultiTenantContextAccessor<AppTenantInfo>
    {
        public IMultiTenantContext MultiTenantContext { get; } = multiTenantContext;

        IMultiTenantContext<AppTenantInfo> IMultiTenantContextAccessor<AppTenantInfo>.MultiTenantContext =>
            (IMultiTenantContext<AppTenantInfo>)MultiTenantContext;
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = nameof(IssueAccountabilityNotificationTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
