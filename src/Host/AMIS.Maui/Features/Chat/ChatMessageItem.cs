using AMIS.Maui.Services;

namespace AMIS.Maui.Features.Chat;

/// <summary>
/// UI projection of a <see cref="ChatMessageDto"/> for the conversation list. Resolves "mine" + the sender
/// label (needs the current user id, which the raw DTO doesn't carry) so the XAML template stays declarative.
/// </summary>
public sealed class ChatMessageItem
{
    public ChatMessageItem(ChatMessageDto dto, string? currentUserId)
    {
        Id = dto.Id;
        IsMine = currentUserId is not null && string.Equals(dto.SenderId, currentUserId, StringComparison.OrdinalIgnoreCase);
        SenderLabel = IsMine
            ? "You"
            : (!string.IsNullOrWhiteSpace(dto.SenderName) ? dto.SenderName! : Shorten(dto.SenderId));
        Body = dto.IsDeleted ? "message deleted" : (dto.Content ?? string.Empty);
        IsDeleted = dto.IsDeleted;
        IsPinned = dto.IsPinned && !dto.IsDeleted;
        Edited = dto.EditedOnUtc is not null && !dto.IsDeleted;
        TimeDisplay = dto.CreatedOnUtc.ToLocalTime().ToString("MMM d, h:mm tt");
    }

    public Guid Id { get; }
    public bool IsMine { get; }
    public string SenderLabel { get; }
    public string Body { get; }
    public bool IsDeleted { get; }
    public bool IsPinned { get; }
    public bool Edited { get; }
    public string TimeDisplay { get; }

    private static string Shorten(string id) => id.Length <= 8 ? id : id[..8];
}
