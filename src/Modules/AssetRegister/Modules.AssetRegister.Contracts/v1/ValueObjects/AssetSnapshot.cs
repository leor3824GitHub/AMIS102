namespace AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;

/// <summary>
/// Frozen subset of <c>AssetRegistry</c> captured at the moment of issue, count,
/// incident, or disposal. Configured as an EF owned type on every line/entry/item
/// that references it. Survives renames and field drift on the master record.
/// </summary>
public sealed class AssetSnapshot
{
    public string PropertyNo { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public AssetType AssetType { get; private set; }
    public decimal UnitCost { get; private set; }
    public string Unit { get; private set; } = default!;
    public int EstimatedUsefulLifeYears { get; private set; }
    public DateOnly AcquisitionDate { get; private set; }
    public string? UacsObjectCode { get; private set; }
    public string? SerialNo { get; private set; }
    public string? Brand { get; private set; }
    public string? Model { get; private set; }

    private AssetSnapshot() { }

    public static AssetSnapshot Create(
        string propertyNo,
        string description,
        AssetType assetType,
        decimal unitCost,
        string unit,
        int estimatedUsefulLifeYears,
        DateOnly acquisitionDate,
        string? uacsObjectCode,
        string? serialNo,
        string? brand,
        string? model) =>
        new()
        {
            PropertyNo = propertyNo,
            Description = description,
            AssetType = assetType,
            UnitCost = unitCost,
            Unit = unit,
            EstimatedUsefulLifeYears = estimatedUsefulLifeYears,
            AcquisitionDate = acquisitionDate,
            UacsObjectCode = uacsObjectCode,
            SerialNo = serialNo,
            Brand = brand,
            Model = model
        };
}

