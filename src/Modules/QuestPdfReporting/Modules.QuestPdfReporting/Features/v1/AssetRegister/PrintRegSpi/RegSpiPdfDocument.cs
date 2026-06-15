using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintRegSpi;

/// <summary>Registry of Semi-Expendable Property Issued (RegSPI), grouped by accountable custodian.</summary>
internal sealed class RegSpiPdfDocument(
    RegSpiReportDto          report,
    OrganizationProfileDto?  org,
    string                   paperSize   = "a4",
    string                   orientation = "landscape",
    float                    marginMm    = 12f) : IDocument
{
    public DocumentMetadata GetMetadata() => new()
    {
        Title  = "RegSPI",
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
            col.Item().PaddingTop(6).AlignCenter().Text("REGISTRY OF SEMI-EXPENDABLE PROPERTY ISSUED").Bold().FontSize(11);
            col.Item().AlignCenter().Text($"As of {report.AsOfDate:MMMM d, yyyy}").FontSize(8);
            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    }

    private void ComposeBody(IContainer container)
    {
        var style = TextStyle.Default.Bold().FontSize(8);

        container.PaddingTop(4).Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(24);   // No.
                    c.RelativeColumn(3);     // Custodian
                    c.RelativeColumn(2);     // ICS No.
                    c.ConstantColumn(60);    // Date
                    c.RelativeColumn(3);     // Property No.
                    c.RelativeColumn(4);     // Description
                    c.RelativeColumn(1);     // Unit
                    c.ConstantColumn(40);    // Qty
                    c.ConstantColumn(70);    // Unit Cost
                    c.ConstantColumn(75);    // Amount
                });

                table.Header(h =>
                {
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("No.").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Accountable Officer").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("ICS/PAR No.").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Date").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Property No.").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Description").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Unit").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Qty").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Unit Cost (₱)").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Amount (₱)").Style(style);
                });

                var no = 1;
                foreach (var r in report.Rows.OrderBy(r => r.CustodianName).ThenBy(r => r.DocumentNo))
                {
                    table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(no.ToString()).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).Text(r.CustodianName).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).Text(r.DocumentNo).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.IssuedOn.ToString("yyyy-MM-dd")).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).Text(r.PropertyNo).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).Text(r.Description).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.Unit).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.Quantity.ToString()).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).AlignRight().Text(r.UnitCost.ToString("N2")).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).AlignRight().Text(r.Amount.ToString("N2")).FontSize(8);
                    no++;
                }

                if (report.Rows.Count == 0)
                    table.Cell().ColumnSpan(10).Border(0.5f).Padding(6).AlignCenter()
                        .Text("No semi-expendable property issued as of this date.").Italic().FontSize(8);
            });

            col.Item().PaddingTop(6).AlignRight().Text(t =>
            {
                t.Span($"Total items: {report.TotalItems}    ").SemiBold().FontSize(9);
                t.Span("Total amount: ").SemiBold().FontSize(9);
                t.Span($"₱{report.TotalAmount:N2}").Bold().FontSize(9);
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(14).Row(row =>
            {
                row.RelativeItem().Column(cell =>
                {
                    cell.Item().Text("Prepared by:").FontSize(8);
                    cell.Item().PaddingTop(20).Text("________________________").FontSize(8);
                    cell.Item().Text("Supply / Property Officer").FontSize(7);
                });
                row.RelativeItem().AlignRight().Column(cell =>
                {
                    cell.Item().Text("Certified Correct:").FontSize(8);
                    cell.Item().PaddingTop(20).AlignRight().Text(org?.AccountantName ?? "________________________").Bold().FontSize(8);
                    cell.Item().AlignRight().Text(org?.AccountantDesignation ?? "Accountant").FontSize(7);
                });
            });

            col.Item().PaddingTop(6).AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(7);
                x.CurrentPageNumber().FontSize(7);
                x.Span(" of ").FontSize(7);
                x.TotalPages().FontSize(7);
            });
        });
    }
}
