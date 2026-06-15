using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintIncident;

/// <summary>Report of Lost, Stolen, Damaged or Destroyed Property (RLSDDSP).</summary>
internal sealed class IncidentPdfDocument(
    IncidentReportDocumentDto report,
    OrganizationProfileDto?   org,
    string                    paperSize   = "a4",
    string                    orientation = "portrait",
    float                     marginMm    = 14f) : IDocument
{
    public DocumentMetadata GetMetadata() => new()
    {
        Title  = $"RLSDDSP — {report.IncidentNo}",
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
            col.Item().PaddingTop(6).AlignCenter().Text("REPORT OF LOST, STOLEN, DAMAGED OR DESTROYED PROPERTY").Bold().FontSize(11);

            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Report No.: ").SemiBold().FontSize(8);
                    t.Span(report.IncidentNo).FontSize(8);
                });
                row.RelativeItem().AlignCenter().Text(t =>
                {
                    t.Span("Type: ").SemiBold().FontSize(8);
                    t.Span(report.IncidentType.ToString()).FontSize(8);
                });
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("Date: ").SemiBold().FontSize(8);
                    t.Span(report.IncidentDate.ToString("MMMM d, yyyy")).FontSize(8);
                });
            });
            col.Item().Text(t =>
            {
                t.Span("Office: ").SemiBold().FontSize(8);
                t.Span(report.DepartmentOffice).FontSize(8);
                t.Span("    Fund Cluster: ").SemiBold().FontSize(8);
                t.Span(report.FundCluster).FontSize(8);
            });
            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    }

    private void ComposeBody(IContainer container)
    {
        var style = TextStyle.Default.Bold().FontSize(8);

        container.PaddingTop(4).Column(col =>
        {
            col.Item().PaddingBottom(6).Text(t =>
            {
                t.Span("Circumstances: ").SemiBold().FontSize(9);
                t.Span(report.Circumstances).FontSize(9);
            });

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(24);    // No.
                    c.RelativeColumn(3);      // Property No.
                    c.RelativeColumn(4);      // Description
                    c.ConstantColumn(80);     // Acq Cost
                    c.ConstantColumn(90);     // Replacement Cost
                    c.RelativeColumn(2);      // Resolution
                });

                table.Header(h =>
                {
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("No.").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Property No.").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Description").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Acq. Cost (₱)").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Repl. Cost (₱)").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Resolution").Style(style);
                });

                var no = 1;
                foreach (var i in report.Items)
                {
                    table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(no.ToString()).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).Text(i.PropertyNo).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).Text(i.Description).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).AlignRight().Text(i.AcquisitionCost.ToString("N2")).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).AlignRight().Text(i.CurrentReplacementCost.ToString("N2")).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(i.Resolution.ToString()).FontSize(8);
                    no++;
                }

                if (report.Items.Count == 0)
                    table.Cell().ColumnSpan(6).Border(0.5f).Padding(6).AlignCenter()
                        .Text("No items on this report.").Italic().FontSize(8);
            });

            col.Item().PaddingTop(6).AlignRight().Text(t =>
            {
                t.Span("Total replacement cost: ").SemiBold().FontSize(9);
                t.Span($"₱{report.TotalCurrentReplacementCost:N2}").Bold().FontSize(9);
            });
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
                    cell.Item().Text("Accountable Officer:").FontSize(8);
                    cell.Item().PaddingTop(18).Text(report.AccountableOfficerName).Bold().FontSize(9);
                    cell.Item().Text(report.AccountableOfficerDesignation).FontSize(7);
                });
                row.RelativeItem().AlignRight().Column(cell =>
                {
                    cell.Item().Text("Approved by:").FontSize(8);
                    cell.Item().PaddingTop(18).AlignRight().Text(org?.ApprovingOfficialName ?? "________________________").Bold().FontSize(8);
                    cell.Item().AlignRight().Text(org?.ApprovingOfficialDesignation ?? "Head of Agency").FontSize(7);
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
