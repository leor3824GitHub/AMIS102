using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintDepartmentIssuance;
using QuestPDF.Companion;
using Xunit;

namespace QuestPdfReporting.Tests.Features.v1.Expendable.PrintDepartmentIssuance;

public sealed class DepartmentIssuancePdfDocumentPreviewTests
{
    [Fact(Skip = "Preview only — remove Skip and run while QuestPDF Companion is listening.")]
    public void Show_DepartmentIssuance_InCompanion()
    {
        var doc = new DepartmentIssuancePdfDocument(
            data:            SampleData(),
            org:             SampleOrg(),
            signatories:     SampleSignatories(),
            from:            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            to:              new DateTimeOffset(2026, 1, 31, 23, 59, 0, TimeSpan.Zero),
            departmentNames: SampleDepartmentNames());

        doc.ShowInCompanion();
    }

    private static List<DepartmentIssuanceSummaryDto> SampleData() =>
    [
        new(DepartmentId: "DEPT-ADMIN",
            TotalRequestsFulfilled: 4, TotalItemsIssued: 124, TotalValue: 18_240.50m,
            Products:
            [
                new(Guid.NewGuid(), "Bond Paper, A4, 80gsm", "OS-001", 80, 17_640m, "Ream", 220.50m),
                new(Guid.NewGuid(), "Ballpen, Black, 0.5mm", "OS-018", 44, 385m,    "Piece", 8.75m)
            ]),
        new(DepartmentId: "DEPT-FIN",
            TotalRequestsFulfilled: 2, TotalItemsIssued: 7, TotalValue: 22_750m,
            Products:
            [
                new(Guid.NewGuid(), "Toner Cartridge, HP 12A", "OS-095", 7, 22_750m, "Piece", 3_250m)
            ])
    ];

    private static OrganizationProfileDto SampleOrg() =>
        new(Id: Guid.NewGuid(),
            Name: "Department of Sample Affairs",
            ShortName: "DSA",
            Address: "1234 Sample Street, Manila, Philippines",
            LogoUrl: null,
            AnnexECode: "DSA-NCR");

    private static List<ReportSignatoryDto> SampleSignatories() =>
    [
        new(Guid.NewGuid(), "RSMI", 1, "Prepared by:",       "Ana Reyes",  "Property Officer",  true),
        new(Guid.NewGuid(), "RSMI", 2, "Certified Correct:", "Pedro Cruz", "Admin Officer V",   true),
        new(Guid.NewGuid(), "RSMI", 3, "Approved by:",       "Jose Rizal", "Regional Director", true)
    ];

    private static Dictionary<string, string> SampleDepartmentNames() => new()
    {
        ["DEPT-ADMIN"] = "Administrative Division",
        ["DEPT-FIN"]   = "Finance Division"
    };
}
