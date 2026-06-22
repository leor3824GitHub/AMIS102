using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.AssetRegister.Domain.Issuance;

/// <summary>
/// Represents a batch of pre-printed, pre-numbered PPEIR accountable forms.
/// Only one series per tenant may be active at a time. The active series provides
/// the next <see cref="ReportNo"/> when a new PPEIR is created.
/// </summary>
public sealed class PPEIRFormSeries : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
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

    /// <summary>True when no PPEIR number has yet been allocated from this series — safe to edit or delete.</summary>
    public bool IsUnused => NextSerial == StartSerial;

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private PPEIRFormSeries() { }

    public static PPEIRFormSeries Create(string tenantId, string label, int startSerial, int endSerial)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        if (string.IsNullOrWhiteSpace(label))
            throw new InvalidOperationException("Series label is required.");
        if (startSerial <= 0)
            throw new InvalidOperationException("Start serial must be greater than zero.");
        if (endSerial < startSerial)
            throw new InvalidOperationException("End serial must be greater than or equal to start serial.");

        return new PPEIRFormSeries
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

    /// <summary>
    /// Update the descriptive label and serial range. Only allowed while the series is unused
    /// (no PPEIR numbers have been allocated). Range changes after allocation would invalidate
    /// already-issued report numbers.
    /// </summary>
    public void UpdateRange(string label, int startSerial, int endSerial)
    {
        if (!IsUnused)
            throw new InvalidOperationException(
                $"Series '{Label}' has already issued PPEIR numbers and cannot be edited.");
        if (string.IsNullOrWhiteSpace(label))
            throw new InvalidOperationException("Series label is required.");
        if (startSerial <= 0)
            throw new InvalidOperationException("Start serial must be greater than zero.");
        if (endSerial < startSerial)
            throw new InvalidOperationException("End serial must be greater than or equal to start serial.");

        Label = label;
        StartSerial = startSerial;
        EndSerial = endSerial;
        NextSerial = startSerial;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
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
    /// Allocates the next serial and returns a formatted PPEIR report number.
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
        return $"PPEIR-{serial.ToString($"D{digits}")}";
    }
}
