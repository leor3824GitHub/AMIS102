using System.Reflection;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.Settings;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.BudgetDisbursement.PrintBudgetUtilizationRequest;

/// <summary>
/// Budget Utilization Request and Status (BURS) — NFA form layout (Appendix 14). A single bordered
/// government form with the title/serial band, Payee/Office/Address band, the Particulars &amp; Amount
/// table (Responsibility Center · MFO/PAP · UACS Object Code), the Certified (A) / Certified (B)
/// signatory boxes, and the Status of Utilization grid.
/// </summary>
internal sealed class BudgetUtilizationRequestPdfDocument(
    BudgetUtilizationRequestDto bur,
    OrganizationProfileDto? org,
    BudgetDisbursementSettingsDto? financeSettings = null,
    string? payee = null,
    string? payeeAddress = null,
    string paperSize = "a4",
    string orientation = "portrait",
    float marginMm = 14f) : IDocument
{
    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"BURS — {bur.BurNumber}",
        Author = org?.Name ?? string.Empty
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            QuestPdfPaperSize.Apply(page, paperSize, orientation, marginMm);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

            page.Content().Column(outer =>
            {
                outer.Item().AlignRight().PaddingBottom(2).Text("Appendix 14").Italic().FontSize(9);

                outer.Item().Border(1).Column(col =>
                {
                    col.Item().Element(ComposeTitleBar);
                    col.Item().Element(ComposePayeeBand);
                    col.Item().Element(ComposeParticulars);
                    col.Item().Element(ComposeCertifyBoxes);
                    col.Item().Element(ComposeStatusOfUtilization);
                });
            });

            page.Footer().AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(7);
                x.CurrentPageNumber().FontSize(7);
                x.Span(" of ").FontSize(7);
                x.TotalPages().FontSize(7);
            });
        });
    }

    // ── Title / Serial band ─────────────────────────────────────────────────────

    private void ComposeTitleBar(IContainer container)
    {
        container.BorderBottom(1).Row(row =>
        {
            row.RelativeItem(3).BorderRight(1).Padding(8).AlignMiddle().Column(c =>
            {
                c.Item().AlignCenter().Text("BUDGET UTILIZATION REQUEST AND STATUS").Bold().FontSize(12);
                c.Item().AlignCenter().Text("NATIONAL FOOD AUTHORITY").Bold().FontSize(11);
                if (!string.IsNullOrWhiteSpace(org?.Name))
                    c.Item().AlignCenter().Text(org!.Name).Bold().FontSize(9);
            });

            row.RelativeItem(2).Padding(6).Column(c =>
            {
                LabelValue(c, "Serial No.", bur.BurNumber);
                c.Item().PaddingTop(6);
                LabelValue(c, "Date", bur.BurDate.ToString("MMMM dd, yyyy"));
                c.Item().PaddingTop(6);
                LabelValue(c, "Fund Cluster", string.Empty);
            });
        });
    }

    // ── Payee / Office / Address band ───────────────────────────────────────────
    // Payee and Address are sourced from the obligated purchase order (supplier name + address).
    // Office is left blank — the PO carries no office data.

    private void ComposePayeeBand(IContainer container)
    {
        container.BorderBottom(1).Column(col =>
        {
            BandRow(col, "Payee", payee ?? string.Empty, borderBottom: true);
            BandRow(col, "Office", string.Empty, borderBottom: true);
            BandRow(col, "Address", payeeAddress ?? string.Empty, borderBottom: false);
        });
    }

    // Padding lives inside the cells (not around the row) so the vertical divider runs the full
    // row height and meets the horizontal row separators, matching the pre-printed form.
    private const float PayeeLabelWidth = 70f;
    private const float PayeeRowHeight = 26f;

    private static void BandRow(ColumnDescriptor col, string label, string value, bool borderBottom)
    {
        var item = borderBottom ? col.Item().BorderBottom(1) : col.Item();
        item.Row(r =>
        {
            r.ConstantItem(PayeeLabelWidth).MinHeight(PayeeRowHeight).Padding(4)
                .AlignMiddle().AlignCenter().Text(label).FontSize(9);
            r.RelativeItem().BorderLeft(1).MinHeight(PayeeRowHeight).PaddingVertical(4).PaddingLeft(6)
                .AlignMiddle().Text(value).Bold().FontSize(9);
        });
    }

    // ── Particulars / Amount table ──────────────────────────────────────────────

    private void ComposeParticulars(IContainer container)
    {
        container.Column(col =>
        {
            // Header row
            col.Item().BorderBottom(1).Row(row =>
            {
                HeaderCell(row, 2, "Responsibility\nCenter");
                HeaderCell(row, 4, "Particulars");
                HeaderCell(row, 2, "MFO/PAP");
                HeaderCell(row, 2, "UACS Object\nCode/\nExpenditures");
                HeaderCell(row, 3, "Amount", last: true);
            });

            // Body row
            col.Item().BorderBottom(1).Row(row =>
            {
                row.RelativeItem(2).BorderRight(1).Padding(4).MinHeight(200).AlignCenter().Text(bur.ResponsibilityCenter ?? string.Empty).FontSize(8);
                row.RelativeItem(4).BorderRight(1).Padding(4).Text(bur.Particulars).FontSize(8);
                row.RelativeItem(2).BorderRight(1).Padding(4).Text(string.Empty).FontSize(8);
                row.RelativeItem(2).BorderRight(1).Padding(4).AlignCenter().Text(bur.UacsObjectCode).FontSize(8);
                row.RelativeItem(3).Padding(4).Row(a =>
                {
                    a.ConstantItem(12).Text("₱").FontSize(9);
                    a.RelativeItem().AlignRight().Text(bur.Amount.ToString("N2")).FontSize(9);
                });
            });

            // Total row
            col.Item().Row(row =>
            {
                row.RelativeItem(10).BorderRight(1).Padding(4).AlignRight().Text("Total").Bold().FontSize(10);
                row.RelativeItem(3).Padding(4).Row(a =>
                {
                    a.ConstantItem(12).Text("₱").Bold().FontSize(9);
                    a.RelativeItem().AlignRight().Text(bur.Amount.ToString("N2")).Bold().FontSize(9);
                });
            });
        });
    }

    private static void HeaderCell(RowDescriptor row, uint size, string text, bool last = false)
    {
        var item = row.RelativeItem(size);
        if (!last) item = item.BorderRight(1);
        item.Padding(3).AlignMiddle().AlignCenter().Text(text).Bold().FontSize(9);
    }

    // ── Signatory resolution (Budget Disbursement Settings override → Org Profile fallback) ──

    private string? BurSectionAName => Coalesce(financeSettings?.BurSectionAName, org?.ApprovingOfficialName?.ToUpperInvariant());
    private string? BurSectionADesignation => Coalesce(financeSettings?.BurSectionADesignation, org?.ApprovingOfficialDesignation) ?? "Regional Manager II";
    private string? BurSectionBName => Coalesce(financeSettings?.BurSectionBName, org?.BudgetOfficerName?.ToUpperInvariant());
    private string? BurSectionBDesignation => Coalesce(financeSettings?.BurSectionBDesignation, org?.BudgetOfficerDesignation) ?? "Budget Officer";

    private static string? Coalesce(string? primary, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(primary)) return primary;
        if (!string.IsNullOrWhiteSpace(fallback)) return fallback;
        return null;
    }

    // ── Certified (A) / Certified (B) boxes ─────────────────────────────────────

    // Box A's certification text wraps to 3 lines while box B's wraps to 2. Pinning both
    // header cells to the same minimum height keeps the divider line and the Signature /
    // Printed Name / Position / Date blocks beneath aligned across the two boxes.
    private const float CertifyHeaderHeight = 44f;

    // Signatory block label/colon geometry — a fixed label column plus a narrow colon column
    // keeps every ":" on the same vertical line and every value/rule starting at the same x.
    private const float LabelWidth = 72f;
    private const float ColonWidth = 8f;

    private void ComposeCertifyBoxes(IContainer container)
    {
        container.BorderTop(1).Row(row =>
        {
            // A. CERTIFIED — Requesting Office
            row.RelativeItem().BorderRight(1).Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.ConstantItem(16).BorderRight(1).BorderBottom(1).AlignCenter().AlignMiddle().Text("A.").Bold().FontSize(9);
                    r.RelativeItem().BorderBottom(1).MinHeight(CertifyHeaderHeight).Padding(4).Text(t =>
                    {
                        t.Span("Certified: ").Bold().FontSize(8);
                        t.Span("Charges to appropriation/budget necessary, lawful and under my direct supervision; and supporting documents valid, proper and legal").FontSize(8);
                    });
                });
                SignatoryBlock(col,
                    BurSectionAName,
                    BurSectionADesignation ?? "Regional Manager II",
                    "Head, Requesting Office/Authorized Representative");
            });

            // B. CERTIFIED — Budget Division
            row.RelativeItem().Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.ConstantItem(16).BorderRight(1).BorderBottom(1).AlignCenter().AlignMiddle().Text("B.").Bold().FontSize(9);
                    r.RelativeItem().BorderBottom(1).MinHeight(CertifyHeaderHeight).Padding(4).Text(t =>
                    {
                        t.Span("Certified: ").Bold().FontSize(8);
                        t.Span("Budget available and utilized for the purpose/adjustment necessary as indicated above").FontSize(8);
                    });
                });
                SignatoryBlock(col,
                    BurSectionBName,
                    BurSectionBDesignation ?? "Budget Officer",
                    "Head, Budget Division/Authorized Representative");
            });
        });
    }

    private static void SignatoryBlock(ColumnDescriptor col, string? printedName, string position, string positionSub)
    {
        col.Item().Padding(6).Column(c =>
        {
            c.Item().PaddingTop(18).Row(r =>
            {
                r.ConstantItem(LabelWidth).AlignMiddle().Text("Signature").FontSize(7);
                r.ConstantItem(ColonWidth).AlignMiddle().Text(":").FontSize(7);
                r.RelativeItem().PaddingLeft(4).MinHeight(12).BorderBottom(0.5f);
            });
            c.Item().PaddingTop(8).Row(r =>
            {
                r.ConstantItem(LabelWidth).AlignMiddle().Text("Printed Name").FontSize(7);
                r.ConstantItem(ColonWidth).AlignMiddle().Text(":").FontSize(7);
                // r.RelativeItem().PaddingLeft(4).Column(nameCol =>
                // {
                //     nameCol.Item().AlignCenter().Text(printedName ?? string.Empty).Bold().FontSize(9);
                //     nameCol.Item().AlignCenter().LineHorizontal(0.5f);
                // });
                r.RelativeItem().PaddingLeft(4).MinHeight(12).BorderBottom(0.5f).AlignBottom().AlignCenter().Text(printedName ?? string.Empty).Bold().FontSize(9);
            });
            c.Item().PaddingTop(8).Row(r =>
            {
                r.ConstantItem(LabelWidth).AlignMiddle().Text("Position").FontSize(7);
                r.ConstantItem(ColonWidth).AlignMiddle().Text(":").FontSize(7);
                r.RelativeItem().PaddingLeft(4).Column(p =>
                {
                    p.Item().BorderBottom(0.5f).AlignCenter().Text(position).FontSize(8);
                    p.Item().AlignCenter().Text(positionSub).FontSize(7);
                });
            });
            c.Item().PaddingTop(8).Row(r =>
            {
                r.ConstantItem(LabelWidth).AlignMiddle().Text("Date").FontSize(7);
                r.ConstantItem(ColonWidth).AlignMiddle().Text(":").FontSize(7);
                r.RelativeItem().PaddingLeft(4).AlignBottom().LineHorizontal(0.5f);
            });
        });
    }

    // ── C. Status of Utilization grid ───────────────────────────────────────────

    private void ComposeStatusOfUtilization(IContainer container)
    {
        container.BorderTop(1).Column(col =>
        {
            // Section title
            col.Item().BorderBottom(1).Row(r =>
            {
                r.ConstantItem(16).BorderRight(1).AlignCenter().AlignMiddle().Text("C.").Bold().FontSize(9);
                r.RelativeItem().Padding(3).AlignCenter().Text("STATUS OF UTILIZATION").Bold().FontSize(10);
            });

            // Grouping header: Reference | Amount
            col.Item().BorderBottom(1).Row(r =>
            {
                r.RelativeItem(7).BorderRight(1).Padding(3).AlignCenter().Text("Reference").Bold().FontSize(9);
                r.RelativeItem(8).Padding(3).AlignCenter().Text("Amount").Bold().FontSize(9);
            });

            // Column header
            col.Item().BorderBottom(1).Row(r =>
            {
                HeaderCell(r, 2, "Date");
                HeaderCell(r, 3, "Particulars");
                HeaderCell(r, 2, "BURS/JEV/RCI/\nRADAI/RTRAI No.");
                HeaderCell(r, 2, "Utilization\n(a)");
                HeaderCell(r, 2, "Payable\n(b)");
                HeaderCell(r, 2, "Payment\n(c)");
                HeaderCell(r, 2, "Not Yet Due\n(a-b)");
                HeaderCell(r, 2, "Due and\nDemandable\n(b-c)", last: true);
            });

            // First data row — seeds the grid with this BURS reference
            col.Item().Row(r =>
            {
                r.RelativeItem(2).BorderRight(1).Padding(3).MinHeight(70).Text(bur.BurDate.ToString("MM/dd/yyyy")).FontSize(8);
                r.RelativeItem(3).BorderRight(1).Padding(3).Text(bur.Particulars).FontSize(8);
                r.RelativeItem(2).BorderRight(1).Padding(3).Text(bur.BurNumber).FontSize(8);
                r.RelativeItem(2).BorderRight(1).Padding(3).AlignRight().Text(bur.Amount.ToString("N2")).FontSize(8);
                r.RelativeItem(2).BorderRight(1).Padding(3);
                r.RelativeItem(2).BorderRight(1).Padding(3);
                r.RelativeItem(2).BorderRight(1).Padding(3);
                r.RelativeItem(2).Padding(3);
            });
        });
    }

    // ── small helpers ───────────────────────────────────────────────────────────

    private static void LabelValue(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(r =>
        {
            r.ConstantItem(72).Text($"{label} :").FontSize(8);
            r.RelativeItem().AlignBottom().BorderBottom(0.5f).PaddingLeft(4).Text(value).FontSize(9);
        });
    }
}
