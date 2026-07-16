namespace AMIS.Blazor.Common;

/// <summary>
/// Converts a calendar date chosen in a <c>MudDatePicker</c> into the zero-offset
/// <see cref="DateTimeOffset"/> the report/query endpoints expect.
///
/// A picker's value carries whatever <see cref="DateTimeKind"/> its source had —
/// <see cref="DateTime.Today"/> defaults are <see cref="DateTimeKind.Local"/>. Passing a
/// Local (or Utc) <see cref="DateTime"/> straight into <c>new DateTimeOffset(value, TimeSpan.Zero)</c>
/// throws "The UTC Offset of the local dateTime parameter does not match the offset argument"
/// on any machine whose local offset isn't zero (e.g. UTC+8 Philippine time). Forcing the Kind
/// to Unspecified makes the offset argument authoritative, so the calendar date is interpreted as
/// midnight at zero offset regardless of the server/browser time zone — the boundary the
/// UTC-timestamp report queries compare against.
/// </summary>
public static class FilterDate
{
    /// <summary>Interpret a picked calendar date as midnight at zero offset (UTC).</summary>
    public static DateTimeOffset ToUtcDateBoundary(this DateTime date) =>
        new(DateTime.SpecifyKind(date, DateTimeKind.Unspecified), TimeSpan.Zero);

    /// <summary>Nullable overload — passes <c>null</c> through for "no filter".</summary>
    public static DateTimeOffset? ToUtcDateBoundary(this DateTime? date) =>
        date.HasValue ? date.Value.ToUtcDateBoundary() : null;
}
