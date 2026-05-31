using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintPhysicalCount;

internal sealed class PhysicalCountPdfDocument(
    List<PhysicalCountItemDto>  items,
    OrganizationProfileDto?     org,
    List<ReportSignatoryDto>    signatories,
    DateTime?                   asOfDate) : IDocument
{
    public DocumentMetadata GetMetadata() => new()
    {
        Title  = "Report on the Physical Count of Inventories",
        Author = org?.Name ?? string.Empty
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));
            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeBody);
            page.Footer().Element(ComposeFooter);
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
            col.Item().PaddingTop(6).AlignCenter()
                .Text("REPORT ON THE PHYSICAL COUNT OF INVENTORIES").Bold().FontSize(11);
            col.Item().AlignCenter().Text("(Type of Inventory Item: Office Supplies)").FontSize(9);
            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(); c.RelativeColumn();
                    c.RelativeColumn(); c.RelativeColumn();
                });
                table.Cell().Text("Entity Name:").Bold();
                table.Cell().Text(org?.Name ?? string.Empty);
                table.Cell().Text("As of:").Bold();
                table.Cell().Text((asOfDate ?? DateTime.Today).ToString("MMMM d, yyyy"));
                table.Cell(); table.Cell();
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
                c.ConstantColumn(28); c.RelativeColumn(5); c.RelativeColumn(2); c.RelativeColumn(2);
                c.RelativeColumn(2); c.ConstantColumn(60); c.ConstantColumn(60);
                c.ConstantColumn(50); c.RelativeColumn(2); c.RelativeColumn(2);
            });

            var style = TextStyle.Default.Bold().FontSize(8);
            table.Header(h =>
            {
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Article").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Description").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Stock No.").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Unit of Measure").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Unit Value").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Balance Per Card (Qty)").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("On Hand Per Count (Qty)").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Shortage Qty").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Shortage Value").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Remarks").Style(style);
            });

            foreach (var item in items)
            {
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(item.ArticleNumber.ToString()).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).Text(item.Description).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(item.StockNo).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(item.UnitOfMeasure).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(item.UnitValue.ToString("N2")).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(item.BalancePerCard.ToString()).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(item.OnHandPerCount.ToString()).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(item.ShortageQuantity.ToString()).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(item.ShortageValue.ToString("N2")).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).Text(item.Remarks ?? string.Empty).FontSize(8);
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            var activeSignatories = signatories
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .ToList();

            if (activeSignatories.Count > 0)
            {
                col.Item().PaddingTop(8).Table(table =>
                {
                    var rows = activeSignatories.Chunk(3).ToList();
                    table.ColumnsDefinition(c =>
                    {
                        for (var i = 0; i < Math.Min(activeSignatories.Count, 3); i++)
                            c.RelativeColumn();
                    });
                    foreach (var row in rows)
                    {
                        foreach (var sig in row)
                        {
                            table.Cell().Padding(4).Column(inner =>
                            {
                                inner.Item().Text(sig.Label).Bold().FontSize(7).AlignCenter();
                                inner.Item().PaddingTop(14).Text(sig.Name).Bold().FontSize(8).AlignCenter();
                                inner.Item().Text(sig.Title).FontSize(7).AlignCenter();
                            });
                        }
                        for (var i = row.Length; i < 3; i++) table.Cell();
                    }
                });
            }
            col.Item().AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(7);
                x.CurrentPageNumber().FontSize(7);
                x.Span(" of ").FontSize(7);
                x.TotalPages().FontSize(7);
            });
        });
    }
}
