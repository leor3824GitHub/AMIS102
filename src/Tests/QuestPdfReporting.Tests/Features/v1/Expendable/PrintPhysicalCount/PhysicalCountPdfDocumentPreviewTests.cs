using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintPhysicalCount;
using QuestPDF.Companion;
using Xunit;

namespace QuestPdfReporting.Tests.Features.v1.Expendable.PrintPhysicalCount;

public sealed class PhysicalCountPdfDocumentPreviewTests
{
    [Fact(Skip = "Preview only — remove Skip and run while QuestPDF Companion is listening.")]
    public void Show_PhysicalCount_InCompanion()
    {
        var doc = new PhysicalCountPdfDocument(
            items:       SampleItems(),
            org:         SampleOrg(),
            signatories: SampleSignatories(),
            asOfDate:    new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc));

        doc.ShowInCompanion();
    }

    private static List<PhysicalCountItemDto> SampleItems() =>
    [
        new(ArticleNumber: 1, Description: "Bond Paper, A4, 80gsm",
            StockNo: "OS-001", UnitOfMeasure: "Ream", UnitValue: 220.50m,
            BalancePerCard: 375, OnHandPerCount: 372,
            ShortageQuantity: 3, ShortageValue: 661.50m, Remarks: "3 reams missing"),
        new(ArticleNumber: 2, Description: "Ballpen, Black, 0.5mm",
            StockNo: "OS-018", UnitOfMeasure: "Piece", UnitValue: 8.75m,
            BalancePerCard: 1200, OnHandPerCount: 1200,
            ShortageQuantity: 0, ShortageValue: 0m, Remarks: null),
        new(ArticleNumber: 3, Description: "Stapler, Heavy Duty",
            StockNo: "OS-042", UnitOfMeasure: "Piece", UnitValue: 285m,
            BalancePerCard: 18, OnHandPerCount: 17,
            ShortageQuantity: 1, ShortageValue: 285m, Remarks: "Damaged; for disposal"),
        new(ArticleNumber: 4, Description: "Toner Cartridge, HP 12A",
            StockNo: "OS-095", UnitOfMeasure: "Piece", UnitValue: 3_250m,
            BalancePerCard: 6, OnHandPerCount: 6,
            ShortageQuantity: 0, ShortageValue: 0m, Remarks: null)
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
        new(Guid.NewGuid(), "PhysicalCount", 1, "Inventory Committee Chair:",   "Ana Reyes",  "Property Officer",  true),
        new(Guid.NewGuid(), "PhysicalCount", 2, "Inventory Committee Member:",  "Pedro Cruz", "Admin Officer V",   true),
        new(Guid.NewGuid(), "PhysicalCount", 3, "Approved by:",                 "Jose Rizal", "Regional Director", true)
    ];
}
