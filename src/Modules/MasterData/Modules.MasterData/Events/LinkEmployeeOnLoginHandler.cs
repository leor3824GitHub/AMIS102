using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Identity.Contracts.Events;
using FSH.Modules.MasterData.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.MasterData.Events;

/// <summary>
/// On every login, tries to auto-link the identity user to an unlinked employee profile
/// by matching on work email first, then on first+last name.
/// Only links when the employee has no IdentityUserId yet, or is linked to a different user id.
/// </summary>
internal sealed class LinkEmployeeOnLoginHandler
    : IIntegrationEventHandler<UserLoggedInIntegrationEvent>
{
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

        // Try email match first (most reliable)
        var employee = await _dbContext.Employees
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e =>
                !e.IsDeleted &&
                e.WorkEmail != null &&
                e.WorkEmail.ToLower() == @event.Email.ToLower(),
                ct)
            .ConfigureAwait(false);

        // Fall back to name match if no email match
        if (employee is null && !string.IsNullOrWhiteSpace(@event.FirstName) && !string.IsNullOrWhiteSpace(@event.LastName))
        {
            employee = await _dbContext.Employees
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e =>
                    !e.IsDeleted &&
                    e.FirstName.ToLower() == @event.FirstName.ToLower() &&
                    e.LastName.ToLower() == @event.LastName.ToLower(),
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
            "Auto-linked identity user {UserId} ({Email}) to employee {EmployeeNumber} in tenant {TenantId}.",
            @event.UserId,
            @event.Email,
            employee.EmployeeNumber,
            @event.TenantId);
    }
}
