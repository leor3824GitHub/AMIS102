using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintPhysicalCount;

internal sealed class PhysicalCountPdfDocument(
    List<PhysicalCountGroupDto> groups,
    OrganizationProfileDto?     org,
    List<ReportSignatoryDto>    signatories,
    DateTime?                   asOfDate,
    DateTime?                   assumedAccountabilityDate = null,
    string                      paperSize   = "a4",
    string                      orientation = "landscape",
    float                       marginMm    = 15f) : IDocument
{
    // The accountability sentence names the first PhysicalCount signatory (order 1).
    // This person still appears in the footer committee grid as well.
    private ReportSignatoryDto? AccountabilityOfficer => signatories
        .Where(s => s.IsActive)
        .OrderBy(s => s.SortOrder)
        .FirstOrDefault();

    public DocumentMetadata GetMetadata() => new()
    {
        Title  = "Report on the Physical Count of Inventories",
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
            col.Item().PaddingTop(6).AlignCenter()
                .Text("REPORT ON THE PHYSICAL COUNT OF INVENTORIES").Bold().FontSize(11);
            col.Item().AlignCenter().Text("(Type of Inventory Item: Office Supplies)").FontSize(9);
            col.Item().AlignCenter().Text($"As of {(asOfDate ?? DateTime.Today):MMMM d, yyyy}").FontSize(9);

            ComposeAccountabilityStatement(col);

            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    }

    private void ComposeAccountabilityStatement(ColumnDescriptor col)
    {
        var officer = AccountabilityOfficer;
        var officerName  = string.IsNullOrWhiteSpace(officer?.Name)  ? "________________________" : officer!.Name.ToUpperInvariant();
        var officerTitle = string.IsNullOrWhiteSpace(officer?.Title) ? "________________________" : officer!.Title;
        var officeName   = string.IsNullOrWhiteSpace(org?.Name)      ? "________________________" : org!.Name;
        var assumedDate  = assumedAccountabilityDate?.ToString("MMMM dd, yyyy") ?? "________________";

        col.Item().PaddingTop(10).PaddingBottom(6).Text(text =>
        {
            text.DefaultTextStyle(x => x.FontSize(9).LineHeight(1.3f));
            text.Span("For which ");
            text.Span(officerName).Bold().Underline();
            text.Span(", ");
            text.Span(officerTitle).Bold().Underline();
            text.Span(", of ");
            text.Span(officeName).Bold().Underline();
            text.Span(" is accountable, having assumed such accountability on ");
            text.Span(assumedDate).Bold().Underline();
            text.Span(".");
        });
    }

    private void ComposeBody(IContainer container)
    {
        const int ColumnCount = 9;

        container.PaddingTop(4).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(5); c.RelativeColumn(2); c.RelativeColumn(2);
                c.RelativeColumn(2); c.ConstantColumn(60); c.ConstantColumn(60);
                c.ConstantColumn(50); c.RelativeColumn(2); c.RelativeColumn(2);
            });

            var style = TextStyle.Default.Bold().FontSize(8);
            table.Header(h =>
            {
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

            foreach (var group in groups)
            {
                // Article group header band spanning the full table width.
                table.Cell().ColumnSpan(ColumnCount).Border(0.5f).Background(Colors.Grey.Lighten3)
                    .Padding(2).Text(group.Article).Style(style);

                foreach (var item in group.Items)
                {
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
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        var active = signatories
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ToList();

        var approved = active.FirstOrDefault(s => s.Label.Contains("APPROVED", StringComparison.OrdinalIgnoreCase));
        var verified = active.FirstOrDefault(s => s.Label.Contains("VERIFIED", StringComparison.OrdinalIgnoreCase));
        var members = active.Where(s => s != approved && s != verified).ToList();

        container.Column(col =>
        {
            col.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2);   // Certified Correct (inventory committee)
                    c.RelativeColumn(1);   // Approved by
                    c.RelativeColumn(1);   // Verified by
                });

                table.Cell().Padding(2).Text("Certified Correct by:").FontSize(8);
                table.Cell().Padding(2).AlignCenter().Text("Approved by:").FontSize(8);
                table.Cell().Padding(2).AlignCenter().Text("Verified by:").FontSize(8);

                table.Cell().Element(c => ComposeCommittee(c, members));
                table.Cell().Element(c => ComposeSignatory(c, approved));
                table.Cell().Element(c => ComposeSignatory(c, verified));
            });

            col.Item().PaddingTop(4).AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(7);
                x.CurrentPageNumber().FontSize(7);
                x.Span(" of ").FontSize(7);
                x.TotalPages().FontSize(7);
            });
        });
    }

    private static void ComposeCommittee(IContainer container, List<ReportSignatoryDto> members)
    {
        container.Column(outer =>
        {
            outer.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                foreach (var m in members)
                {
                    table.Cell().Padding(2).PaddingTop(16).PaddingRight(8).Column(cell =>
                    {
                        cell.Item().Text(m.Name).Bold().FontSize(8);
                        cell.Item().Text(FormatMemberTitle(m)).FontSize(7);
                    });
                }
                if (members.Count % 2 != 0) table.Cell();
            });

            outer.Item().PaddingTop(4).Text(
                "(Signature over Printed Name of Inventory Committee Chair and Members)")
                .Italic().FontSize(7);
        });
    }

    private static void ComposeSignatory(IContainer container, ReportSignatoryDto? sig)
    {
        if (sig is null)
        {
            container.Text(string.Empty);
            return;
        }

        container.PaddingTop(16).Column(cell =>
        {
            cell.Item().AlignCenter().Text(sig.Name).Bold().FontSize(8);
            cell.Item().AlignCenter().Text(sig.Title).FontSize(7);
        });
    }

    private static string FormatMemberTitle(ReportSignatoryDto sig)
    {
        var role = sig.Label.Contains("CHAIR", StringComparison.OrdinalIgnoreCase)
            ? "Chairperson"
            : "Member";
        return string.IsNullOrWhiteSpace(sig.Title) ? $"/ {role}" : $"{sig.Title} / {role}";
    }
}
