using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using AMIS.Modules.AssetRegister.Domain.Unserviceable;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Domain;

/// <summary>
/// Covers <see cref="UnserviceablePropertyReport.UpdateHeader"/> (rewrites the header fields while Draft,
/// ReportType and FundCluster excepted) and the derive-on-first-item fund cluster rule on
/// <see cref="UnserviceablePropertyReport.AddItem"/>.
/// </summary>
public sealed class UnserviceablePropertyReportHeaderTests
{
    private static readonly DateOnly Today = new(2026, 7, 16);

    [Fact]
    public void UpdateHeader_OnDraft_RewritesFields_ButNotReportType()
    {
        var report = NewDraft();

        report.UpdateHeader("Main Warehouse", Today.AddDays(3),
            EmployeeRef.Create(Guid.NewGuid(), "New Officer", "Supply Officer"));

        report.Station.ShouldBe("Main Warehouse");
        report.AsAt.ShouldBe(Today.AddDays(3));
        report.AccountableOfficer.PrintedName.ShouldBe("New Officer");
        report.AccountableOfficer.Designation.ShouldBe("Supply Officer");
        report.ReportType.ShouldBe(UnserviceableReportType.IIRUP); // immutable
    }

    [Fact]
    public void UpdateHeader_AfterSubmit_Throws()
    {
        var report = NewDraft();
        report.AddItem(NewPpeAsset(), remarks: null);
        report.Submit(EmployeeRef.Create(Guid.NewGuid(), "Approver", null));

        Should.Throw<InvalidOperationException>(() =>
            report.UpdateHeader("Elsewhere", Today, EmployeeRef.Create(Guid.NewGuid(), "X", null)));
    }

    [Fact]
    public void FundCluster_IsBlank_UntilFirstItem_ThenStampedFromAsset()
    {
        var report = NewDraft();
        report.FundCluster.ShouldBe(string.Empty);

        report.AddItem(NewPpeAsset(fundCluster: "05"), remarks: null);

        report.FundCluster.ShouldBe("05"); // inherited from the first item, never typed
    }

    [Fact]
    public void AddItem_WithMismatchedFundCluster_Throws()
    {
        var report = NewDraft();
        report.AddItem(NewPpeAsset("2026-06-DP-0001", fundCluster: "05"), remarks: null);

        Should.Throw<InvalidOperationException>(() =>
            report.AddItem(NewPpeAsset("2026-06-DP-0002", fundCluster: "01"), remarks: null));

        report.Items.Count.ShouldBe(1); // the mismatched item was rejected
    }

    private static UnserviceablePropertyReport NewDraft() =>
        UnserviceablePropertyReport.CreateDraft(
            "t", "IIRUP-2026-07-0001", UnserviceableReportType.IIRUP, station: "",
            Today, EmployeeRef.Create(Guid.NewGuid(), "Officer", null));

    private static AssetRegistry NewPpeAsset(string propertyNo = "2026-06-DP-0001", string fundCluster = "01")
    {
        var catalog = PropertyItemCatalog.Create("t", "CAT", "Equipment", "DP", "05", "unit", "1060405", 5);
        return AssetRegistry.Register(
            "t", catalog, AssetType.PPE, AssetCategory.PPE, PropertyNumber.Create(propertyNo),
            "Laptop", null, null, null, fundCluster, Today, 45000m, null, null);
    }
}
