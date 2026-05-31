using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintStockCard;

internal sealed class StockCardPdfDocument(StockCardDto card, OrganizationProfileDto? org) : IDocument
{
    public DocumentMetadata GetMetadata() => new()
    {
        Title  = $"Stock Card — {card.ProductCode} {card.ProductName}",
        Author = org?.Name ?? string.Empty
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial").FontColor(Colors.Black));
            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeBody);
            page.Footer().AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(7);
                x.CurrentPageNumber().FontSize(7);
                x.Span(" of ").FontSize(7);
                x.TotalPages().FontSize(7);
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text("Republic of the Philippines").FontSize(9);
            if (org is not null)
            {
                col.Item().AlignCenter().Text(org.Name).Bold().FontSize(11);
                if (!string.IsNullOrWhiteSpace(org.Address))
                    col.Item().AlignCenter().Text(org.Address).FontSize(8);
            }
            col.Item().PaddingTop(6).AlignCenter().Text("STOCK CARD").Bold().FontSize(13);
            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(); c.RelativeColumn(3);
                    c.RelativeColumn(); c.RelativeColumn(3);
                });
                table.Cell().Text("Entity Name:").Bold();
                table.Cell().Text(org?.Name ?? string.Empty);
                table.Cell().Text("Item:").Bold();
                table.Cell().Text(card.ProductName);
                table.Cell().Text("Stock No.:").Bold();
                table.Cell().Text(card.ProductCode);
                table.Cell().Text("Unit:").Bold();
                table.Cell().Text(card.UnitOfMeasure);
                table.Cell().Text("Re-order Point:").Bold();
                table.Cell().Text("—");
            });
            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    }

    private void ComposeBody(IContainer container)
    {
        container.PaddingTop(4).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(60); c.RelativeColumn(3); c.RelativeColumn(2);
                c.ConstantColumn(40); c.RelativeColumn(2); c.RelativeColumn(2.5f);
                c.ConstantColumn(40); c.RelativeColumn(2); c.RelativeColumn(2.5f);
                c.ConstantColumn(40); c.RelativeColumn(2); c.RelativeColumn(2.5f);
            });

            var hStyle = TextStyle.Default.Bold().FontSize(8).FontColor(Colors.Black);

            table.Header(h =>
            {
                h.Cell().RowSpan(2).Border(1).Padding(2).AlignCenter().Text("Date").Style(hStyle);
                h.Cell().RowSpan(2).Border(1).Padding(2).AlignCenter().Text("Reference").Style(hStyle);
                h.Cell().RowSpan(2).Border(1).Padding(2).AlignCenter().Text("Office / Dept").Style(hStyle);
                h.Cell().ColumnSpan(3).Border(1).Padding(2).AlignCenter().Text("Receipt / Beginning Balance").Style(hStyle);
                h.Cell().ColumnSpan(3).Border(1).Padding(2).AlignCenter().Text("Issuance").Style(hStyle);
                h.Cell().ColumnSpan(3).Border(1).Padding(2).AlignCenter().Text("Balance").Style(hStyle);
                for (var i = 0; i < 3; i++)
                {
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Qty").Style(hStyle);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Unit Cost").Style(hStyle);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Total").Style(hStyle);
                }
            });

            foreach (var line in card.Lines)
            {
                var isReceipt = line.TransactionType == "Receipt";

                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(line.Date.LocalDateTime.ToString("MM/dd/yyyy")).FontSize(8).FontColor(Colors.Black);
                table.Cell().Border(0.5f).Padding(2).Text(line.Reference).FontSize(8).FontColor(Colors.Black);
                table.Cell().Border(0.5f).Padding(2).Text(line.Office ?? string.Empty).FontSize(8).FontColor(Colors.Black);

                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(isReceipt ? line.ReceiptQty.ToString() : "—").FontSize(8).FontColor(Colors.Black);
                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(isReceipt ? line.ReceiptUnitCost.ToString("N4") : "—").FontSize(8).FontColor(Colors.Black);
                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(isReceipt ? line.ReceiptTotalCost.ToString("N2") : "—").FontSize(8).FontColor(Colors.Black);

                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(!isReceipt ? line.IssueQty.ToString() : "—").FontSize(8).FontColor(Colors.Black);
                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(!isReceipt ? line.IssueUnitCost.ToString("N4") : "—").FontSize(8).FontColor(Colors.Black);
                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(!isReceipt ? line.IssueTotalCost.ToString("N2") : "—").FontSize(8).FontColor(Colors.Black);

                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(line.BalanceQty.ToString()).FontSize(8).FontColor(Colors.Black);
                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(line.BalanceUnitCost.ToString("N4")).FontSize(8).FontColor(Colors.Black);
                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(line.BalanceTotalCost.ToString("N2")).FontSize(8).FontColor(Colors.Black);
            }
        });
    }
}
