using AMIS.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AMIS.Maui.Features.Chat;

/// <summary>
/// Row model for the channel list: the server DTO plus the device-local unread flag, which is the
/// one piece of channel state the API doesn't provide (see <see cref="ChatUnreadService"/>).
/// </summary>
public sealed partial class ChatChannelItem : ObservableObject
{
    public ChatChannelItem(ChatChannelDto channel, bool isUnread)
    {
        Channel = channel;
        IsUnread = isUnread;
    }

    public ChatChannelDto Channel { get; }

    public Guid Id => Channel.Id;
    public string DisplayName => Channel.DisplayName;
    public string? Topic => Channel.Topic;
    public string ScopeLabel => Channel.ScopeLabel;

    [ObservableProperty] public partial bool IsUnread { get; set; }
}
