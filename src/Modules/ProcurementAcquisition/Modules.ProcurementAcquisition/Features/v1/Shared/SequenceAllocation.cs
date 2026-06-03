using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Shared;

/// <summary>
/// Helpers for the document-number allocation loops (PR / RIV / PO / IAR). Each allocates a serial from a
/// per-tenant counter row guarded by xmin optimistic concurrency, then retries on conflict.
/// </summary>
internal static class SequenceAllocation
{
    /// <summary>Number of attempts each allocation loop makes before giving up.</summary>
    internal const int MaxAttempts = 5;

    /// <summary>
    /// True when a <see cref="DbUpdateException"/> from saving a newly-allocated document number is a transient
    /// allocation conflict worth retrying, rather than a genuine error to surface. Two cases:
    /// <list type="bullet">
    /// <item><see cref="DbUpdateConcurrencyException"/> — another request advanced the counter row (xmin) between
    /// our read and save.</item>
    /// <item>Postgres unique-violation (23505) — two requests inserted the first counter row of a new period at
    /// once, or a freshly-created counter generated a number an older document (created before the counter
    /// existed) already holds. Retrying advances the serial past the collision, so the loop self-heals.</item>
    /// </list>
    /// </summary>
    internal static bool IsRetryableAllocationConflict(DbUpdateException ex) =>
        ex is DbUpdateConcurrencyException
        || ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
