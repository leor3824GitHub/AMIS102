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

        var rows = _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId);

        if (query.UnreadOnly)
        {
            rows = rows.Where(n => !n.IsRead);
        }

        var result = await rows
            .OrderByDescending(n => n.CreatedOnUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return result.Select(n => n.ToDto()).ToList();
    }
}
