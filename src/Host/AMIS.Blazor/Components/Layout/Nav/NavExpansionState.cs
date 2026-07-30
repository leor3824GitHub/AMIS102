namespace AMIS.Blazor.Components.Layout.Nav;

/// <summary>
/// Remembers which groups the user has explicitly opened or closed.
/// </summary>
/// <remarks>
/// A group the user has not touched defaults to "open when the current route is inside it", so the
/// active page is never hidden behind a collapsed chevron. Because this is evaluated per render
/// rather than once in <c>OnInitializedAsync</c>, it also tracks client-side navigation — the
/// hand-written groups compute their expansion a single time at circuit start and go stale.
/// Once the user clicks a chevron, their choice wins for the rest of the session.
/// </remarks>
public sealed class NavExpansionState
{
    private readonly Dictionary<string, bool> _userChoice = new(StringComparer.Ordinal);

    /// <summary>Whether <paramref name="group"/> should render expanded for the current route.</summary>
    public bool IsExpanded(NavGroup group, string path)
    {
        ArgumentNullException.ThrowIfNull(group);
        return _userChoice.TryGetValue(group.Key, out var chosen) ? chosen : group.ContainsPath(path);
    }

    /// <summary>Records an explicit open/close from the user.</summary>
    public void Set(NavGroup group, bool expanded)
    {
        ArgumentNullException.ThrowIfNull(group);
        _userChoice[group.Key] = expanded;
    }
}
