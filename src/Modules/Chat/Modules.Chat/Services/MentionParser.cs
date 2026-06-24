using System.Text.RegularExpressions;

namespace AMIS.Modules.Chat.Services;

/// <summary>Extracts distinct <c>@username</c> tokens from message content.</summary>
internal static partial class MentionParser
{
    // Negative lookbehind for a word char so emails (foo@bar) aren't treated as mentions.
    [GeneratedRegex(@"(?<!\w)@([a-zA-Z0-9._-]{1,64})")]
    private static partial Regex MentionRegex();

    public static IReadOnlyList<string> Parse(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return [];
        }

        return MentionRegex().Matches(content)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
