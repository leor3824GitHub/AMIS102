using AMIS.Modules.Notifications.Contracts.v1.DTOs;
using Mediator;

namespace AMIS.Modules.Notifications.Contracts.v1.Notifications;

/// <summary>List the caller's notifications, newest first. <paramref name="UnreadOnly"/> filters to unread.</summary>
public sealed record ListMyNotificationsQuery(bool UnreadOnly = false, int Take = 50)
    : IQuery<IReadOnlyList<NotificationDto>>;

/// <summary>Count the caller's unread notifications (drives the bell badge).</summary>
public sealed record GetUnreadCountQuery : IQuery<int>;

/// <summary>Mark one of the caller's notifications read. Idempotent; 404 if not the caller's row.</summary>
public sealed record MarkNotificationReadCommand(Guid Id) : ICommand<Unit>;

/// <summary>Mark all of the caller's notifications read.</summary>
public sealed record MarkAllNotificationsReadCommand : ICommand<Unit>;

/// <summary>Dismiss (delete) one of the caller's notifications. 404 if not the caller's row.</summary>
public sealed record DismissNotificationCommand(Guid Id) : ICommand<Unit>;
