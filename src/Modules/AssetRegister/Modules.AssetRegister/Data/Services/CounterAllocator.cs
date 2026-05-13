using FSH.Modules.AssetRegister.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Data.Services;

/// <summary>
/// Allocates the next serial against a (year, month, key) row in PropertyCodeCounter
/// using optimistic concurrency with bounded retry. The counter row is created on
/// first allocation.
/// </summary>
public sealed class CounterAllocator(AssetRegisterDbContext db)
{
    private const int MaxAttempts = 5;

    public async Task<int> NextSerialAsync(string tenantId, int year, int month, string counterKey, CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var counter = await db.PropertyCodeCounters
                .FirstOrDefaultAsync(c =>
                    c.Year == year &&
                    c.Month == month &&
                    c.CounterKey == counterKey, ct).ConfigureAwait(false);

            if (counter is null)
            {
                counter = PropertyCodeCounter.Create(tenantId, year, month, counterKey);
                db.PropertyCodeCounters.Add(counter);
            }

            var next = counter.NextSerial();

            try
            {
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                return next;
            }
            catch (DbUpdateConcurrencyException)
            {
                db.Entry(counter).State = EntityState.Detached;
            }
        }

        throw new InvalidOperationException(
            $"Failed to allocate next serial for counter '{counterKey}' after {MaxAttempts} attempts.");
    }
}
