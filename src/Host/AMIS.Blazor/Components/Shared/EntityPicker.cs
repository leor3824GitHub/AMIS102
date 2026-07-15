namespace AMIS.Blazor.Components.Shared;

/// <summary>
/// Load request handed to an <see cref="EntityPickerDialog{T}"/> server-side data provider,
/// carrying the current keyword and paging position.
/// </summary>
public sealed record EntityPickerRequest(string? Keyword, int Page, int PageSize);

/// <summary>
/// A page of results returned by an <see cref="EntityPickerDialog{T}"/> server-side data provider.
/// <paramref name="TotalCount"/> is the full (unpaged) count used to drive pagination.
/// </summary>
public sealed record EntityPickerResult<T>(IReadOnlyList<T> Items, int TotalCount);

/// <summary>
/// Per-row context passed to the <c>RowTemplate</c> of an <see cref="EntityPickerDialog{T}"/>.
/// <paramref name="IsExcluded"/> is true when the row's id is in <c>ExcludeIds</c> (already picked
/// elsewhere) — the checkbox is disabled and consumers typically render an "(already added)" hint.
/// <paramref name="IsLocked"/> is true when the row is disabled by the dialog's <c>RowSelectable</c>
/// rule given the current selection (e.g. a mutually-exclusive category) — consumers typically grey
/// the row and render a short reason.
/// </summary>
public sealed record EntityPickerRow<T>(T Item, bool IsExcluded, bool IsSelected, bool IsLocked = false);

/// <summary>How many rows the picker lets the user select before confirming.</summary>
public enum EntityPickerSelection
{
    /// <summary>One row at a time; confirming returns a single <c>T</c> (or null).</summary>
    Single,

    /// <summary>Any number of rows; confirming returns a <c>List&lt;T&gt;</c>.</summary>
    Multiple
}
