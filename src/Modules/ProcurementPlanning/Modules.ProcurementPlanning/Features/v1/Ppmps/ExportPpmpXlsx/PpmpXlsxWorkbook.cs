using System.Globalization;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using ClosedXML.Excel;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.ExportPpmpXlsx;

/// <summary>
/// Builds the GPPB PPMP form as an .xlsx workbook. Mirrors the QuestPDF layout: letterhead, PPMP No.,
/// Indicative / Final boxes, Fiscal Year + End-User lines, two-tier 12-column header, a live TOTAL BUDGET
/// formula, and the Prepared by / Submitted by blocks.
/// </summary>
internal static class PpmpXlsxWorkbook
{
    private const int ColumnCount = 12;
    private const int BudgetColumn = 10;
    private const int HeaderTop = 7;
    private const string MoneyFormat = "#,##0.00";
    private const string Ballot = "☐";        // ☐
    private const string BallotChecked = "☒"; // ☒

    private static readonly double[] ColumnWidths = [36, 18, 14, 18, 14, 13, 13, 14, 18, 16, 20, 26];
    private static readonly int[] CenteredColumns = [2, 3, 4, 5, 6, 7, 8, 9];

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

    internal static byte[] Build(PpmpDto ppmp, string? agencyName, EmployeeReferenceDto? preparer)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(SheetName(ppmp.PpmpNumber));
        ws.Style.Font.FontName = "Arial";
        ws.Style.Font.FontSize = 9;

        for (var c = 1; c <= ColumnCount; c++)
            ws.Column(c).Width = ColumnWidths[c - 1];

        WriteHeader(ws, ppmp, agencyName);

        var row = HeaderTop + 3;
        var firstDataRow = row;
        foreach (var item in ppmp.Items.OrderBy(i => i.ItemNo))
            WriteItem(ws, row++, item);
        var lastDataRow = Math.Max(firstDataRow, row - 1);

        // TOTAL BUDGET under Source of Funds / Estimated Budget, as a live SUM over the item rows.
        var label = ws.Cell(row, BudgetColumn - 1);
        label.Value = "TOTAL BUDGET:";
        label.Style.Font.Bold = true;
        var total = ws.Cell(row, BudgetColumn);
        total.FormulaA1 = $"SUM(J{firstDataRow}:J{lastDataRow})";
        total.Style.NumberFormat.Format = MoneyFormat;
        total.Style.Font.Bold = true;
        Bordered(ws.Range(row, BudgetColumn - 1, row, BudgetColumn));

        WriteSignatures(ws, row + 3, preparer);
        ConfigurePrint(ws);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteHeader(IXLWorksheet ws, PpmpDto ppmp, string? agencyName)
    {
        Title(ws, 1, (agencyName ?? string.Empty).ToUpperInvariant(), 12);
        Title(ws, 2, $"PROJECT PROCUREMENT MANAGEMENT PLAN (PPMP) NO. {ppmp.PpmpNumber}", 11);

        var final = ppmp.Phase == PpmpPhase.Updated
            ? $"FINAL (Updated — Version No. {ppmp.VersionNumber.ToString(CultureInfo.InvariantCulture)})"
            : "FINAL";
        Title(ws, 3,
            $"{Box(ppmp.Phase == PpmpPhase.Indicative)} INDICATIVE        " +
            $"{Box(ppmp.Phase is PpmpPhase.Final or PpmpPhase.Updated)} {final}", 10);

        LabelValue(ws, 5, "Fiscal Year :", ppmp.FiscalYear.ToString(CultureInfo.InvariantCulture));
        LabelValue(ws, 6, "End-User or Implementing Unit:", ppmp.EndUserUnit);

        const int top = HeaderTop;
        HeaderCell(ws.Range(top, 1, top, 5).Merge(), "PROCUREMENT PROJECT DETAILS");
        HeaderCell(ws.Range(top, 6, top, 8).Merge(), "PROJECTED TIMELINE (MM/YYYY)");
        HeaderCell(ws.Range(top, 9, top, 10).Merge(), "FUNDING DETAILS");
        HeaderCell(ws.Range(top, 11, top + 1, 11).Merge(), "ATTACHED SUPPORTING DOCUMENTS");
        HeaderCell(ws.Range(top, 12, top + 1, 12).Merge(), "REMARKS");

        for (var c = 1; c <= ColumnTitles.Length; c++)
            HeaderCell(ws.Range(top + 1, c, top + 1, c), ColumnTitles[c - 1]);

        for (var c = 1; c <= ColumnCount; c++)
            HeaderCell(ws.Range(top + 2, c, top + 2, c), $"Column {c.ToString(CultureInfo.InvariantCulture)}");

        ws.Row(top).Height = 22;
        ws.Row(top + 1).Height = 62;
    }

    private static void WriteItem(IXLWorksheet ws, int row, PpmpItemDto item)
    {
        string[] text =
        [
            item.GeneralDescription,
            FormatProjectType(item.ProjectType),
            $"{item.Quantity.ToString("#,##0.##", CultureInfo.InvariantCulture)} {item.Unit}",
            item.ModeOfProcurement,
            item.PreProcurementConference ? "Yes" : "No",
            item.ProcurementStart,
            item.ProcurementEnd,
            item.ExpectedDelivery,
            item.SourceOfFunds,
        ];
        for (var c = 1; c <= text.Length; c++)
            ws.Cell(row, c).Value = text[c - 1];

        ws.Cell(row, BudgetColumn).Value = item.EstimatedBudget;
        ws.Cell(row, BudgetColumn).Style.NumberFormat.Format = MoneyFormat;
        ws.Cell(row, 11).Value = item.SupportingDocuments ?? string.Empty;
        ws.Cell(row, 12).Value = item.Remarks ?? string.Empty;

        var range = ws.Range(row, 1, row, ColumnCount);
        range.Style.Alignment.WrapText = true;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        foreach (var c in CenteredColumns)
            ws.Cell(row, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        Bordered(range);
    }

    // Prepared by (columns B-C) = the PPMP's preparer; Submitted by (columns E-G) = Head of the End-User, signed by hand.
    private static void WriteSignatures(IXLWorksheet ws, int row, EmployeeReferenceDto? preparer)
    {
        var name = preparer is null ? string.Empty : $"{preparer.FirstName} {preparer.LastName}".ToUpperInvariant();
        SignatureBlock(ws, row, 2, 3, "Prepared by:", name, preparer?.PositionName, "[End-User or Implementing Unit]");
        SignatureBlock(ws, row, 5, 7, "Submitted by:", string.Empty, null, "[Head of the End-User or Implementing Unit]");
    }

    private static void SignatureBlock(IXLWorksheet ws, int row, int from, int to,
        string label, string name, string? position, string role)
    {
        ws.Range(row, from, row, to).Merge().Value = label;

        var nameRange = Centered(ws.Range(row + 3, from, row + 3, to).Merge(), name);
        nameRange.Style.Font.Bold = true;
        nameRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        Centered(ws.Range(row + 4, from, row + 4, to).Merge(), "Signature over Printed Name");
        Centered(ws.Range(row + 5, from, row + 5, to).Merge(), string.IsNullOrWhiteSpace(position) ? "Position/Designation" : position);
        var roleRange = Centered(ws.Range(row + 6, from, row + 6, to).Merge(), role);
        roleRange.Style.Font.Italic = true;
        roleRange.Style.Font.Underline = XLFontUnderlineValues.Single;
        ws.Range(row + 8, from, row + 8, to).Merge().Value = "Date : ______________________";
    }

    private static void ConfigurePrint(IXLWorksheet ws)
    {
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.PaperSize = XLPaperSize.FolioPaper; // 8.5 × 13 in
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.SetRowsToRepeatAtTop(HeaderTop, HeaderTop + 2);
        ws.PageSetup.Margins.Left = ws.PageSetup.Margins.Right = 0.3;
        ws.PageSetup.Margins.Top = ws.PageSetup.Margins.Bottom = 0.4;
        ws.PageSetup.CenterHorizontally = true;
        ws.PageSetup.Footer.Right.AddText("Page &P of &N");
        ws.SheetView.FreezeRows(HeaderTop + 2);
    }

    // Excel sheet names: max 31 chars, none of : \ / ? * [ ].
    private static string SheetName(string ppmpNumber)
    {
        var clean = new string(ppmpNumber.Where(ch => ":\\/?*[]".IndexOf(ch) < 0).ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "PPMP" : clean[..Math.Min(31, clean.Length)];
    }

    private static void LabelValue(IXLWorksheet ws, int row, string label, string value)
    {
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Range(row, 2, row, 6).Merge().Value = value;
    }

    private static void Title(IXLWorksheet ws, int row, string text, double size)
    {
        var range = ws.Range(row, 1, row, ColumnCount).Merge();
        range.Value = text;
        range.Style.Font.Bold = true;
        range.Style.Font.FontSize = size;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static IXLRange Centered(IXLRange range, string text)
    {
        range.Value = text;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        return range;
    }

    private static void HeaderCell(IXLRange range, string text)
    {
        range.Value = text;
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Alignment.WrapText = true;
        Bordered(range);
    }

    private static void Bordered(IXLRange range)
    {
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    private static string FormatProjectType(ProjectType type) => type switch
    {
        ProjectType.Infrastructure => "Infrastructure",
        ProjectType.ConsultingServices => "Consulting Services",
        _ => "Goods"
    };

    private static string Box(bool ticked) => ticked ? BallotChecked : Ballot;
}
