using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.Identity.Contracts.Events;
using AMIS.Modules.MasterData.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.MasterData.Events;

/// <summary>
/// On every login, tries to auto-link the identity user to an unlinked employee profile
/// by matching on work email first, then on first+last name.
/// Only links when the employee is currently unlinked.
/// </summary>
internal sealed class LinkEmployeeOnLoginHandler
    : IIntegrationEventHandler<UserLoggedInIntegrationEvent>
{
    private static readonly EventId AutoLinkSkippedExistingLinkEventId = new(1001, "MasterData.EmployeeAutoLink.SkippedExistingLink");
    private static readonly EventId AutoLinkSucceededEventId = new(1002, "MasterData.EmployeeAutoLink.Succeeded");

    private readonly MasterDataDbContext _dbContext;
    private readonly ILogger<LinkEmployeeOnLoginHandler> _logger;

    public LinkEmployeeOnLoginHandler(MasterDataDbContext dbContext, ILogger<LinkEmployeeOnLoginHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task HandleAsync(UserLoggedInIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (string.IsNullOrWhiteSpace(@event.TenantId))
        {
            return;
        }

        // IgnoreQueryFilters: this handler runs in a background scope with no HTTP/tenant context,
        // so Finbuckle's tenant filter and the named SoftDelete filter would block all rows.
        // We apply the equivalent conditions explicitly in each query.

        var normalizedEmail = @event.Email.Trim().ToLowerInvariant();
        var normalizedFirstName = @event.FirstName.Trim().ToLowerInvariant();
        var normalizedLastName = @event.LastName.Trim().ToLowerInvariant();
        var normalizedFullName = BuildNormalizedFullName(@event.FirstName, @event.LastName);

        // Try email match first (most reliable)
        var employee = await _dbContext.Employees
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e =>
                !e.IsDeleted &&
                e.WorkEmail != null &&
                e.WorkEmail.ToLower() == normalizedEmail,
                ct)
            .ConfigureAwait(false);

        // Fall back to first+last exact match if no email match.
        if (employee is null && !string.IsNullOrWhiteSpace(normalizedFirstName) && !string.IsNullOrWhiteSpace(normalizedLastName))
        {
            employee = await _dbContext.Employees
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e =>
                    !e.IsDeleted &&
                    e.FirstName.ToLower() == normalizedFirstName &&
                    e.LastName.ToLower() == normalizedLastName,
                    ct)
                .ConfigureAwait(false);
        }

        // Last fallback for providers that only send display-name style claims.
        if (employee is null && !string.IsNullOrWhiteSpace(normalizedFullName))
        {
            employee = await _dbContext.Employees
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e =>
                    !e.IsDeleted &&
                    ((e.FirstName + " " + e.LastName).Trim().ToLower() == normalizedFullName),
                    ct)
                .ConfigureAwait(false);
        }

        if (employee is null)
        {
            return;
        }

        if (employee.IdentityUserId == @event.UserId)
        {
            // Already correctly linked — nothing to do
            return;
        }

        // Prevent taking over a profile already linked to someone else.
        if (!string.IsNullOrWhiteSpace(employee.IdentityUserId) && employee.IdentityUserId != @event.UserId)
        {
            _logger.LogWarning(
                AutoLinkSkippedExistingLinkEventId,
                "Auto-link decision. AutoLinkAction={AutoLinkAction} Reason={SkipReason} UserId={UserId} Email={Email} EmployeeNumber={EmployeeNumber} ExistingLinkedUserId={LinkedUserId} TenantId={TenantId}.",
                "SkippedExistingLink",
                "AlreadyLinkedToDifferentUser",
                @event.UserId,
                @event.Email,
                employee.EmployeeNumber,
                employee.IdentityUserId,
                @event.TenantId);
            return;
        }

        // Use ExecuteUpdateAsync to bypass BaseDbContext.SaveChangesAsync which sets
        // TenantNotSetMode = Overwrite — that would corrupt the employee's TenantId to null
        // since this handler runs in a background scope with no tenant context.
        await _dbContext.Employees
            .IgnoreQueryFilters()
            .Where(e => e.Id == employee.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.IdentityUserId, @event.UserId)
                .SetProperty(e => e.LastModifiedOnUtc, DateTimeOffset.UtcNow),
                ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            AutoLinkSucceededEventId,
            "Auto-link decision. AutoLinkAction={AutoLinkAction} UserId={UserId} Email={Email} EmployeeNumber={EmployeeNumber} TenantId={TenantId}.",
            "Linked",
            @event.UserId,
            @event.Email,
            employee.EmployeeNumber,
            @event.TenantId);
    }

    private static string BuildNormalizedFullName(string firstName, string lastName)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName)
            ? string.Empty
            : fullName.ToLowerInvariant();
    }
}

