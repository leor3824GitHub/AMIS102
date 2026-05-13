using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.AssetRegister.Domain.Catalog;

/// <summary>
/// Optimistic-concurrency counter used by every number generator. Unique key:
/// (TenantId, Year, Month, CounterKey). Known CounterKeys: SPLV, SPHV, PAR,
/// PPE-{sub-major}, ITR, RLSDDSP, IIRUSP, IIRUP, RSPI.
/// </summary>
public sealed class PropertyCodeCounter : AggregateRoot<Guid>, IHasTenant
{
    public string TenantId { get; private set; } = default!;
    public int Year { get; private set; }
    public int Month { get; private set; }
    public string CounterKey { get; private set; } = default!;
    public int LastSerial { get; private set; }

    private PropertyCodeCounter() { }

    public static PropertyCodeCounter Create(string tenantId, int year, int month, string counterKey) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Year = year,
            Month = month,
            CounterKey = counterKey,
            LastSerial = 0
        };

    public int NextSerial()
    {
        LastSerial++;
        return LastSerial;
    }
}

