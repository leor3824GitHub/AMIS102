using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintPhysicalCount;

internal sealed class PhysicalCountPdfDocument(
    List<PhysicalCountItemDto>  items,
    OrganizationProfileDto?     org,
    List<ReportSignatoryDto>    signatories,
    DateTime?                   asOfDate,
    DateTime?                   assumedAccountabilityDate = null,
    string                      paperSize   = "a4",
    string                      orientation = "landscape",
    float                       marginMm    = 15f) : IDocument
{
    // Signatory entry that supplies the accountable officer for the accountability
    // sentence. Matched by Label and excluded from the footer signature grid.
    private const string AccountableOfficerLabel = "Accountable Officer";

    private ReportSignatoryDto? AccountableOfficer => signatories
        .FirstOrDefault(s => string.Equals(s.Label.Trim(), AccountableOfficerLabel, StringComparison.OrdinalIgnoreCase));

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
                    c.ConstantColumn(72);  // "Entity Name:" label
                    c.RelativeColumn(3);   // entity value
                    c.ConstantColumn(42);  // "As of:" label
                    c.RelativeColumn(2);   // as-of date
                });
                table.Cell().Text("Entity Name:").Bold();
                table.Cell().Text(org?.Name ?? string.Empty);
                table.Cell().Text("As of:").Bold();
                table.Cell().Text((asOfDate ?? DateTime.Today).ToString("MMMM d, yyyy"));
            });

            ComposeAccountabilityStatement(col);

            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    }

    private void ComposeAccountabilityStatement(ColumnDescriptor col)
    {
        var officer = AccountableOfficer;
        if (officer is null || string.IsNullOrWhiteSpace(officer.Name))
            return;

        col.Item().PaddingTop(4).Text(text =>
        {
            text.DefaultTextStyle(x => x.FontSize(9));
            text.Span("For which ");
            text.Span(officer.Name.ToUpperInvariant()).Bold().Underline();
            if (!string.IsNullOrWhiteSpace(officer.Title))
            {
                text.Span(", ");
                text.Span(officer.Title).Bold().Underline();
            }
            text.Span(", of ");
            text.Span(org?.Name ?? string.Empty).Bold().Underline();
            text.Span(" is accountable");
            if (assumedAccountabilityDate is { } assumed)
            {
                text.Span(", having assumed such accountability on ");
                text.Span(assumed.ToString("MMMM dd, yyyy")).Bold().Underline();
            }
            text.Span(".");
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
        var accountableOfficer = AccountableOfficer;
        var active = signatories
            .Where(s => s.IsActive && s != accountableOfficer)
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
