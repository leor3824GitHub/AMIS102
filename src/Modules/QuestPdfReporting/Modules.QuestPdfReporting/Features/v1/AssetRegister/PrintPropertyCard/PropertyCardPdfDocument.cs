using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPropertyCard;

/// <summary>
/// Property Card (PPE) / Semi-Expendable Property Card (SE) — chronological movements for one
/// asset (COA §6.3.1.a). Same underlying movement projection for both; only the COA form title
/// differs, since PC and SPC are distinct prescribed forms despite sharing this ledger shape.
/// </summary>
internal sealed class PropertyCardPdfDocument(
    PropertyCardDto          card,
    OrganizationProfileDto?  org,
    string                   paperSize   = "a4",
    string                   orientation = "landscape",
    float                    marginMm    = 12f) : IDocument
{
    private string FormTitle => card.AssetType == AssetType.SE ? "Semi-Expendable Property Card" : "Property Card";

    public DocumentMetadata GetMetadata() => new()
    {
        Title  = $"{FormTitle} — {card.PropertyNo}",
        Author = org?.Name ?? string.Empty
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            QuestPdfPaperSize.Apply(page, paperSize, orientation, marginMm);
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
            col.Item().AlignCenter().Text("NATIONAL FOOD AUTHORITY").Bold().FontSize(10);
            if (org is not null)
            {
                col.Item().AlignCenter().Text(org.Name).Bold().FontSize(11);
                if (!string.IsNullOrWhiteSpace(org.Address))
                    col.Item().AlignCenter().Text(org.Address).FontSize(8);
            }
            col.Item().PaddingTop(6).AlignCenter().Text(FormTitle.ToUpperInvariant()).Bold().FontSize(12);

            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Property No.: ").SemiBold().FontSize(8);
                    t.Span(card.PropertyNo).FontSize(8);
                });
                row.RelativeItem().AlignCenter().Text(t =>
                {
                    t.Span("Acquired: ").SemiBold().FontSize(8);
                    t.Span($"{card.AcquisitionDate:yyyy-MM-dd} @ ₱{card.AcquisitionCost:N2}").FontSize(8);
                });
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("State: ").SemiBold().FontSize(8);
                    t.Span(card.CurrentState.ToString()).FontSize(8);
                });
            });
            col.Item().Text(t =>
            {
                t.Span("Description: ").SemiBold().FontSize(8);
                t.Span($"{card.Description} ({card.Unit})").FontSize(8);
            });
            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    }

    private void ComposeBody(IContainer container)
    {
        var style = TextStyle.Default.Bold().FontSize(8);

        container.PaddingTop(4).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(60);     // Date
                c.RelativeColumn(2);       // Movement
                c.RelativeColumn(2);       // Source
                c.RelativeColumn(2);       // Document No.
                c.RelativeColumn(3);       // Party
                c.ConstantColumn(80);      // Amount
                c.RelativeColumn(3);       // Remarks
            });

            table.Header(h =>
            {
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Date").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Movement").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Source").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Document No.").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Party").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Amount (₱)").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Remarks").Style(style);
            });

            foreach (var m in card.Movements)
            {
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(m.Date.ToString("yyyy-MM-dd")).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).Text(m.MovementType.ToString()).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).Text(m.Source.ToString()).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).Text(m.DocumentNo ?? "—").FontSize(8);
                table.Cell().Border(0.5f).Padding(2).Text(m.Party ?? string.Empty).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(m.Amount?.ToString("N2") ?? string.Empty).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).Text(m.Remarks ?? string.Empty).FontSize(8);
            }

            if (card.Movements.Count == 0)
                table.Cell().ColumnSpan(7).Border(0.5f).Padding(6).AlignCenter()
                    .Text("No movements recorded for this asset.").Italic().FontSize(8);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignRight().Text(x =>
        {
            x.Span("Page ").FontSize(7);
            x.CurrentPageNumber().FontSize(7);
            x.Span(" of ").FontSize(7);
            x.TotalPages().FontSize(7);
        });
    }
}
