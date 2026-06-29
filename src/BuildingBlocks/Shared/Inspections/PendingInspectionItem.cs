namespace AMIS.Framework.Shared.Inspections;

/// <summary>
/// One item awaiting the current user's inspection, in a module-agnostic shape so a unified worklist
/// ("My Inspections") can merge items from any source. Each producing module maps its own pending records
/// to this shape and returns them from a self-scoped <c>pending-for-me</c> query.
/// </summary>
/// <param name="SourceType">"JobOrder" | "IAR" | "ReturnedProperty" — drives the row's type chip/icon. Kept a
/// free string (not an enum) so a new producer needs zero changes here; consumers fall back gracefully for
/// unknown values.</param>
/// <param name="SourceId">The underlying record's id.</param>
/// <param name="Reference">Human-facing reference (JO #, IAR #, accountability doc #).</param>
/// <param name="Title">Supplier / short description shown on the row.</param>
/// <param name="RequestedOnUtc">When it became pending (Issued / SubmittedForInspection / created) — the worklist sorts oldest-first.</param>
/// <param name="ActionRoute">SPA deep-link that opens the module's native inspect flow.</param>
public sealed record PendingInspectionItem(
    string SourceType,
    Guid SourceId,
    string Reference,
    string Title,
    DateTimeOffset RequestedOnUtc,
    string ActionRoute);
