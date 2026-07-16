using AMIS.Framework.Eventing.Abstractions;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Accountability;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using AMIS.Modules.AssetRegister.Provisioning;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.Multitenancy.Contracts;
using AMIS.Modules.Notifications.Contracts.Events;
using AMIS.Modules.Notifications.Contracts.v1.Enums;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Integration;

/// <summary>
/// Guards the weekly renewal digest (<see cref="AccountabilityRenewalDigestJob"/>): each accountable person
/// with a linked login gets exactly one <see cref="NotificationType.AccountabilityRenewalDue"/> row summarizing
/// their Active ICS/PAR due for renewal within 60 days (overdue included). Documents outside the window, not
/// yet Active, or belonging to an employee with no login are excluded.
/// </summary>
public sealed class AccountabilityRenewalDigestJobTests
{
    private const string Tenant = "test-tenant";
    private const string ReceiverLogin = "9f1b3c2d-0000-4444-8888-aaaaaaaaaaaa";

    [Fact]
    public async Task Run_PublishesOneDigest_PerAccountableEmployee_WithinWindow()
    {
        using var db = CreateDbContext();
        var receiverId = Guid.NewGuid();

        // Two due (one overdue) → counted; one far-future → excluded; one still PendingAcceptance → excluded.
        await SeedActiveParAsync(db, receiverId, "PAR-A", expiresInDays: 20);
        await SeedActiveParAsync(db, receiverId, "PAR-B", expiresInDays: -5);
        await SeedActiveParAsync(db, receiverId, "PAR-C", expiresInDays: 400);
        await SeedPendingParAsync(db, receiverId, "PAR-D", expiresInDays: 10);

        var published = new List<NotificationRequestedIntegrationEvent>();
        var eventBus = CapturingEventBus(published);

        var job = NewJob(db, MediatorWithEmployees((receiverId, ReceiverLogin)), eventBus);
        await job.RunAsync(CancellationToken.None);

        published.Count.ShouldBe(1);
        var digest = published[0];
        digest.RecipientUserId.ShouldBe(ReceiverLogin);
        digest.Type.ShouldBe(NotificationType.AccountabilityRenewalDue);
        digest.Source.ShouldBe("AssetRegister");
        digest.TenantId.ShouldBe(Tenant);
        digest.Link.ShouldBe("/asset-register/my-accountability");
        digest.CorrelationId.ShouldContain("accountability-renewal-digest:");
        digest.CorrelationId.ShouldEndWith(receiverId.ToString());
        digest.Body.ShouldContain("2 ICS/PAR");   // two due documents
        digest.Body.ShouldContain("1 already overdue");
    }

    [Fact]
    public async Task Run_SkipsEmployee_WithNoLinkedLogin()
    {
        using var db = CreateDbContext();
        var receiverId = Guid.NewGuid();
        await SeedActiveParAsync(db, receiverId, "PAR-A", expiresInDays: 10);

        var published = new List<NotificationRequestedIntegrationEvent>();
        var eventBus = CapturingEventBus(published);

        // Directory-only employee (no identity account) → nobody to notify.
        var job = NewJob(db, MediatorWithEmployees((receiverId, null)), eventBus);
        await job.RunAsync(CancellationToken.None);

        published.ShouldBeEmpty();
    }

    [Fact]
    public async Task Run_PublishesNothing_WhenNoAccountabilitiesAreDue()
    {
        using var db = CreateDbContext();
        var receiverId = Guid.NewGuid();
        await SeedActiveParAsync(db, receiverId, "PAR-C", expiresInDays: 400);

        var published = new List<NotificationRequestedIntegrationEvent>();
        var eventBus = CapturingEventBus(published);

        var job = NewJob(db, MediatorWithEmployees((receiverId, ReceiverLogin)), eventBus);
        await job.RunAsync(CancellationToken.None);

        published.ShouldBeEmpty();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    private static AccountabilityRenewalDigestJob NewJob(AssetRegisterDbContext db, IMediator mediator, IEventBus eventBus)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton(mediator);
        services.AddSingleton(eventBus);
        services.AddSingleton<IMultiTenantContextSetter>(new StubMultiTenantContextSetter());
        var provider = services.BuildServiceProvider();

        var tenant = new AppTenantInfo(Tenant, Tenant, "Test Tenant") { IsActive = true, ValidUpto = DateTime.UtcNow.AddYears(1) };
        var tenantService = Substitute.For<ITenantService>();
        tenantService.GetAllTenantInfosAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AppTenantInfo>)[tenant]);

        return new AccountabilityRenewalDigestJob(
            provider.GetRequiredService<IServiceScopeFactory>(),
            tenantService,
            NullLogger<AccountabilityRenewalDigestJob>.Instance);
    }

    private static IEventBus CapturingEventBus(List<NotificationRequestedIntegrationEvent> sink)
    {
        var eventBus = Substitute.For<IEventBus>();
        eventBus.PublishAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci =>
            {
                if (ci.Arg<IIntegrationEvent>() is NotificationRequestedIntegrationEvent n)
                    sink.Add(n);
            });
        return eventBus;
    }

    private static IMediator MediatorWithEmployees(params (Guid Id, string? Login)[] employees)
    {
        var mediator = Substitute.For<IMediator>();
        var dict = employees.ToDictionary(e => e.Id, e => Receiver(e.Id, e.Login));
        mediator.Send(Arg.Any<GetEmployeeReferencesByIdsQuery>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<Guid, EmployeeReferenceDto>)dict);
        return mediator;
    }

    private static EmployeeReferenceDto Receiver(Guid id, string? identityUserId) => new(
        id, "EMP-RECV", identityUserId, "End", "User", null,
        Guid.NewGuid(), "OFF", "Office", Guid.NewGuid(), "DEP", "Dept",
        Guid.NewGuid(), "POS", "Engineer", null, null, null, true);

    private static Task SeedActiveParAsync(AssetRegisterDbContext db, Guid receiverId, string docNo, int expiresInDays)
        => SeedParAsync(db, receiverId, docNo, expiresInDays, accept: true);

    private static Task SeedPendingParAsync(AssetRegisterDbContext db, Guid receiverId, string docNo, int expiresInDays)
        => SeedParAsync(db, receiverId, docNo, expiresInDays, accept: false);

    private static async Task SeedParAsync(AssetRegisterDbContext db, Guid receiverId, string docNo, int expiresInDays, bool accept)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // PAR expiry is server-authoritative: issuedOn + 3 years. Back-date issuance so expiry lands where we want.
        var issuedOn = today.AddDays(expiresInDays).AddYears(-PropertyAccountability.ParRenewalYears);

        var asset = await SeedAvailablePpeAsync(db, docNo);
        var issuedBy = EmployeeRef.Create(Guid.NewGuid(), "Supply Officer", "Officer");
        var receivedBy = EmployeeRef.Create(receiverId, "End User", "Engineer");

        var accountability = PropertyAccountability.Issue(
            Tenant, AccountabilityType.PPE_PAR, docNo, "01", issuedBy, receivedBy, issuedOn, null,
            [(asset, "1", null, null)]);

        if (accept)
            accountability.Accept(today);

        db.PropertyAccountabilities.Add(accountability);
        await db.SaveChangesAsync();
    }

    private static async Task<AssetRegistry> SeedAvailablePpeAsync(AssetRegisterDbContext db, string suffix)
    {
        var catalog = PropertyItemCatalog.Create(
            Tenant, $"DESK-{suffix}", "Office Desk", "07-PPE", "DSK", "pc", "10405030", 10);
        var asset = AssetRegistry.Register(
            Tenant, catalog, AssetType.PPE, AssetCategory.PPE,
            PropertyNumber.Create($"2026-NFA-00B-07-DSK-{Guid.NewGuid():N}".Substring(0, 30)), "Office Desk",
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

    private sealed class StubMultiTenantContextSetter : IMultiTenantContextSetter
    {
        // The job assigns this per-tenant before anything reads it; the initial value is never observed.
        public IMultiTenantContext MultiTenantContext { get; set; } = null!;
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
        public string ApplicationName { get; set; } = nameof(AccountabilityRenewalDigestJobTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
