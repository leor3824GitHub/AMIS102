using System.Globalization;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.ProcurementPlanning.PrintApp;

/// <summary>
/// Annual Procurement Plan — GPPB APP form (RA 12009). Agency header with the Indicative / Final / Updated
/// phase boxes, a 12-column table grouped into General Requirements, Miscellaneous (Direct Acquisition,
/// Sec 32.2) and CSE-from-PS-DBM sections, the EPA / CSE / grand totals, and three signature blocks.
/// </summary>
internal sealed class AppPdfDocument(
    AnnualProcurementPlanDto app,
    OrganizationProfileDto? org,
    IReadOnlyList<ReportSignatoryDto> signatories,
    string paperSize = QuestPdfPaperSize.LongBond,
    string orientation = QuestPdfPaperSize.Landscape,
    float marginMm = 8f) : IDocument
{
    private static readonly CultureInfo Nf = CultureInfo.InvariantCulture;
    private const float BodyFont = 7f;
    private const string HeaderFill = "#F2F2F2";

    private static readonly AppSection[] Sections =
        [AppSection.GeneralRequirements, AppSection.DirectAcquisition, AppSection.CommonUseSupplies];

    // Second header tier — Columns 1-10 (11 and 12 span both tiers).
    private static readonly string[] ColumnTitles =
    [
        "Project Title", "End-User or Implementing Unit", "General Description of the Project",
        "Mode of Procurement", "To be covered by an Early Procurement", "Criteria for Bid Evaluation",
        "Start of Procurement", "End of Procurement", "Source of Fund", "Estimated Budget / Approved"
    ];

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"APP — FY {app.FiscalYear}",
        Author = org?.Name ?? string.Empty
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            QuestPdfPaperSize.Apply(page, paperSize, orientation, marginMm);
            page.DefaultTextStyle(x => x.FontSize(BodyFont).FontFamily("Arial"));

            page.Header().Element(ComposeHeader);
            page.Content().Column(col =>
            {
                col.Item().Element(ComposeTable);
                col.Item().Element(ComposeTotals);
                col.Item().Element(ComposeSignatures);
            });
            page.Footer().AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(6);
                x.CurrentPageNumber().FontSize(6);
                x.Span(" of ").FontSize(6);
                x.TotalPages().FontSize(6);
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.PaddingBottom(4).Row(row =>
        {
            row.RelativeItem();
            var logo = ReportLogo.Bytes;
            if (logo is not null)
                row.ConstantItem(48).Height(48).AlignMiddle().Image(logo).FitArea();

            row.AutoItem().PaddingHorizontal(10).AlignMiddle().Column(col =>
            {
                col.Item().AlignCenter().Text((org?.Name ?? string.Empty).ToUpperInvariant()).Bold().FontSize(12);
                col.Item().AlignCenter().Text($"ANNUAL PROCUREMENT PLAN FOR FY {app.FiscalYear}").Bold().FontSize(11);
                col.Item().PaddingTop(3).AlignCenter().Row(r =>
                {
                    r.Spacing(14);
                    r.AutoItem().Element(c => PhaseBox(c, "INDICATIVE", app.Phase == AppPhase.Indicative));
                    r.AutoItem().Element(c => PhaseBox(c, "FINAL", app.Phase == AppPhase.Final));
                    var version = app.Phase == AppPhase.Updated ? app.VersionNumber.ToString(Nf) : "_____";
                    r.AutoItem().Element(c => PhaseBox(c, $"UPDATED [Version No. {version}]", app.Phase == AppPhase.Updated));
                });
            });
            row.RelativeItem();
        });
    }

    private static void PhaseBox(IContainer container, string label, bool ticked) =>
        container.Row(r =>
        {
            r.ConstantItem(14).Height(11).Border(1).AlignCenter().AlignMiddle()
                .Text(ticked ? "X" : string.Empty).Bold().FontSize(8);
            r.AutoItem().PaddingLeft(3).AlignMiddle().Text(label).Bold().FontSize(9);
        });

    private void ComposeTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2.4f); // 1 Project Title
                c.RelativeColumn(1.1f); // 2 End-User
                c.RelativeColumn(2.4f); // 3 General Description
                c.RelativeColumn(1.3f); // 4 Mode
                c.RelativeColumn(0.9f); // 5 Early Procurement
                c.RelativeColumn(0.9f); // 6 Bid Evaluation
                c.RelativeColumn(0.9f); // 7 Start
                c.RelativeColumn(0.9f); // 8 End
                c.RelativeColumn(1.2f); // 9 Source of Fund
                c.RelativeColumn(1.2f); // 10 Estimated Budget
                c.RelativeColumn(1.1f); // 11 Strategy
                c.RelativeColumn(1.5f); // 12 Remarks
            });

            table.Header(h =>
            {
                h.Cell().ColumnSpan(6).Element(HeadCell).Text("PROCUREMENT PROJECT DETAILS").Bold();
                h.Cell().ColumnSpan(2).Element(HeadCell).Text("PROJECTED TIMELINE (MM/YYYY)").Bold();
                h.Cell().ColumnSpan(2).Element(HeadCell).Text("FUNDING DETAILS").Bold();
                h.Cell().RowSpan(2).Element(HeadCell).Text("PROCUREMENT STRATEGY OR TOOLS").Bold();
                h.Cell().RowSpan(2).Element(HeadCell).Text("REMARKS (Other relevant descriptions of the procurement project, if any)").Bold();

                foreach (var title in ColumnTitles)
                {
                    h.Cell().Element(HeadCell).Text(title).Bold();
                }

                for (var i = 1; i <= 12; i++)
                    h.Cell().Element(HeadCell).Text($"Column {i}").Bold().FontSize(6);
            });

            foreach (var section in Sections)
            {
                var rows = app.Items.Where(i => i.Section == section).OrderBy(i => i.ItemNo).ToList();
                if (rows.Count == 0)
                    continue;

                table.Cell().ColumnSpan(12).Border(0.5f).Padding(2).Text(section.ToFormTitle()).Bold().FontSize(BodyFont + 0.5f);

                foreach (var item in rows)
                {
                    BodyCell(table, string.IsNullOrWhiteSpace(item.ProjectTitle) ? item.GeneralDescription : item.ProjectTitle);
                    BodyCell(table, item.EndUserUnit, center: true);
                    BodyCell(table, item.GeneralDescription);
                    BodyCell(table, item.ModeOfProcurement, center: true);
                    BodyCell(table, item.IsEarlyProcurement ? "Yes" : "No", center: true);
                    BodyCell(table, item.BidEvaluationCriteria.ToDisplay(), center: true);
                    BodyCell(table, item.ProcurementStart, center: true);
                    BodyCell(table, item.ProcurementEnd, center: true);
                    BodyCell(table, item.SourceOfFunds, center: true);
                    table.Cell().Border(0.5f).Padding(2).AlignRight().AlignMiddle()
                        .Text(item.EstimatedBudget.ToString("N2", Nf));
                    BodyCell(table, string.IsNullOrWhiteSpace(item.ProcurementStrategy) ? "-" : item.ProcurementStrategy!, center: true);
                    BodyCell(table, item.Remarks ?? string.Empty);
                }
            }
        });
    }

    private static IContainer HeadCell(IContainer c) =>
        c.Border(0.5f).Background(HeaderFill).Padding(2).AlignCenter().AlignMiddle().DefaultTextStyle(x => x.FontSize(BodyFont));

    private static void BodyCell(TableDescriptor table, string text, bool center = false)
    {
        var cell = table.Cell().Border(0.5f).Padding(2).AlignMiddle();
        if (center)
            cell = cell.AlignCenter();
        cell.Text(text);
    }

    private void ComposeTotals(IContainer container)
    {
        var epaTotal = app.Items.Where(i => i.IsEarlyProcurement).Sum(i => i.EstimatedBudget);
        var cseTotal = app.Items.Where(i => i.Section == AppSection.CommonUseSupplies).Sum(i => i.EstimatedBudget);
        var grandTotal = app.Items.Sum(i => i.EstimatedBudget);

        container.PaddingTop(4).Row(row =>
        {
            row.RelativeItem().Text("Note: Insert additional rows as necessary").FontSize(6).Italic();
            row.ConstantItem(360).Column(col =>
            {
                TotalLine(col, "Total Amount of Estimated Budget for EPA Projects:", epaTotal, bold: false);
                TotalLine(col, "Total Amount of CSEs to be purchased from PS-DBM:", cseTotal, bold: false);
                TotalLine(col, "Total Amount of Estimated Budget:", grandTotal, bold: true);
            });
        });
    }

    private static void TotalLine(ColumnDescriptor col, string label, decimal amount, bool bold) =>
        col.Item().Row(r =>
        {
            r.RelativeItem().AlignRight().PaddingRight(6).Text(label).Bold().FontSize(8);
            var v = r.ConstantItem(90).AlignRight().Text(amount.ToString("N2", Nf)).FontSize(8);
            if (bold)
                v.Bold();
        });

    private void ComposeSignatures(IContainer container)
    {
        if (signatories.Count == 0)
            return;

        container.PaddingTop(16).ShowEntire().Row(row =>
        {
            row.Spacing(30);
            foreach (var s in signatories)
            {
                row.RelativeItem().Column(c =>
                {
                    foreach (var line in s.Label.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                        c.Item().Text(line).FontSize(7);
                    c.Item().PaddingTop(18).BorderBottom(0.75f).AlignCenter()
                        .Text(s.Name.ToUpperInvariant()).Bold().FontSize(8);
                    c.Item().AlignCenter().Text("Signature over Printed Name").FontSize(7);
                    c.Item().AlignCenter().Text("Position/Designation").FontSize(7);
                    c.Item().AlignCenter().Text(s.Title).Italic().Underline().FontSize(7);
                    c.Item().PaddingTop(8).Text("Date : ______________________").FontSize(7);
                });
            }
        });
    }
}
