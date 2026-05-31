using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintStockCard;
using QuestPDF.Companion;
using Xunit;

namespace QuestPdfReporting.Tests.Features.v1.Expendable.PrintStockCard;

public sealed class StockCardPdfDocumentPreviewTests
{
    [Fact(Skip = "Preview only — remove Skip and run while QuestPDF Companion is listening.")]
    public void Show_StockCard_InCompanion()
    {
        var doc = new StockCardPdfDocument(SampleCard(), SampleOrg());
        doc.ShowInCompanion();
    }

    private static StockCardDto SampleCard() =>
        new(ProductId: Guid.NewGuid(),
            ProductCode: "OS-001",
            ProductName: "Bond Paper, A4, 80gsm",
            UnitOfMeasure: "Ream",
            Lines:
            [
                new(Date: new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero),
                    Reference: "PO-2026-001", TransactionType: "Receipt", Office: null,
                    ReceiptQty: 500, ReceiptUnitCost: 220.50m, ReceiptTotalCost: 110_250m,
                    IssueQty:   0,   IssueUnitCost:   0,        IssueTotalCost:   0,
                    BalanceQty: 500, BalanceUnitCost: 220.50m, BalanceTotalCost: 110_250m),
                new(Date: new DateTimeOffset(2026, 1, 12, 10, 30, 0, TimeSpan.Zero),
                    Reference: "RIS-2026-014", TransactionType: "Issue", Office: "Admin Division",
                    ReceiptQty: 0,   ReceiptUnitCost: 0,       ReceiptTotalCost: 0,
                    IssueQty:   50,  IssueUnitCost:   220.50m, IssueTotalCost:   11_025m,
                    BalanceQty: 450, BalanceUnitCost: 220.50m, BalanceTotalCost: 99_225m),
                new(Date: new DateTimeOffset(2026, 1, 20, 14, 0, 0, TimeSpan.Zero),
                    Reference: "RIS-2026-027", TransactionType: "Issue", Office: "Finance Division",
                    ReceiptQty: 0,   ReceiptUnitCost: 0,       ReceiptTotalCost: 0,
                    IssueQty:   75,  IssueUnitCost:   220.50m, IssueTotalCost:   16_537.50m,
                    BalanceQty: 375, BalanceUnitCost: 220.50m, BalanceTotalCost: 82_687.50m)
            ]);

    private static OrganizationProfileDto SampleOrg() =>
        new(Id: Guid.NewGuid(),
            Name: "Department of Sample Affairs",
            ShortName: "DSA",
            Address: "1234 Sample Street, Manila, Philippines",
            LogoUrl: null,
            AnnexECode: "DSA-NCR");
}
