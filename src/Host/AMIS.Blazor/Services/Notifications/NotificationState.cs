using AMIS.Modules.Notifications.Contracts.v1.DTOs;

namespace AMIS.Blazor.Services.Notifications;

#pragma warning disable CA1003 // Action is the idiomatic pattern for Blazor state change events

/// <summary>
/// Circuit-scoped store for the current user's notification bell — unread count + a recent slice. Owned by
/// the <c>NotificationBell</c> in the app bar; mutated by REST loads and live <c>NotificationCreated</c>
/// hub pushes. Mirrors the <c>IUserProfileState</c> event pattern.
/// </summary>
internal interface INotificationState
{
    int UnreadCount { get; }
    IReadOnlyList<NotificationDto> Recent { get; }
    event Action? OnChanged;

    void SetInitial(int unreadCount, IReadOnlyList<NotificationDto> recent);
    void Add(NotificationDto notification);
    void MarkRead(Guid id);
    void MarkAllRead();
    void Remove(Guid id);
    void Clear();
}

internal sealed class NotificationState : INotificationState
{
    private const int RecentCap = 15;

    private readonly List<NotificationDto> _recent = [];

    public int UnreadCount { get; private set; }
    public IReadOnlyList<NotificationDto> Recent => _recent;

    public event Action? OnChanged;

    public void SetInitial(int unreadCount, IReadOnlyList<NotificationDto> recent)
    {
        ArgumentNullException.ThrowIfNull(recent);
        UnreadCount = unreadCount;
        _recent.Clear();
        _recent.AddRange(recent.Take(RecentCap));
        OnChanged?.Invoke();
    }

    public void Add(NotificationDto notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        // Ignore a duplicate id (e.g. a live push that races the initial load).
        if (_recent.Any(n => n.Id == notification.Id))
        {
            return;
        }

        _recent.Insert(0, notification);
        if (_recent.Count > RecentCap)
        {
            _recent.RemoveRange(RecentCap, _recent.Count - RecentCap);
        }

        if (!notification.IsRead)
        {
            UnreadCount++;
        }

        OnChanged?.Invoke();
    }

    public void MarkRead(Guid id)
    {
        var index = _recent.FindIndex(n => n.Id == id);
        if (index >= 0 && !_recent[index].IsRead)
        {
            _recent[index] = _recent[index] with { IsRead = true };
            UnreadCount = Math.Max(0, UnreadCount - 1);
            OnChanged?.Invoke();
        }
    }

    public void MarkAllRead()
    {
        for (var i = 0; i < _recent.Count; i++)
        {
            if (!_recent[i].IsRead)
            {
                _recent[i] = _recent[i] with { IsRead = true };
            }
        }

        UnreadCount = 0;
        OnChanged?.Invoke();
    }

    public void Remove(Guid id)
    {
        var index = _recent.FindIndex(n => n.Id == id);
        if (index >= 0)
        {
            if (!_recent[index].IsRead)
            {
                UnreadCount = Math.Max(0, UnreadCount - 1);
            }

            _recent.RemoveAt(index);
            OnChanged?.Invoke();
        }
    }

    public void Clear()
    {
        _recent.Clear();
        UnreadCount = 0;
        OnChanged?.Invoke();
    }
}
#pragma warning restore CA1003
