using AMIS.Framework.Core.Context;
using AMIS.Modules.Notifications.Contracts.v1.Notifications;
using AMIS.Modules.Notifications.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Notifications.Features.v1.MarkAllRead;

public sealed class MarkAllNotificationsReadCommandHandler : ICommandHandler<MarkAllNotificationsReadCommand, Unit>
{
    private readonly NotificationsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public MarkAllNotificationsReadCommandHandler(NotificationsDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();

        var unread = await _dbContext.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (unread.Count == 0)
        {
            return Unit.Value;
        }

        foreach (var notification in unread)
        {
            notification.MarkRead();
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
