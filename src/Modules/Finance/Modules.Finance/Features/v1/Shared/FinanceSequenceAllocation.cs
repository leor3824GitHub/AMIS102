using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AMIS.Modules.Finance.Features.v1.Shared;

/// <summary>
/// Helpers for the document-number allocation loops (DV / BUR). Each allocates a serial from a per-year
/// counter row guarded by xmin optimistic concurrency, then retries on conflict. Mirrors
/// <c>SequenceAllocation</c> in Modules.ProcurementAcquisition.
/// </summary>
internal static class FinanceSequenceAllocation
{
    /// <summary>Number of attempts each allocation loop makes before giving up.</summary>
    internal const int MaxAttempts = 5;

    /// <summary>
    /// True when a <see cref="DbUpdateException"/> from saving a newly-allocated document number is a transient
    /// allocation conflict worth retrying: either another request advanced the counter row (xmin →
    /// <see cref="DbUpdateConcurrencyException"/>), or two requests collided on a unique index
    /// (Postgres 23505 — the counter row's year, or the generated DvNumber/BurNumber). Retrying advances the
    /// serial past the collision, so the loop self-heals.
    /// </summary>
    internal static bool IsRetryableAllocationConflict(DbUpdateException ex) =>
        ex is DbUpdateConcurrencyException
        || ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    /// <summary>
    /// Extracts the trailing serial from a document number like <c>DV-2026-00042</c> → <c>42</c>.
    /// Returns 0 when the trailing segment isn't a number. Used to seed a missing per-year counter row
    /// from the documents already issued, so allocation never re-collides with an existing number.
    /// </summary>
    internal static int ParseSerial(string documentNumber)
    {
        var lastDash = documentNumber.LastIndexOf('-');
        return lastDash >= 0 && int.TryParse(documentNumber.AsSpan(lastDash + 1), out var serial)
            ? serial
            : 0;
    }
}
