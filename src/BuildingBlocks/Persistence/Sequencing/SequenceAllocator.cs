using AMIS.Framework.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AMIS.Framework.Persistence.Sequencing;

/// <summary>
/// Shared document-number allocation over the <see cref="NumberSequence"/> counter table. Consolidates the
/// per-module allocation loops (PR/PO/JO/RIV/IAR/BUR/DV) into one implementation.
/// <para>
/// The allocator operates on the <b>caller's tracked <see cref="DbContext"/></b> and never saves on its own:
/// the counter increment is committed by the caller's own <c>SaveChanges</c>, in the same transaction as the
/// document insert. That keeps two guarantees the old inline loops relied on — a rolled-back document never
/// burns a number, and the loop self-heals when the document's own unique-number index (not just the counter
/// row) collides.
/// </para>
/// </summary>
public static class SequenceAllocator
{
    /// <summary>Number of attempts each allocation loop makes before giving up.</summary>
    public const int MaxAttempts = 5;

    /// <summary>
    /// True when a <see cref="DbUpdateException"/> from saving a newly-allocated document number is a transient
    /// allocation conflict worth retrying, rather than a genuine error. Two cases:
    /// <list type="bullet">
    /// <item><see cref="DbUpdateConcurrencyException"/> — another request advanced the counter row (xmin)
    /// between our read and save.</item>
    /// <item>Postgres unique-violation (23505) — two requests inserted the first counter row of a new period
    /// at once, or a freshly-created counter generated a number an older document already holds. Retrying
    /// advances the serial past the collision, so the loop self-heals.</item>
    /// </list>
    /// </summary>
    public static bool IsRetryableAllocationConflict(DbUpdateException ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        return ex is DbUpdateConcurrencyException
            || ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }

    /// <summary>
    /// Loads (or creates on first use) the tracked counter row for (<paramref name="tenantId"/>,
    /// <paramref name="sequenceKey"/>, <paramref name="year"/>, <paramref name="month"/>) on
    /// <paramref name="ctx"/>, WITHOUT incrementing it. The caller advances it via
    /// <see cref="NumberSequence.NextSerial"/> — call it once per number needed (e.g. a batch that mints
    /// several documents from one row in a single save). Does NOT save.
    /// <para>
    /// <paramref name="seedFactory"/> runs only when the row is first created; use it to seed
    /// <see cref="NumberSequence.LastSerial"/> from the highest number already issued (e.g. after a counter
    /// reset) so allocation never re-collides with an existing document number. Pass <c>null</c> to start at 0.
    /// </para>
    /// The read uses <c>IgnoreQueryFilters</c> with an explicit tenant match so it is unaffected by the
    /// ambient multi-tenant query filter, matching the previous per-module behavior.
    /// </summary>
    public static async Task<NumberSequence> GetOrCreateRowAsync(
        DbContext ctx,
        string tenantId,
        string sequenceKey,
        int year,
        int month,
        CancellationToken ct,
        Func<Task<int>>? seedFactory = null)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var row = await ctx.Set<NumberSequence>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId
                     && x.SequenceKey == sequenceKey
                     && x.Year == year
                     && x.Month == month,
                ct)
            .ConfigureAwait(false);

        if (row is null)
        {
            var seed = seedFactory is null ? 0 : await seedFactory().ConfigureAwait(false);
            row = NumberSequence.Create(tenantId, sequenceKey, year, month, seed);
            ctx.Set<NumberSequence>().Add(row);
        }

        return row;
    }

    /// <summary>
    /// Reserves the next serial: <see cref="GetOrCreateRowAsync"/> followed by one
    /// <see cref="NumberSequence.NextSerial"/>. Does NOT save — the caller commits it alongside the document.
    /// </summary>
    public static async Task<int> ReserveNextSerialAsync(
        DbContext ctx,
        string tenantId,
        string sequenceKey,
        int year,
        int month,
        CancellationToken ct,
        Func<Task<int>>? seedFactory = null)
    {
        var row = await GetOrCreateRowAsync(ctx, tenantId, sequenceKey, year, month, ct, seedFactory)
            .ConfigureAwait(false);
        return row.NextSerial();
    }

    /// <summary>
    /// Runs the full allocate-and-save loop for a single document: reserve one serial, let
    /// <paramref name="buildAndTrack"/> format the number and add the document to <paramref name="ctx"/>, then
    /// save. On a retryable conflict the change tracker is cleared and the attempt repeats (up to
    /// <see cref="MaxAttempts"/>). Returns whatever <paramref name="buildAndTrack"/> produced on the successful
    /// attempt — commonly the entity, which the caller maps to a DTO <b>after</b> this returns so post-save
    /// audit fields are populated.
    /// <para>
    /// <paramref name="buildAndTrack"/> receives the reserved serial, formats the document number, creates the
    /// document and <c>Add</c>s it to the context, and returns it (or a result derived from it).
    /// <paramref name="seedFactory"/> optionally seeds a first-created counter row (see
    /// <see cref="GetOrCreateRowAsync"/>). <paramref name="onRetryAsync"/> is an optional hook invoked after
    /// <c>ChangeTracker.Clear()</c> on each retry, to re-establish tracked state the clear detached (e.g.
    /// re-fetching a related aggregate the document must also mutate).
    /// </para>
    /// </summary>
    public static async Task<T> AllocateAsync<T>(
        DbContext ctx,
        string tenantId,
        string sequenceKey,
        int year,
        int month,
        Func<int, T> buildAndTrack,
        CancellationToken ct,
        Func<Task<int>>? seedFactory = null,
        Func<CancellationToken, Task>? onRetryAsync = null)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(buildAndTrack);

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var serial = await ReserveNextSerialAsync(ctx, tenantId, sequenceKey, year, month, ct, seedFactory)
                .ConfigureAwait(false);

            var result = buildAndTrack(serial);

            try
            {
                await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
                return result;
            }
            catch (DbUpdateException ex) when (IsRetryableAllocationConflict(ex))
            {
                ctx.ChangeTracker.Clear();
                if (onRetryAsync is not null)
                    await onRetryAsync(ct).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException(
            $"Failed to allocate a unique '{sequenceKey}' number after {MaxAttempts} attempts. Please try again.");
    }
}
