using AMIS.Modules.AssetRegister.Contracts.v1.Repairs;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintRepair;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Shouldly;
using Xunit;

namespace QuestPdfReporting.Tests.Features.v1.AssetRegister.PrintRepair;

public sealed class RepairPdfDocumentTests
{
    static RepairPdfDocumentTests() => QuestPDF.Settings.License = LicenseType.Community;

    [Fact]
    public void GeneratePdf_FullyPopulatedRpri_ProducesNonEmptyPdf()
    {
        var doc = new RepairPdfDocument(SampleRepair(RepairStatusValues.Accepted), SampleOrg());

        var bytes = doc.GeneratePdf();

        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(RepairStatusValues.Requested)]
    [InlineData(RepairStatusValues.PreInspected)]
    [InlineData(RepairStatusValues.PostInspected)]
    [InlineData(RepairStatusValues.Accepted)]
    public void GeneratePdf_AnyStatus_RendersWithoutLayoutErrors(string status)
    {
        var doc = new RepairPdfDocument(SampleRepair(status), org: null);

        var bytes = doc.GeneratePdf();

        bytes.Length.ShouldBeGreaterThan(0);
    }

    private static PropertyRepairDto SampleRepair(string status) => new(
        Id: Guid.NewGuid(),
        AssetRegistryId: Guid.NewGuid(),
        RpriNo: "RPRI-202606-AB12CD",
        Status: status,
        PropertyNo: "2026-06-LT-0001",
        Description: "Toyota Innova",
        SerialNo: "SN-123",
        Brand: "Toyota",
        Model: "Innova",
        AcquisitionDate: new DateOnly(2020, 1, 15),
        AcquisitionCost: 950_000m,
        NatureOfWork: "Replace brake pads and change engine oil",
        PartsToReplace: "Front brake pads, oil filter",
        RequestedBy: "Juan dela Cruz",
        RequestedOn: new DateOnly(2026, 6, 1),
        EngineNo: "4ZZ-000123",
        ChassisNo: "J28S-000456",
        OdometerReading: 450_000,
        NatureOfLastRepair: "Aircon recharge",
        DateOfLastRepair: new DateOnly(2025, 12, 1),
        PreInspectionFindings: "Brake pads worn beyond limit.",
        PreInspectedBy: "Inspector A",
        NotedBy: "Head of Office",
        PreInspectedOn: new DateOnly(2026, 6, 2),
        RepairShop: "ABC Auto Repair",
        JobOrderNo: "JO-2026-001",
        InvoiceNo: "INV-555",
        InvoiceDate: new DateOnly(2026, 6, 10),
        AmountPerJO: 8_500m,
        PostInspectionFindings: "Repair completed satisfactorily.",
        PostInspectedBy: "Inspector A",
        PostInspectedOn: new DateOnly(2026, 6, 12),
        PrNo: "PR-2026-100",
        PoJoNo: "PO-2026-200",
        BurNo: "BUR-2026-300",
        DvNo: "DV-2026-400",
        AcceptedBy: "Property Custodian",
        AcceptedOn: new DateOnly(2026, 6, 13));

    private static OrganizationProfileDto SampleOrg() =>
        new(Id: Guid.NewGuid(),
            Name: "Department of Sample Affairs",
            ShortName: "DSA",
            Address: "1234 Sample Street, Manila, Philippines",
            LogoUrl: null,
            AnnexECode: "DSA-NCR");
}
