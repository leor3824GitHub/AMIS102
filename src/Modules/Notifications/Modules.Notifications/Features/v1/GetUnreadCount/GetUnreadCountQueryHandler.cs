using AMIS.Framework.Core.Context;
using AMIS.Modules.Notifications.Contracts.v1.Notifications;
using AMIS.Modules.Notifications.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Notifications.Features.v1.GetUnreadCount;

public sealed class GetUnreadCountQueryHandler : IQueryHandler<GetUnreadCountQuery, int>
{
    private readonly NotificationsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetUnreadCountQueryHandler(NotificationsDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<int> Handle(GetUnreadCountQuery query, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();

        return await _dbContext.Notifications
            .AsNoTracking()
            .CountAsync(n => n.RecipientUserId == userId && !n.IsRead, cancellationToken)
            .ConfigureAwait(false);
    }
}
