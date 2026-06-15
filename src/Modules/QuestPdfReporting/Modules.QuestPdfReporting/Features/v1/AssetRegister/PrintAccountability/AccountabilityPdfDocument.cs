using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintAccountability;

/// <summary>Inventory Custodian Slip (SE) or Property Acknowledgement Receipt (PPE).</summary>
internal sealed class AccountabilityPdfDocument(
    AccountabilityReportDto  report,
    OrganizationProfileDto?  org,
    string                   paperSize   = "a4",
    string                   orientation = "portrait",
    float                    marginMm    = 15f) : IDocument
{
    private bool IsIcs => report.AccountabilityType == AccountabilityType.SE_ICS;
    private string Title => IsIcs ? "INVENTORY CUSTODIAN SLIP" : "PROPERTY ACKNOWLEDGEMENT RECEIPT";
    private string Acronym => IsIcs ? "ICS" : "PAR";

    public DocumentMetadata GetMetadata() => new()
    {
        Title  = $"{Acronym} — {report.DocumentNo}",
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
            col.Item().PaddingTop(6).AlignCenter().Text(Title).Bold().FontSize(12);

            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Fund Cluster: ").SemiBold().FontSize(8);
                    t.Span(report.FundCluster).FontSize(8);
                });
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span($"{Acronym} No.: ").SemiBold().FontSize(8);
                    t.Span(report.DocumentNo).FontSize(8);
                });
            });
            col.Item().Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Date: ").SemiBold().FontSize(8);
                    t.Span(report.IssuedOn.ToString("MMMM d, yyyy")).FontSize(8);
                });
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("Status: ").SemiBold().FontSize(8);
                    t.Span(report.Status.ToString()).FontSize(8);
                });
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
                c.ConstantColumn(40);    // Qty
                c.RelativeColumn(1);      // Unit
                c.ConstantColumn(75);     // Unit Cost
                c.ConstantColumn(80);     // Amount
                c.RelativeColumn(5);      // Description
                c.RelativeColumn(3);      // Property No.
            });

            table.Header(h =>
            {
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Qty").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Unit").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Unit Cost (₱)").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Amount (₱)").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Description").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Property No.").Style(style);
            });

            foreach (var l in report.Lines)
            {
                var amount = l.UnitCost * l.IssuedQty;
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(l.IssuedQty.ToString()).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(l.Unit).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(l.UnitCost.ToString("N2")).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(amount.ToString("N2")).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).Text(l.Description).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).Text(l.PropertyNo).FontSize(8);
            }

            if (report.Lines.Count == 0)
                table.Cell().ColumnSpan(6).Border(0.5f).Padding(6).AlignCenter()
                    .Text("No items on this document.").Italic().FontSize(8);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(16).Row(row =>
            {
                row.RelativeItem().Column(cell =>
                {
                    cell.Item().Text("Received by:").FontSize(8);
                    cell.Item().PaddingTop(18).Text(report.ReceivedByName).Bold().FontSize(9);
                    cell.Item().Text(report.ReceivedByDesignation ?? "Accountable Officer").FontSize(7);
                });
                row.RelativeItem().AlignRight().Column(cell =>
                {
                    cell.Item().Text("Issued by:").FontSize(8);
                    cell.Item().PaddingTop(18).AlignRight().Text(report.IssuedByName).Bold().FontSize(9);
                    cell.Item().AlignRight().Text(report.IssuedByDesignation ?? "Supply / Property Officer").FontSize(7);
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
