using Microsoft.AspNetCore.Components.Routing;

namespace AMIS.Blazor.Components.Layout.Nav;

/// <summary>
/// A node in the side navigation tree — either a <see cref="NavItem"/> (a link) or a
/// <see cref="NavGroup"/> (a collapsible container of further entries).
/// </summary>
/// <remarks>
/// Visibility is <b>derived</b>, never declared twice. A group is visible only when at least one
/// of its descendants is visible, so a container whose children are all permission-gated can never
/// render as an empty heading or an empty chevron. That invariant is the reason this model exists:
/// in hand-written markup a group's permission check and its children's checks are separate
/// statements that drift apart as items are added.
/// </remarks>
public interface INavEntry
{
    /// <summary>True when the current user should see this entry.</summary>
    bool IsVisible(IReadOnlySet<string> permissions);

    /// <summary>True when <paramref name="path"/> resolves to this entry or one of its descendants.</summary>
    bool ContainsPath(string path);
}

/// <summary>A single navigable page.</summary>
public sealed record NavItem(string Label, string Href, string Icon) : INavEntry
{
    /// <summary>Permission required to see this item. <c>null</c> means always visible.</summary>
    public string? Permission { get; init; }

    /// <summary>
    /// Route matching mode. <see cref="NavLinkMatch.Prefix"/> suits most routes; use
    /// <see cref="NavLinkMatch.All"/> when this route is a prefix of a sibling's.
    /// </summary>
    public NavLinkMatch Match { get; init; } = NavLinkMatch.Prefix;

    /// <summary>
    /// Extra routes that should light this item up — pages reached from it that have no nav entry
    /// of their own (detail views, print pages).
    /// </summary>
    public IReadOnlyList<string> AlsoMatches { get; init; } = [];

    /// <inheritdoc />
    public bool IsVisible(IReadOnlySet<string> permissions) =>
        Permission is null || (permissions?.Contains(Permission) ?? false);

    /// <inheritdoc />
    public bool ContainsPath(string path) =>
        IsUnder(path, Href) || AlsoMatches.Any(r => IsUnder(path, r));

    /// <summary>
    /// Segment-anchored prefix match. Guards against <c>/vehicle/maintenance/logs</c> matching a
    /// <c>/logs</c> route, and <c>/procurement-planning/x</c> matching <c>/procurement</c>.
    /// </summary>
    private static bool IsUnder(string path, string route) =>
        string.Equals(path, route, StringComparison.OrdinalIgnoreCase) ||
        (path?.StartsWith(route + "/", StringComparison.OrdinalIgnoreCase) ?? false);
}

/// <summary>A collapsible container. Nests to any depth.</summary>
public sealed record NavGroup(string Key, string Title, string Icon) : INavEntry
{
    /// <summary>Child entries, in display order.</summary>
    public IReadOnlyList<INavEntry> Entries { get; init; } = [];

    /// <summary>
    /// Optional gate for the group as a whole, for conditions that are not permissions
    /// (e.g. root-tenant-admin). Children are still filtered independently.
    /// </summary>
    public Func<bool>? When { get; init; }

    /// <inheritdoc />
    public bool IsVisible(IReadOnlySet<string> permissions) =>
        (When?.Invoke() ?? true) && Entries.Any(e => e.IsVisible(permissions));

    /// <inheritdoc />
    public bool ContainsPath(string path) => Entries.Any(e => e.ContainsPath(path));

    /// <summary>Entries the current user may see, in declaration order.</summary>
    public IEnumerable<INavEntry> VisibleEntries(IReadOnlySet<string> permissions) =>
        Entries.Where(e => e.IsVisible(permissions));
}
