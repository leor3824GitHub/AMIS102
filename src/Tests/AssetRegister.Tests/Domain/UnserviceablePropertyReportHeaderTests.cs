using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using AMIS.Modules.AssetRegister.Domain.Unserviceable;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Domain;

/// <summary>
/// Covers <see cref="UnserviceablePropertyReport.UpdateHeader"/>: it may rewrite the header fields
/// while Draft (ReportType stays immutable), and is refused once the report has left Draft.
/// </summary>
public sealed class UnserviceablePropertyReportHeaderTests
{
    private static readonly DateOnly Today = new(2026, 7, 16);

    [Fact]
    public void UpdateHeader_OnDraft_RewritesFields_ButNotReportType()
    {
        var report = NewDraft();

        report.UpdateHeader("02", "Main Warehouse", Today.AddDays(3),
            EmployeeRef.Create(Guid.NewGuid(), "New Officer", "Supply Officer"));

        report.FundCluster.ShouldBe("02");
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
            report.UpdateHeader("02", "Elsewhere", Today, EmployeeRef.Create(Guid.NewGuid(), "X", null)));
    }

    private static UnserviceablePropertyReport NewDraft() =>
        UnserviceablePropertyReport.CreateDraft(
            "t", "IIRUP-2026-07-0001", UnserviceableReportType.IIRUP, "01", station: "",
            Today, EmployeeRef.Create(Guid.NewGuid(), "Officer", null));

    private static AssetRegistry NewPpeAsset()
    {
        var catalog = PropertyItemCatalog.Create("t", "CAT", "Equipment", "DP", "05", "unit", "1060405", 5);
        return AssetRegistry.Register(
            "t", catalog, AssetType.PPE, AssetCategory.PPE, PropertyNumber.Create("2026-06-DP-0001"),
            "Laptop", null, null, null, "01", Today, 45000m, null, null);
    }
}
