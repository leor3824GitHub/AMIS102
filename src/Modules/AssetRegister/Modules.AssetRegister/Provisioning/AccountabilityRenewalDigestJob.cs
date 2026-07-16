using System.Globalization;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Eventing.Abstractions;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.Multitenancy.Contracts;
using AMIS.Modules.Notifications.Contracts.Events;
using AMIS.Modules.Notifications.Contracts.v1.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetRegister.Provisioning;

/// <summary>
/// Hangfire recurring job that drops a weekly "renewal due" digest in each accountable person's notification
/// bell. Pure read model — it never mutates AssetRegister state; it only reads the Active ICS/PAR whose expiry
/// falls within <see cref="WithinDays"/> (overdue included) and publishes one <see cref="NotificationType.AccountabilityRenewalDue"/>
/// row per recipient summarizing their due documents.
///
/// Each tenant is processed inside its own DI scope with the multi-tenant context set, so Finbuckle's query
/// filters and per-tenant connection strings apply normally (mirrors <see cref="DepreciationRecurringJob"/>).
/// A failure in one tenant is logged and does not abort the rest.
///
/// De-duplication is delegated to the Notifications module's unique (CorrelationId, RecipientUserId, Type)
/// index: the correlation id is bucketed by ISO week, so re-runs inside the same week are idempotent while a
/// fresh nudge is produced the following week until the document is renewed.
/// </summary>
public sealed class AccountabilityRenewalDigestJob(
    IServiceScopeFactory scopeFactory,
    ITenantService tenantService,
    ILogger<AccountabilityRenewalDigestJob> logger)
{
    /// <summary>Renewal-reminder horizon — mirrors <c>GetExpiringAccountabilitiesQuery</c>'s default window.</summary>
    private const int WithinDays = 60;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(WithinDays);

        // ISO-week bucket makes the correlation id stable for the whole week (idempotent re-runs) but distinct
        // next week (a fresh reminder until the document is renewed).
        var nowUtc = DateTime.UtcNow;
        var weekBucket = $"{ISOWeek.GetYear(nowUtc)}W{ISOWeek.GetWeekOfYear(nowUtc):D2}";

        var tenants = await tenantService.GetAllTenantInfosAsync(cancellationToken).ConfigureAwait(false);

        foreach (var tenant in tenants)
        {
            if (!tenant.IsActive)
                continue;

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                // Bind the tenant context before resolving the DbContext so its query filters and connection
                // string target this tenant.
                scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                    .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

                await ProcessTenantAsync(scope.ServiceProvider, tenant, today, cutoff, weekBucket, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Isolate per-tenant failures so one bad tenant doesn't abort the digest for the rest.
                logger.LogError(ex,
                    "AccountabilityRenewalDigestJob: failed to build renewal digest for tenant {TenantId}.", tenant.Id);
            }
        }
    }

    private async Task ProcessTenantAsync(
        IServiceProvider provider,
        AppTenantInfo tenant,
        DateOnly today,
        DateOnly cutoff,
        string weekBucket,
        CancellationToken cancellationToken)
    {
        var db = provider.GetRequiredService<AssetRegisterDbContext>();

        // Active accountabilities due for renewal within the horizon (overdue included), keyed by recipient.
        var dueRows = await db.PropertyAccountabilities.AsNoTracking()
            .Where(a => a.Status == AccountabilityStatus.Active
                        && a.ExpiresOn != null
                        && a.ExpiresOn <= cutoff)
            .Select(a => new { EmployeeId = a.ReceivedBy.EmployeeId, a.ExpiresOn })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (dueRows.Count == 0)
            return;

        var digests = dueRows
            .GroupBy(r => r.EmployeeId)
            .Select(g => new
            {
                EmployeeId = g.Key,
                Total = g.Count(),
                Overdue = g.Count(r => r.ExpiresOn < today)
            })
            .ToList();

        // Resolve each recipient's login account in one round-trip; only those with a linked identity user have
        // an inbox to target.
        var mediator = provider.GetRequiredService<IMediator>();
        var employees = await mediator
            .Send(new GetEmployeeReferencesByIdsQuery(digests.Select(d => d.EmployeeId).ToList()), cancellationToken)
            .ConfigureAwait(false);

        var eventBus = provider.GetRequiredService<IEventBus>();
        var tenantId = db.TenantInfo?.Identifier ?? tenant.Identifier ?? tenant.Id ?? string.Empty;
        var published = 0;

        foreach (var digest in digests)
        {
            if (!employees.TryGetValue(digest.EmployeeId, out var employee)
                || string.IsNullOrWhiteSpace(employee.IdentityUserId))
            {
                continue;
            }

            var body = digest.Overdue > 0
                ? $"You have {digest.Total} ICS/PAR document(s) due for renewal within {WithinDays} days, {digest.Overdue} already overdue. Please review and renew."
                : $"You have {digest.Total} ICS/PAR document(s) due for renewal within {WithinDays} days. Please review and renew.";

            var notification = new NotificationRequestedIntegrationEvent(
                RecipientUserId: employee.IdentityUserId,
                Type: NotificationType.AccountabilityRenewalDue,
                Title: "ICS/PAR renewal due",
                Body: body,
                Link: "/asset-register/my-accountability",
                Source: "AssetRegister",
                MetadataJson: null,
                TenantId: tenantId,
                CorrelationId: $"accountability-renewal-digest:{weekBucket}:{digest.EmployeeId}");

            await eventBus.PublishAsync(notification, cancellationToken).ConfigureAwait(false);
            published++;
        }

        if (published > 0 && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "AccountabilityRenewalDigestJob: tenant {TenantId} — published {Published} renewal digest(s) for {Recipients} recipient(s).",
                tenant.Id, published, digests.Count);
        }
    }
}
