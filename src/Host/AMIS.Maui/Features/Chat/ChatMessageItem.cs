using System.Globalization;
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
        SenderLabel = ResolveSenderLabel(dto, IsMine);
        Body = dto.IsDeleted ? "message deleted" : (dto.Content ?? string.Empty);
        IsDeleted = dto.IsDeleted;
        IsPinned = dto.IsPinned && !dto.IsDeleted;
        Edited = dto.EditedOnUtc is not null && !dto.IsDeleted;
        // Chat timestamps are shown to the operator, so format in their culture (not invariant).
        TimeDisplay = dto.CreatedOnUtc.ToLocalTime().ToString("MMM d, h:mm tt", CultureInfo.CurrentCulture);
    }

    public Guid Id { get; }
    public bool IsMine { get; }
    public string SenderLabel { get; }
    public string Body { get; }
    public bool IsDeleted { get; }
    public bool IsPinned { get; }
    public bool Edited { get; }
    public string TimeDisplay { get; }

    private static string ResolveSenderLabel(ChatMessageDto dto, bool isMine)
    {
        if (isMine) return "You";
        return !string.IsNullOrWhiteSpace(dto.SenderName) ? dto.SenderName! : Shorten(dto.SenderId);
    }

    private static string Shorten(string id) => id.Length <= 8 ? id : id[..8];
}
