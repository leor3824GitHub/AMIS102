using System.Globalization;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.ProcurementPlanning.PrintPpmp;

/// <summary>
/// Project Procurement Management Plan — GPPB PPMP form. Letterhead with logo, "PPMP NO.", Indicative / Final
/// boxes (an Updated version ticks Final and shows its version), Fiscal Year + End-User lines, the 12-column
/// project table, TOTAL BUDGET, and the Prepared by (End-User) / Submitted by (Head of End-User) blocks.
/// </summary>
internal sealed class PpmpPdfDocument(
    PpmpDto ppmp,
    OrganizationProfileDto? org,
    EmployeeReferenceDto? preparer,
    string paperSize = QuestPdfPaperSize.LongBond,
    string orientation = QuestPdfPaperSize.Landscape,
    float marginMm = 8f) : IDocument
{
    private static readonly CultureInfo Nf = CultureInfo.InvariantCulture;
    private const float BodyFont = 7.5f;
    private const string HeaderFill = "#F2F2F2";

    // Second header tier — Columns 1-10 (11 and 12 span both tiers).
    private static readonly string[] ColumnTitles =
    [
        "General Description and Objective of the Project to be Procured",
        "Type of the Project to be Procured (whether Goods, Infrastructure and Consulting Services)",
        "Quantity and Size of the Project to be Procured",
        "Recommended Mode of Procurement",
        "Pre-Procurement Conference, if applicable (Yes/No)",
        "Start of Procurement Activity",
        "End of Procurement Activity",
        "Expected Delivery/ Implementation Period",
        "Source of Funds",
        "Estimated Budget / Authorized Budgetary Allocation (PhP)"
    ];

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"PPMP — {ppmp.PpmpNumber}",
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
        container.PaddingBottom(4).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem();
                var logo = ReportLogo.Bytes;
                if (logo is not null)
                    row.ConstantItem(44).Height(44).AlignMiddle().Image(logo).FitArea();

                row.AutoItem().PaddingHorizontal(10).AlignMiddle().Column(c =>
                {
                    c.Item().AlignCenter().Text((org?.Name ?? string.Empty).ToUpperInvariant()).Bold().FontSize(12);
                    if (!string.IsNullOrWhiteSpace(org?.Address))
                        c.Item().AlignCenter().Text(org!.Address).FontSize(8);
                    c.Item().PaddingTop(2).AlignCenter()
                        .Text($"PROJECT PROCUREMENT MANAGEMENT PLAN (PPMP) NO. {ppmp.PpmpNumber}").Bold().FontSize(11);
                    c.Item().PaddingTop(3).AlignCenter().Row(r =>
                    {
                        r.Spacing(24);
                        r.AutoItem().Element(x => PhaseBox(x, "INDICATIVE", ppmp.Phase == PpmpPhase.Indicative));
                        var final = ppmp.Phase == PpmpPhase.Updated
                            ? $"FINAL (Updated — Version No. {ppmp.VersionNumber.ToString(Nf)})"
                            : "FINAL";
                        r.AutoItem().Element(x => PhaseBox(x, final, ppmp.Phase is PpmpPhase.Final or PpmpPhase.Updated));
                    });
                });
                row.RelativeItem();
            });

            col.Item().PaddingTop(8).Text(t =>
            {
                t.Span("Fiscal Year : ").Bold().FontSize(8);
                t.Span(ppmp.FiscalYear.ToString(Nf)).FontSize(8);
            });
            col.Item().Text(t =>
            {
                t.Span("End-User or Implementing Unit: ").Bold().FontSize(8);
                t.Span(ppmp.EndUserUnit).FontSize(8);
            });
        });
    }

    private static void PhaseBox(IContainer container, string label, bool ticked) =>
        container.Row(r =>
        {
            r.ConstantItem(16).Height(12).Border(1).AlignCenter().AlignMiddle()
                .Text(ticked ? "X" : string.Empty).Bold().FontSize(8);
            r.AutoItem().PaddingLeft(4).AlignMiddle().Text(label).Bold().FontSize(9);
        });

    private void ComposeTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2.6f); // 1 Description
                c.RelativeColumn(1.3f); // 2 Type
                c.RelativeColumn(1.1f); // 3 Quantity and Size
                c.RelativeColumn(1.3f); // 4 Mode
                c.RelativeColumn(0.9f); // 5 Pre-Proc Conference
                c.RelativeColumn(0.9f); // 6 Start
                c.RelativeColumn(0.9f); // 7 End
                c.RelativeColumn(0.9f); // 8 Expected Delivery
                c.RelativeColumn(1.2f); // 9 Source of Funds
                c.RelativeColumn(1.2f); // 10 Estimated Budget
                c.RelativeColumn(1.3f); // 11 Supporting Documents
                c.RelativeColumn(1.7f); // 12 Remarks
            });

            table.Header(h =>
            {
                h.Cell().ColumnSpan(5).Element(HeadCell).Text("PROCUREMENT PROJECT DETAILS").Bold();
                h.Cell().ColumnSpan(3).Element(HeadCell).Text("PROJECTED TIMELINE (MM/YYYY)").Bold();
                h.Cell().ColumnSpan(2).Element(HeadCell).Text("FUNDING DETAILS").Bold();
                h.Cell().RowSpan(2).Element(HeadCell).Text("ATTACHED SUPPORTING DOCUMENTS").Bold();
                h.Cell().RowSpan(2).Element(HeadCell).Text("REMARKS").Bold();

                foreach (var title in ColumnTitles)
                    h.Cell().Element(HeadCell).Text(title).Bold();

                for (var i = 1; i <= 12; i++)
                    h.Cell().Element(HeadCell).Text($"Column {i}").Bold().FontSize(6);
            });

            foreach (var item in ppmp.Items.OrderBy(i => i.ItemNo))
            {
                BodyCell(table, item.GeneralDescription);
                BodyCell(table, FormatProjectType(item.ProjectType), center: true);
                BodyCell(table, $"{item.Quantity.ToString("#,##0.##", Nf)} {item.Unit}", center: true);
                BodyCell(table, item.ModeOfProcurement, center: true);
                BodyCell(table, item.PreProcurementConference ? "Yes" : "No", center: true);
                BodyCell(table, item.ProcurementStart, center: true);
                BodyCell(table, item.ProcurementEnd, center: true);
                BodyCell(table, item.ExpectedDelivery, center: true);
                BodyCell(table, item.SourceOfFunds, center: true);
                table.Cell().Border(0.5f).Padding(2).AlignRight().AlignMiddle()
                    .Text(item.EstimatedBudget.ToString("N2", Nf));
                BodyCell(table, item.SupportingDocuments ?? string.Empty);
                BodyCell(table, item.Remarks ?? string.Empty);
            }

            // TOTAL BUDGET sits under Source of Funds / Estimated Budget, like the paper form.
            table.Cell().ColumnSpan(8);
            table.Cell().Border(0.5f).Padding(2).AlignMiddle().Text("TOTAL BUDGET:").Bold();
            table.Cell().Border(0.5f).Padding(2).AlignRight().AlignMiddle()
                .Text(ppmp.TotalEstimatedBudget.ToString("N2", Nf)).Bold();
            table.Cell().ColumnSpan(2);
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

    private void ComposeSignatures(IContainer container)
    {
        var preparerName = preparer is null ? string.Empty : $"{preparer.FirstName} {preparer.LastName}".ToUpperInvariant();
        var preparerPosition = preparer?.PositionName ?? string.Empty;

        container.PaddingTop(18).ShowEntire().Row(row =>
        {
            row.ConstantItem(30);
            row.ConstantItem(220).Element(c => SignatureBlock(c, "Prepared by:", preparerName, preparerPosition,
                "[End-User or Implementing Unit]"));
            row.ConstantItem(60);
            row.ConstantItem(220).Element(c => SignatureBlock(c, "Submitted by:", string.Empty, string.Empty,
                "[Head of the End-User or Implementing Unit]"));
            row.RelativeItem();
        });
    }

    private static void SignatureBlock(IContainer container, string label, string name, string position, string role) =>
        container.Column(c =>
        {
            c.Item().Text(label).FontSize(8);
            c.Item().PaddingTop(20).BorderBottom(0.75f).AlignCenter().Text(name).Bold().FontSize(8);
            c.Item().AlignCenter().Text("Signature over Printed Name").FontSize(7);
            c.Item().AlignCenter().Text(string.IsNullOrWhiteSpace(position) ? "Position/Designation" : position).FontSize(7);
            c.Item().AlignCenter().Text(role).Italic().Underline().FontSize(7);
            c.Item().PaddingTop(10).Text("Date : ______________________").FontSize(7);
        });

    private static string FormatProjectType(ProjectType type) => type switch
    {
        ProjectType.Infrastructure => "Infrastructure",
        ProjectType.ConsultingServices => "Consulting Services",
        _ => "Goods"
    };
}
