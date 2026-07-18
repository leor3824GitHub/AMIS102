using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Domain;

/// <summary>
/// Depreciation continuity on inter-agency transfers and donations (COA GAM §V.B): the receiving agency
/// must inherit the sending agency's accumulated depreciation, not restart the asset at full cost.
/// </summary>
public sealed class AssetRegistryDepreciationCarryOverTests
{
    [Fact]
    public void Register_WithoutCarryOver_StartsUndepreciated()
    {
        var asset = NewPpe(unitCost: 60_000m, acquisition: new DateOnly(2026, 1, 15));

        asset.AccumulatedDepreciation.ShouldBe(0m);
        asset.DepreciatedThrough.ShouldBeNull();
        asset.CarryingAmount.ShouldBe(60_000m);
    }

    [Fact]
    public void Register_WithCarryOver_SeedsAccumulatedDepreciationAndCarryingAmount()
    {
        // A 4-year-old asset arriving from another agency with 45,600 already booked.
        var asset = NewPpe(
            unitCost: 60_000m,
            acquisition: new DateOnly(2022, 1, 15),
            accumulatedDepreciation: 45_600m,
            depreciationCurrentThrough: new DateOnly(2026, 1, 1));

        asset.AccumulatedDepreciation.ShouldBe(45_600m);
        asset.CarryingAmount.ShouldBe(14_400m);   // 60,000 − 45,600, not full cost
    }

    /// <summary>
    /// The regression this whole feature turns on. Seeding the amount WITHOUT the cursor would leave
    /// DepreciatedThrough null, and DepreciationPostingService starts its cursor at DepreciationStartDate
    /// when that is null — replaying every month since the original acquisition and double-charging the
    /// years the sending agency already booked.
    /// </summary>
    [Fact]
    public void Register_WithCarryOver_SeedsDepreciationCursor_SoThePostingServiceDoesNotReplayTheSchedule()
    {
        var asset = NewPpe(
            unitCost: 60_000m,
            acquisition: new DateOnly(2022, 1, 15),
            accumulatedDepreciation: 45_600m,
            depreciationCurrentThrough: new DateOnly(2026, 1, 1));

        asset.DepreciationStartDate.ShouldBe(new DateOnly(2022, 2, 1)); // sender's timeline is inherited
        asset.DepreciatedThrough.ShouldBe(new DateOnly(2026, 1, 1));    // ...and so is the cursor

        // The next chargeable period is the month after the carried cursor, not 2022-02.
        Should.Throw<InvalidOperationException>(() =>
            asset.PostDepreciation(new DateOnly(2026, 1, 1), 950m));
        asset.PostDepreciation(new DateOnly(2026, 2, 1), 950m).ShouldBe(950m);
        asset.AccumulatedDepreciation.ShouldBe(46_550m);
    }

    [Fact]
    public void Register_WithCarryOver_NormalizesCursorToFirstOfMonth()
    {
        var asset = NewPpe(
            unitCost: 60_000m,
            acquisition: new DateOnly(2022, 1, 15),
            accumulatedDepreciation: 45_600m,
            depreciationCurrentThrough: new DateOnly(2026, 1, 23));

        asset.DepreciatedThrough.ShouldBe(new DateOnly(2026, 1, 1));
    }

    [Fact]
    public void Register_CarryOverWithoutCursor_LeavesCursorNull()
    {
        // A donation where the source figure is known but the period it covers is not. The command
        // validator blocks this combination at the API edge; the domain simply does not invent a cursor.
        var asset = NewPpe(
            unitCost: 60_000m,
            acquisition: new DateOnly(2022, 1, 15),
            accumulatedDepreciation: 45_600m,
            depreciationCurrentThrough: null);

        asset.AccumulatedDepreciation.ShouldBe(45_600m);
        asset.DepreciatedThrough.ShouldBeNull();
    }

    [Fact]
    public void Register_RejectsCarryOverExceedingDepreciableAmount()
    {
        // Residual is 5% = 3,000, so the depreciable base is 57,000.
        Should.Throw<InvalidOperationException>(() => NewPpe(
            unitCost: 60_000m,
            acquisition: new DateOnly(2022, 1, 15),
            accumulatedDepreciation: 57_000.01m,
            depreciationCurrentThrough: new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void Register_RejectsNegativeCarryOver()
    {
        Should.Throw<InvalidOperationException>(() => NewPpe(
            unitCost: 60_000m,
            acquisition: new DateOnly(2022, 1, 15),
            accumulatedDepreciation: -1m,
            depreciationCurrentThrough: new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void Register_RejectsCarryOverOnSemiExpendable()
    {
        // SE is expensed on issue and never depreciates.
        var catalog = PropertyItemCatalog.Create(
            tenantId: "root", code: "CHAIR-001", description: "Monobloc Chair",
            defaultPropertyClass: "SE", defaultCategoryCode: "CHR", defaultUnit: "pc",
            uacsObjectCode: "10405020", estimatedUsefulLifeYears: 5);

        Should.Throw<InvalidOperationException>(() => AssetRegistry.Register(
            tenantId: "root", catalog: catalog, assetType: AssetType.SE, category: AssetCategory.LowValuedSemi,
            propertyNo: PropertyNumber.Create("2026-NFA-00B-SE-CHR-001"), description: "Monobloc Chair",
            serialNo: null, brand: null, model: null, fundCluster: "01",
            acquisitionDate: new DateOnly(2022, 1, 15), unitCost: 4_000m,
            sourceIARId: null, sourcePurchaseOrderId: null,
            accumulatedDepreciation: 1_000m, depreciationCurrentThrough: new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void Register_RejectsCursorEarlierThanDepreciationStart()
    {
        Should.Throw<InvalidOperationException>(() => NewPpe(
            unitCost: 60_000m,
            acquisition: new DateOnly(2026, 1, 15),           // depreciation starts 2026-02-01
            accumulatedDepreciation: 950m,
            depreciationCurrentThrough: new DateOnly(2025, 12, 1)));
    }

    private static AssetRegistry NewPpe(
        decimal unitCost,
        DateOnly acquisition,
        decimal accumulatedDepreciation = 0m,
        DateOnly? depreciationCurrentThrough = null)
    {
        var catalog = PropertyItemCatalog.Create(
            tenantId: "root", code: "DESK-001", description: "Office Desk",
            defaultPropertyClass: "07-PPE", defaultCategoryCode: "DSK", defaultUnit: "pc",
            uacsObjectCode: "10405030", estimatedUsefulLifeYears: 5);

        return AssetRegistry.Register(
            tenantId: "root", catalog: catalog, assetType: AssetType.PPE, category: AssetCategory.PPE,
            propertyNo: PropertyNumber.Create("2026-NFA-00B-07-DSK-001"), description: "Office Desk",
            serialNo: null, brand: null, model: null, fundCluster: "01",
            acquisitionDate: acquisition, unitCost: unitCost,
            sourceIARId: null, sourcePurchaseOrderId: null,
            accumulatedDepreciation: accumulatedDepreciation,
            depreciationCurrentThrough: depreciationCurrentThrough);
    }
}
