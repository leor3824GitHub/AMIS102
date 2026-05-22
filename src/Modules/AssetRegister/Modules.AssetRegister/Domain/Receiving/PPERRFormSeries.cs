using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.AssetRegister.Domain.Receiving;

/// <summary>
/// Represents a batch of pre-printed, pre-numbered PPERR accountable forms.
/// Only one series per tenant may be active at a time. The active series provides
/// the next <see cref="ReportNo"/> when a new PPERR is created.
/// </summary>
public sealed class PPERRFormSeries : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    /// <summary>Descriptive label, e.g. "FY 2026 Batch 1".</summary>
    public string Label { get; private set; } = default!;

    /// <summary>First serial number in the batch (inclusive).</summary>
    public int StartSerial { get; private set; }

    /// <summary>Last serial number in the batch (inclusive).</summary>
    public int EndSerial { get; private set; }

    /// <summary>Next serial number to be assigned. When > <see cref="EndSerial"/> the series is exhausted.</summary>
    public int NextSerial { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsExhausted => NextSerial > EndSerial;

    public int Remaining => IsExhausted ? 0 : EndSerial - NextSerial + 1;

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private PPERRFormSeries() { }

    public static PPERRFormSeries Create(string tenantId, string label, int startSerial, int endSerial)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        if (string.IsNullOrWhiteSpace(label))
            throw new InvalidOperationException("Series label is required.");
        if (startSerial <= 0)
            throw new InvalidOperationException("Start serial must be greater than zero.");
        if (endSerial < startSerial)
            throw new InvalidOperationException("End serial must be greater than or equal to start serial.");

        return new PPERRFormSeries
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Label = label,
            StartSerial = startSerial,
            EndSerial = endSerial,
            NextSerial = startSerial,
            IsActive = false,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void Activate()
    {
        if (IsExhausted)
            throw new InvalidOperationException($"Series '{Label}' is exhausted and cannot be activated.");
        IsActive = true;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Allocates the next serial and returns a formatted PPERR report number.
    /// Throws if the series is exhausted.
    /// </summary>
    public string AllocateNext()
    {
        if (IsExhausted)
            throw new InvalidOperationException($"Series '{Label}' is exhausted. Register a new series.");

        var serial = NextSerial;
        NextSerial++;
        if (IsExhausted) IsActive = false;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;

        var digits = EndSerial.ToString().Length;
        return $"PPERR-{serial.ToString($"D{digits}")}";
    }
}
