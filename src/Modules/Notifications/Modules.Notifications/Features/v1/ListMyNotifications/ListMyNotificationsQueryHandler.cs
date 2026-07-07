using AMIS.Framework.Core.Context;
using AMIS.Modules.Notifications.Contracts.v1.DTOs;
using AMIS.Modules.Notifications.Contracts.v1.Notifications;
using AMIS.Modules.Notifications.Data;
using AMIS.Modules.Notifications.Features.v1;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Notifications.Features.v1.ListMyNotifications;

public sealed class ListMyNotificationsQueryHandler : IQueryHandler<ListMyNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private const int MaxTake = 200;

    private readonly NotificationsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListMyNotificationsQueryHandler(NotificationsDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<IReadOnlyList<NotificationDto>> Handle(ListMyNotificationsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var userId = _currentUser.GetUserId().ToString();
        var take = Math.Clamp(query.Take, 1, MaxTake);

        var baseQuery = _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId && (!query.UnreadOnly || !n.IsRead));

        // Order + take. On providers that can translate DateTimeOffset ordering (Postgres in production)
        // push it to the DB so a user's entire inbox is never materialized. The SQLite provider used in
        // tests cannot ORDER BY a DateTimeOffset, so fall back to in-memory ordering there.
        var providerSortsDateTimeOffset =
            _dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

        if (providerSortsDateTimeOffset)
        {
            var ordered = await baseQuery
                .OrderByDescending(n => n.CreatedOnUtc)
                .Take(take)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return ordered.Select(n => n.ToDto()).ToList();
        }

        var rows = await baseQuery
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .OrderByDescending(n => n.CreatedOnUtc)
            .Take(take)
            .Select(n => n.ToDto())
            .ToList();
    }
}
