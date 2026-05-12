namespace FSH.Modules.AssetRegister.Domain.Accountability;

/// <summary>
/// Owned-entity profile attached to a <see cref="PropertyAccountabilityLine"/> when the
/// underlying asset is a vehicle. Captures odometer readings at issue/transfer/return
/// (per refinement §H — vehicle data lives on the line, not on AssetRegistry).
/// </summary>
public sealed class VehicleAccountabilityProfile
{
    public int? OdometerAtIssue { get; private set; }
    public int? OdometerAtReturn { get; private set; }
    public string? PlateNumber { get; private set; }
    public string? EngineNumber { get; private set; }
    public string? ChassisNumber { get; private set; }

    private VehicleAccountabilityProfile() { }

    internal static VehicleAccountabilityProfile Create(
        int? odometerAtIssue, string? plateNumber, string? engineNumber, string? chassisNumber)
    {
        if (odometerAtIssue is < 0)
            throw new InvalidOperationException("OdometerAtIssue must be non-negative.");
        return new VehicleAccountabilityProfile
        {
            OdometerAtIssue = odometerAtIssue,
            PlateNumber = plateNumber,
            EngineNumber = engineNumber,
            ChassisNumber = chassisNumber
        };
    }

    /// <summary>
    /// Vehicle odometer rule (Phase 3, refactoring guide rule #4): on return / transfer,
    /// the end reading must be strictly greater than the start reading.
    /// </summary>
    internal void RecordReturn(int? odometerAtReturn)
    {
        if (odometerAtReturn.HasValue && OdometerAtIssue.HasValue && odometerAtReturn.Value <= OdometerAtIssue.Value)
            throw new InvalidOperationException(
                $"Vehicle odometer at return ({odometerAtReturn}) must be strictly greater than at issue ({OdometerAtIssue}).");
        OdometerAtReturn = odometerAtReturn;
    }
}
