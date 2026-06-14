using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Domain;

public sealed class AssetRegistryDepreciationTests
{
    [Fact]
    public void Register_PpeAsset_SetsFivePercentResidual_AndStartsMonthAfterAcquisition()
    {
        var asset = NewPpe(unitCost: 60_000m, lifeYears: 5, acquisition: new DateOnly(2026, 1, 15));

        asset.ResidualValue.ShouldBe(3_000m);                      // 5% of 60,000
        asset.DepreciationStartDate.ShouldBe(new DateOnly(2026, 2, 1)); // month after acquisition
        asset.DepreciatedThrough.ShouldBeNull();
        asset.IsFullyDepreciated.ShouldBeFalse();
    }

    [Fact]
    public void MonthlyDepreciation_IsStraightLineNetOfResidual()
    {
        var asset = NewPpe(unitCost: 60_000m, lifeYears: 5, acquisition: new DateOnly(2026, 1, 15));

        // (60,000 - 3,000) / (5 * 12) = 57,000 / 60 = 950.00
        asset.MonthlyDepreciation().ShouldBe(950m);
    }

    [Fact]
    public void PostDepreciation_AccumulatesAndAdvancesDepreciatedThrough()
    {
        var asset = NewPpe(unitCost: 60_000m, lifeYears: 5, acquisition: new DateOnly(2026, 1, 15));
        var monthly = asset.MonthlyDepreciation();

        var charged = asset.PostDepreciation(new DateOnly(2026, 2, 1), monthly);

        charged.ShouldBe(950m);
        asset.AccumulatedDepreciation.ShouldBe(950m);
        asset.CarryingAmount.ShouldBe(59_050m);
        asset.DepreciatedThrough.ShouldBe(new DateOnly(2026, 2, 1));
    }

    [Fact]
    public void PostDepreciation_StopsAtResidualValue_WhenFullyDepreciated()
    {
        // 1-year life with an uneven monthly charge exercises the residual-floor cap on the final month.
        var asset = NewPpe(unitCost: 10_000m, lifeYears: 1, acquisition: new DateOnly(2026, 1, 15));
        var monthly = asset.MonthlyDepreciation(); // 9,500 / 12 = 791.67 (rounded)

        var period = new DateOnly(2026, 2, 1);
        for (var i = 0; i < 24; i++) // post well past useful life; should self-terminate at residual
        {
            if (asset.IsFullyDepreciated) break;
            asset.PostDepreciation(period, monthly);
            period = period.AddMonths(1);
        }

        asset.IsFullyDepreciated.ShouldBeTrue();
        asset.CarryingAmount.ShouldBe(asset.ResidualValue); // 500
        asset.AccumulatedDepreciation.ShouldBe(9_500m);     // never exceeds depreciable base
    }

    [Fact]
    public void PostDepreciation_RejectsAlreadyPostedPeriod()
    {
        var asset = NewPpe(unitCost: 60_000m, lifeYears: 5, acquisition: new DateOnly(2026, 1, 15));
        var monthly = asset.MonthlyDepreciation();
        asset.PostDepreciation(new DateOnly(2026, 2, 1), monthly);

        Should.Throw<InvalidOperationException>(() =>
            asset.PostDepreciation(new DateOnly(2026, 2, 1), monthly));
        Should.Throw<InvalidOperationException>(() =>
            asset.PostDepreciation(new DateOnly(2026, 1, 1), monthly)); // earlier period also rejected
    }

    [Fact]
    public void Register_UsesCatalogResidualPercent_WhenConfigured()
    {
        // Catalog policy overrides the 5% default with 10%.
        var catalog = PropertyItemCatalog.Create(
            tenantId: "root", code: "VEH-001", description: "Service Vehicle",
            defaultPropertyClass: "07-PPE", defaultCategoryCode: "VEH", defaultUnit: "unit",
            uacsObjectCode: "10406010", estimatedUsefulLifeYears: 7,
            residualValuePercent: 10m, depreciationMethod: DepreciationMethod.StraightLine);

        var asset = AssetRegistry.Register(
            tenantId: "root", catalog: catalog, assetType: AssetType.PPE, category: AssetCategory.PPE,
            propertyNo: PropertyNumber.Create("2026-NFA-00B-07-VEH-001"), description: "Service Vehicle",
            serialNo: null, brand: null, model: null, fundCluster: "01",
            acquisitionDate: new DateOnly(2026, 1, 15), unitCost: 1_000_000m,
            sourceIARId: null, sourcePurchaseOrderId: null);

        asset.ResidualValue.ShouldBe(100_000m); // 10% of 1,000,000
    }

    [Fact]
    public void UpdateDepreciation_OverridesResidualAndUsefulLife_Prospectively()
    {
        var asset = NewPpe(unitCost: 60_000m, lifeYears: 5, acquisition: new DateOnly(2026, 1, 15));
        asset.PostDepreciation(new DateOnly(2026, 2, 1), asset.MonthlyDepreciation()); // 950 posted

        asset.UpdateDepreciation(residualValue: 6_000m, estimatedUsefulLifeYears: 10, method: DepreciationMethod.StraightLine);

        asset.ResidualValue.ShouldBe(6_000m);
        asset.EstimatedUsefulLifeYears.ShouldBe(10);
        asset.AccumulatedDepreciation.ShouldBe(950m); // already-posted amount untouched
        // Prospective (COA change-in-estimate): remaining depreciable / remaining months.
        // Carrying 59,050 − residual 6,000 = 53,050 over (120 − 1) = 119 months = 445.80.
        asset.MonthlyDepreciation().ShouldBe(445.80m);
    }

    [Fact]
    public void MonthlyDepreciation_StaysConstant_MidLife_WhenNotOverridden()
    {
        // The prospective formula must equal a plain constant straight-line charge for an asset
        // that is part-way through its life with no change-in-estimate.
        var asset = NewPpe(unitCost: 60_000m, lifeYears: 5, acquisition: new DateOnly(2026, 1, 15));
        var period = new DateOnly(2026, 2, 1);
        for (var i = 0; i < 12; i++)
        {
            asset.PostDepreciation(period, 950m);
            period = period.AddMonths(1);
        }

        asset.AccumulatedDepreciation.ShouldBe(11_400m); // 12 × 950
        asset.MonthlyDepreciation().ShouldBe(950m);       // 45,600 remaining / 48 months
    }

    [Fact]
    public void UpdateDepreciation_RejectsResidualNotBelowCost()
    {
        var asset = NewPpe(unitCost: 60_000m, lifeYears: 5, acquisition: new DateOnly(2026, 1, 15));
        Should.Throw<InvalidOperationException>(() =>
            asset.UpdateDepreciation(residualValue: 60_000m, estimatedUsefulLifeYears: 5, method: DepreciationMethod.StraightLine));
    }

    [Fact]
    public void SemiExpendable_HasNoResidual_AndIsNeverDepreciated()
    {
        var asset = NewSe(unitCost: 4_000m, acquisition: new DateOnly(2026, 1, 15));

        asset.ResidualValue.ShouldBe(0m);
        asset.MonthlyDepreciation().ShouldBe(0m);
        asset.IsFullyDepreciated.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() =>
            asset.PostDepreciation(new DateOnly(2026, 2, 1), 100m));
    }

    private static AssetRegistry NewPpe(decimal unitCost, int lifeYears, DateOnly acquisition)
    {
        var catalog = PropertyItemCatalog.Create(
            tenantId: "root", code: "DESK-001", description: "Office Desk",
            defaultPropertyClass: "07-PPE", defaultCategoryCode: "DSK", defaultUnit: "pc",
            uacsObjectCode: "10405030", estimatedUsefulLifeYears: lifeYears);

        return AssetRegistry.Register(
            tenantId: "root", catalog: catalog, assetType: AssetType.PPE, category: AssetCategory.PPE,
            propertyNo: PropertyNumber.Create("2026-NFA-00B-07-DSK-001"), description: "Office Desk",
            serialNo: null, brand: null, model: null, fundCluster: "01",
            acquisitionDate: acquisition, unitCost: unitCost,
            sourceIARId: null, sourcePurchaseOrderId: null);
    }

    private static AssetRegistry NewSe(decimal unitCost, DateOnly acquisition)
    {
        var catalog = PropertyItemCatalog.Create(
            tenantId: "root", code: "CHAIR-001", description: "Monobloc Chair",
            defaultPropertyClass: "SE", defaultCategoryCode: "CHR", defaultUnit: "pc",
            uacsObjectCode: "10405020", estimatedUsefulLifeYears: 5);

        return AssetRegistry.Register(
            tenantId: "root", catalog: catalog, assetType: AssetType.SE, category: AssetCategory.LowValuedSemi,
            propertyNo: PropertyNumber.Create("2026-NFA-00B-SE-CHR-001"), description: "Monobloc Chair",
            serialNo: null, brand: null, model: null, fundCluster: "01",
            acquisitionDate: acquisition, unitCost: unitCost,
            sourceIARId: null, sourcePurchaseOrderId: null);
    }
}
