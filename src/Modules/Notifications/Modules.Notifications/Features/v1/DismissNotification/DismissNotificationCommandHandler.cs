using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Notifications.Contracts.v1.Notifications;
using AMIS.Modules.Notifications.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Notifications.Features.v1.DismissNotification;

public sealed class DismissNotificationCommandHandler : ICommandHandler<DismissNotificationCommand, Unit>
{
    private readonly NotificationsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DismissNotificationCommandHandler(NotificationsDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(DismissNotificationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var userId = _currentUser.GetUserId().ToString();

        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == command.Id && n.RecipientUserId == userId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Notification '{command.Id}' not found.");

        _dbContext.Notifications.Remove(notification);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
