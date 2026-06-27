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

        var rows = await _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId && (!query.UnreadOnly || !n.IsRead))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Order + page client-side: SQLite (used in tests) cannot ORDER BY a DateTimeOffset, and a per-user
        // inbox is small. Mirrors ListMyChannelsQueryHandler. Postgres would sort this in-DB equally well.
        return rows
            .OrderByDescending(n => n.CreatedOnUtc)
            .Take(take)
            .Select(n => n.ToDto())
            .ToList();
    }
}
