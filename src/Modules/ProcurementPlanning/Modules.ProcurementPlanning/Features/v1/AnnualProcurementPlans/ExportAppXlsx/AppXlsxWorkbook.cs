using System.Globalization;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using ClosedXML.Excel;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ExportAppXlsx;

/// <summary>
/// Builds the GPPB APP form (RA 12009) as an .xlsx workbook. Mirrors the QuestPDF layout: agency header with
/// phase boxes, two-tier 12-column header, section heading rows, live SUM/SUMIF totals, and signature blocks.
/// </summary>
internal static class AppXlsxWorkbook
{
    private const int ColumnCount = 12;
    private const int BudgetColumn = 10;
    private const string MoneyFormat = "#,##0.00";
    private const string Ballot = "☐";        // ☐
    private const string BallotChecked = "☒"; // ☒

    private static readonly AppSection[] Sections =
        [AppSection.GeneralRequirements, AppSection.DirectAcquisition, AppSection.CommonUseSupplies];

    private static readonly double[] ColumnWidths = [32, 16, 32, 18, 12, 12, 12, 12, 18, 16, 16, 24];

    private static readonly string[] ColumnTitles =
    [
        "Project Title", "End-User or Implementing Unit", "General Description of the Project",
        "Mode of Procurement", "To be covered by an Early Procurement", "Criteria for Bid Evaluation",
        "Start of Procurement", "End of Procurement", "Source of Fund", "Estimated Budget / Approved"
    ];

    internal static byte[] Build(
        AnnualProcurementPlanDto app,
        string? agencyName,
        IReadOnlyList<ReportSignatoryDto> signatories)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add($"APP FY{app.FiscalYear.ToString(CultureInfo.InvariantCulture)}");
        ws.Style.Font.FontName = "Arial";
        ws.Style.Font.FontSize = 9;

        for (var c = 1; c <= ColumnCount; c++)
            ws.Column(c).Width = ColumnWidths[c - 1];

        var row = WriteHeader(ws, app, agencyName);
        var firstDataRow = row;
        var cseRows = new List<int>();

        foreach (var section in Sections)
        {
            var items = app.Items.Where(i => i.Section == section).OrderBy(i => i.ItemNo).ToList();
            if (items.Count == 0)
                continue;

            var heading = ws.Range(row, 1, row, ColumnCount).Merge();
            heading.Value = section.ToFormTitle();
            heading.Style.Font.Bold = true;
            Bordered(heading);
            row++;

            foreach (var item in items)
            {
                WriteItem(ws, row, item);
                if (section == AppSection.CommonUseSupplies)
                    cseRows.Add(row);
                row++;
            }
        }

        var lastDataRow = Math.Max(firstDataRow, row - 1);
        row = WriteTotals(ws, row + 1, firstDataRow, lastDataRow, cseRows);
        WriteSignatures(ws, row + 2, signatories);

        ConfigurePrint(ws);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // Rows 1-3: agency / title / phase boxes. Rows 5-7: two-tier header + "Column n" row. Returns first data row.
    private static int WriteHeader(IXLWorksheet ws, AnnualProcurementPlanDto app, string? agencyName)
    {
        Title(ws, 1, (agencyName ?? string.Empty).ToUpperInvariant(), 12);
        Title(ws, 2, $"ANNUAL PROCUREMENT PLAN FOR FY {app.FiscalYear.ToString(CultureInfo.InvariantCulture)}", 11);

        var version = app.Phase == AppPhase.Updated ? app.VersionNumber.ToString(CultureInfo.InvariantCulture) : "_____";
        Title(ws, 3,
            $"{Box(app.Phase == AppPhase.Indicative)} INDICATIVE      {Box(app.Phase == AppPhase.Final)} FINAL      " +
            $"{Box(app.Phase == AppPhase.Updated)} UPDATED [Version No. {version}]", 10);

        const int top = 5;
        GroupHeader(ws, top, 1, 6, "PROCUREMENT PROJECT DETAILS");
        GroupHeader(ws, top, 7, 8, "PROJECTED TIMELINE (MM/YYYY)");
        GroupHeader(ws, top, 9, 10, "FUNDING DETAILS");
        HeaderCell(ws.Range(top, 11, top + 1, 11).Merge(), "PROCUREMENT STRATEGY OR TOOLS");
        HeaderCell(ws.Range(top, 12, top + 1, 12).Merge(), "REMARKS (Other relevant descriptions of the procurement project, if any)");

        for (var c = 1; c <= ColumnTitles.Length; c++)
            HeaderCell(ws.Range(top + 1, c, top + 1, c), ColumnTitles[c - 1]);

        for (var c = 1; c <= ColumnCount; c++)
            HeaderCell(ws.Range(top + 2, c, top + 2, c), $"Column {c.ToString(CultureInfo.InvariantCulture)}");

        ws.Row(top + 1).Height = 36;
        ws.Row(top).Height = 24;
        return top + 3;
    }

    private static void WriteItem(IXLWorksheet ws, int row, AppItemDto item)
    {
        string[] text =
        [
            string.IsNullOrWhiteSpace(item.ProjectTitle) ? item.GeneralDescription : item.ProjectTitle,
            item.EndUserUnit,
            item.GeneralDescription,
            item.ModeOfProcurement,
            item.IsEarlyProcurement ? "Yes" : "No",
            item.BidEvaluationCriteria.ToDisplay(),
            item.ProcurementStart,
            item.ProcurementEnd,
            item.SourceOfFunds,
        ];
        for (var c = 1; c <= text.Length; c++)
            ws.Cell(row, c).Value = text[c - 1];

        ws.Cell(row, BudgetColumn).Value = item.EstimatedBudget;
        ws.Cell(row, BudgetColumn).Style.NumberFormat.Format = MoneyFormat;
        ws.Cell(row, 11).Value = string.IsNullOrWhiteSpace(item.ProcurementStrategy) ? "-" : item.ProcurementStrategy;
        ws.Cell(row, 12).Value = item.Remarks ?? string.Empty;

        var range = ws.Range(row, 1, row, ColumnCount);
        range.Style.Alignment.WrapText = true;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        foreach (var c in new[] { 2, 4, 5, 6, 7, 8, 9, 11 })
            ws.Cell(row, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        Bordered(range);
    }

    // Live formulas so totals stay correct if the user edits budgets or the EPA flag in Excel.
    private static int WriteTotals(IXLWorksheet ws, int row, int firstDataRow, int lastDataRow, List<int> cseRows)
    {
        var budget = $"J{firstDataRow}:J{lastDataRow}";
        var early = $"E{firstDataRow}:E{lastDataRow}";
        var cseFormula = cseRows.Count == 0 ? "0" : $"SUM({string.Join(',', cseRows.Select(r => $"J{r}"))})";

        ws.Cell(row, 1).Value = "Note: Insert additional rows as necessary";
        ws.Cell(row, 1).Style.Font.Italic = true;
        ws.Cell(row, 1).Style.Font.FontSize = 8;

        TotalLine(ws, row, "Total Amount of Estimated Budget for EPA Projects:", $"SUMIF({early},\"Yes\",{budget})", bold: false);
        TotalLine(ws, row + 1, "Total Amount of CSEs to be purchased from PS-DBM:", cseFormula, bold: false);
        TotalLine(ws, row + 2, "Total Amount of Estimated Budget:", $"SUM({budget})", bold: true);
        return row + 2;
    }

    private static void TotalLine(IXLWorksheet ws, int row, string label, string formula, bool bold)
    {
        var labelRange = ws.Range(row, 6, row, 9).Merge();
        labelRange.Value = label;
        labelRange.Style.Font.Bold = true;
        labelRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        var cell = ws.Cell(row, BudgetColumn);
        cell.FormulaA1 = formula;
        cell.Style.NumberFormat.Format = MoneyFormat;
        cell.Style.Font.Bold = bold;
    }

    // Up to three signature blocks spread across the sheet (columns A-B, E-H, J-L), like the paper form.
    private static void WriteSignatures(IXLWorksheet ws, int row, IReadOnlyList<ReportSignatoryDto> signatories)
    {
        (int From, int To)[] slots = [(1, 2), (5, 8), (10, 12)];
        for (var i = 0; i < signatories.Count && i < slots.Length; i++)
        {
            var s = signatories[i];
            var (from, to) = slots[i];

            var label = ws.Range(row, from, row, to).Merge();
            label.Value = s.Label;
            label.Style.Alignment.WrapText = true;

            SignatureLine(ws.Range(row + 3, from, row + 3, to).Merge(), s.Name.ToUpperInvariant(), bold: true)
                .Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            SignatureLine(ws.Range(row + 4, from, row + 4, to).Merge(), "Signature over Printed Name", bold: false);
            SignatureLine(ws.Range(row + 5, from, row + 5, to).Merge(), "Position/Designation", bold: false);
            var title = SignatureLine(ws.Range(row + 6, from, row + 6, to).Merge(), s.Title, bold: false);
            title.Style.Font.Italic = true;
            title.Style.Font.Underline = XLFontUnderlineValues.Single;
            ws.Range(row + 8, from, row + 8, to).Merge().Value = "Date : ______________________";
        }

        ws.Row(row).Height = 26;
    }

    private static IXLRange SignatureLine(IXLRange range, string text, bool bold)
    {
        range.Value = text;
        range.Style.Font.Bold = bold;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        return range;
    }

    // 8.5 × 13 in landscape (Excel "Folio"), fit all columns on one page width, repeat the header rows.
    private static void ConfigurePrint(IXLWorksheet ws)
    {
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.PaperSize = XLPaperSize.FolioPaper;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.SetRowsToRepeatAtTop(5, 7);
        ws.PageSetup.Margins.Left = ws.PageSetup.Margins.Right = 0.3;
        ws.PageSetup.Margins.Top = ws.PageSetup.Margins.Bottom = 0.4;
        ws.PageSetup.CenterHorizontally = true;
        ws.PageSetup.Footer.Right.AddText("Page &P of &N");
        ws.SheetView.FreezeRows(7);
    }

    private static void Title(IXLWorksheet ws, int row, string text, double size)
    {
        var range = ws.Range(row, 1, row, ColumnCount).Merge();
        range.Value = text;
        range.Style.Font.Bold = true;
        range.Style.Font.FontSize = size;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void GroupHeader(IXLWorksheet ws, int row, int from, int to, string text) =>
        HeaderCell(ws.Range(row, from, row, to).Merge(), text);

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

    private static string Box(bool ticked) => ticked ? BallotChecked : Ballot;
}
