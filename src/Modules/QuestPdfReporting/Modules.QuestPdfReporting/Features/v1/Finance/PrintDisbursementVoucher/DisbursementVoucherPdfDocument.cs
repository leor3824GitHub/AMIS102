using System.Reflection;
using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Finance.PrintDisbursementVoucher;

/// <summary>
/// Disbursement Voucher (DV) — COA/NFA government form layout. A single-page bordered form with the
/// agency header, title bar with DV No., Mode of Payment band, Payee / TIN / ORS-BURS band, the
/// 4-column Particulars table (Particulars / Responsibility Center / MFO-PAP / Amount) with the
/// Less &amp; Amount Due breakdown, and the Certified (A) / Certified (B) / Approved (C) /
/// Receipt of Payment (D) boxes.
/// </summary>
internal sealed class DisbursementVoucherPdfDocument(
    DisbursementVoucherDto  dv,
    OrganizationProfileDto? org,
    string?                 responsibilityCenter = null,
    string                  paperSize   = "a4",
    string                  orientation = "portrait",
    float                   marginMm    = 14f) : IDocument
{
    private bool IsMdsCheck => dv.ModeOfPayment.Contains("MDS", StringComparison.OrdinalIgnoreCase);
    private bool IsCommercialCheck => dv.ModeOfPayment.Contains("Commercial", StringComparison.OrdinalIgnoreCase);
    private bool IsAda => dv.ModeOfPayment.Contains("ADA", StringComparison.OrdinalIgnoreCase);
    private bool IsOthers => !IsMdsCheck && !IsCommercialCheck && !IsAda;

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"DV — {dv.DvNumber}",
        Author = org?.Name ?? string.Empty
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            QuestPdfPaperSize.Apply(page, paperSize, orientation, marginMm);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));
            page.Content().Border(1).Column(col =>
            {
                col.Item().Element(ComposeHeader);
                col.Item().Element(ComposeTitleBar);
                col.Item().Element(ComposeModeOfPayment);
                col.Item().Element(ComposePayeeAddress);
                // Particulars absorbs the leftover page height so the form fills a single page
                // and the A/B/C/D blocks sit flush at the bottom (no empty gap).
                col.Item().ExtendVertical().Element(ComposeParticulars);
                col.Item().Element(ComposeCertifiedBySupervisor);
                col.Item().Element(ComposeCertifyApprove);
                col.Item().Element(ComposeReceiptOfPayment);
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

    private void ComposeHeader(IContainer container)
    {
        container.BorderBottom(1).Padding(4).Row(row =>
        {
            // Logo on the left; an equal-width spacer on the right keeps the agency text optically
            // centered on the page. When no logo asset is bundled, both sides collapse to nothing.
            var logo = LogoBytes.Value;
            if (logo is not null)
                row.ConstantItem(60).AlignMiddle().Height(48).Image(logo).FitArea();
            else
                row.ConstantItem(60);

            row.RelativeItem().AlignMiddle().Column(col =>
            {
                col.Item().AlignCenter().Text("Republic of the Philippines").FontSize(8);
                col.Item().AlignCenter().Text("NATIONAL FOOD AUTHORITY").Bold().FontSize(11);
                if (!string.IsNullOrWhiteSpace(org?.Name))
                    col.Item().AlignCenter().Text(org!.Name).Bold().FontSize(9);
                if (!string.IsNullOrWhiteSpace(org?.Address))
                    col.Item().AlignCenter().Text(org!.Address).FontSize(8);
            });

            row.ConstantItem(60);
        });
    }

    // Bundled agency logo, loaded once from any image embedded under the module's ReportAssets\ folder
    // (e.g. ReportAssets\nfa-logo.png). Returns null when no asset is present so the header degrades to
    // text only. See Modules.QuestPdfReporting.csproj — ReportAssets\**\*.png|jpg are auto-embedded.
    private static readonly Lazy<byte[]?> LogoBytes = new(LoadLogo);

    private static byte[]? LoadLogo()
    {
        var asm = typeof(DisbursementVoucherPdfDocument).Assembly;
        var images = asm.GetManifestResourceNames()
            .Where(n => n.Contains(".ReportAssets.", StringComparison.OrdinalIgnoreCase)
                && (n.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || n.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)))
            .ToList();
        // Prefer an asset whose name mentions "logo"; otherwise take the first embedded image.
        var name = images.FirstOrDefault(n => n.Contains("logo", StringComparison.OrdinalIgnoreCase))
                   ?? images.FirstOrDefault();
        if (name is null)
            return null;

        using var stream = asm.GetManifestResourceStream(name);
        if (stream is null)
            return null;

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private void ComposeTitleBar(IContainer container)
    {
        container.BorderBottom(1).Row(row =>
        {
            row.RelativeItem().BorderRight(1).PaddingVertical(8).PaddingHorizontal(6).AlignCenter().AlignMiddle()
                .Text("DISBURSEMENT VOUCHER").Bold().FontSize(15);

            row.ConstantItem(180).Padding(6).AlignMiddle().Row(r =>
            {
                r.AutoItem().Text("DV No. :").FontSize(9);
                r.RelativeItem().PaddingLeft(4).AlignBottom().Text(dv.DvNumber).Bold().FontSize(9);
            });
        });
    }

    private void ComposeModeOfPayment(IContainer container)
    {
        container.BorderBottom(1).Row(row =>
        {
            row.ConstantItem(70).BorderRight(1).Padding(4).AlignMiddle().Text("Mode of\nPayment").FontSize(8);
            row.RelativeItem().Padding(4).AlignMiddle().Row(modes =>
            {
                CheckBox(modes, "MDS Check", IsMdsCheck);
                modes.ConstantItem(14);
                CheckBox(modes, "Commercial Check", IsCommercialCheck);
                modes.ConstantItem(14);
                CheckBox(modes, "ADA", IsAda);
                modes.ConstantItem(14);
                CheckBox(modes, "Others (Please specify)", IsOthers);
            });
        });
    }

    private void ComposePayeeAddress(IContainer container)
    {
        container.Column(col =>
        {
            // Payee + TIN/Employee No. + ORS/BURS No.
            col.Item().BorderBottom(1).Row(row =>
            {
                row.ConstantItem(70).BorderRight(1).Padding(4).AlignMiddle().Text("Payee").FontSize(8);
                row.RelativeItem(3).BorderRight(1).Padding(4).AlignMiddle().Text(dv.Payee).Bold().FontSize(10);
                row.RelativeItem(2).BorderRight(1).Padding(4).Column(c =>
                {
                    c.Item().Text("TIN/Employee No.:").FontSize(8);
                    c.Item().PaddingTop(2).Text(dv.TinNo ?? string.Empty).Bold().FontSize(9);
                });
                row.RelativeItem(2).Padding(4).Column(c =>
                {
                    c.Item().Text("ORS/BURS No.:").FontSize(8);
                    c.Item().PaddingTop(2).Text(dv.BurNumber ?? string.Empty).Bold().FontSize(9);
                });
            });

            // Address
            col.Item().Row(row =>
            {
                row.ConstantItem(70).BorderRight(1).Padding(4).AlignMiddle().Text("Address").FontSize(8);
                row.RelativeItem().Padding(4).MinHeight(24).AlignMiddle().Text(dv.PayeeAddress ?? string.Empty).Bold().FontSize(9);
            });
        });
    }

    private void ComposeParticulars(IContainer container)
    {
        container.BorderTop(1).Column(col =>
        {
            // Header row — Particulars | Responsibility Center | MFO/PAP | Amount
            col.Item().BorderBottom(1).Row(row =>
            {
                row.RelativeItem(6).BorderRight(1).Padding(3).AlignCenter().Text("Particulars").Bold().FontSize(9);
                row.RelativeItem(2).BorderRight(1).Padding(3).AlignCenter().Text("Responsibility\nCenter").Bold().FontSize(8);
                row.RelativeItem(2).BorderRight(1).Padding(3).AlignCenter().Text("MFO/PAP").Bold().FontSize(8);
                row.RelativeItem(3).Padding(3).AlignCenter().Text("Amount").Bold().FontSize(9);
            });

            // Body row — extends to fill the section so the column dividers run full height
            col.Item().ExtendVertical().BorderBottom(1).Row(row =>
            {
                row.RelativeItem(6).BorderRight(1).Padding(6).MinHeight(120).Text(dv.Particulars).FontSize(9);
                row.RelativeItem(2).BorderRight(1).Padding(4).Text(responsibilityCenter ?? string.Empty).FontSize(8);
                row.RelativeItem(2).BorderRight(1).Padding(4);
                row.RelativeItem(3).Column(amt =>
                {
                    amt.Item().Padding(4).Row(a =>
                    {
                        a.ConstantItem(14).Text("P").FontSize(9);
                        a.RelativeItem().AlignRight().Text(dv.Amount.ToString("N2")).FontSize(9);
                    });

                    // Configurable deductions (tax, withholding, fees). Each line prints its label —
                    // suffixed with the rate for percentage lines — and its computed peso amount. When
                    // there are no deductions the breakdown collapses and Amount Due equals the gross.
                    if (dv.Deductions.Count > 0)
                    {
                        amt.Item().PaddingHorizontal(4).PaddingTop(4).Text("Less:").FontSize(8);
                        foreach (var d in dv.Deductions)
                            DeductionLine(amt, DeductionLabel(d), d.Amount);
                        amt.Item().PaddingHorizontal(4).PaddingTop(3).AlignRight().Width(80).LineHorizontal(0.4f);
                        amt.Item().PaddingHorizontal(4).PaddingTop(2).AlignRight()
                            .Text(dv.TotalDeductions.ToString("N2")).FontSize(8);
                    }
                });
            });

            // Amount Due row
            col.Item().Row(row =>
            {
                row.RelativeItem(10).BorderRight(1).Padding(4).AlignRight().AlignMiddle().Text("Amount Due").Bold().FontSize(10);
                row.RelativeItem(3).Padding(4).Row(a =>
                {
                    a.ConstantItem(14).Text("P").Bold().FontSize(9);
                    a.RelativeItem().AlignRight().Text(dv.AmountDue.ToString("N2")).Bold().FontSize(9);
                });
            });
        });
    }

    // A. CERTIFIED — by the supervisor (Assistant Regional Manager)
    private void ComposeCertifiedBySupervisor(IContainer container)
    {
        container.BorderTop(1).Padding(6).Column(col =>
        {
            col.Item().Row(r =>
            {
                r.ConstantItem(16).Border(1).AlignCenter().Text("A").Bold().FontSize(9);
                r.RelativeItem().PaddingLeft(6).AlignMiddle()
                    .Text("Certified: Expenses/Cash Advance necessary, lawful and incurred under my direct supervision.")
                    .FontSize(8);
            });
            col.Item().PaddingTop(18).AlignCenter().Column(c =>
            {
                c.Item().AlignCenter().Text(org?.AssistantRegionalManagerName ?? string.Empty).Bold().Underline().FontSize(10);
                c.Item().AlignCenter().Text(org?.AssistantRegionalManagerDesignation ?? string.Empty).FontSize(8);
                c.Item().PaddingTop(2).AlignCenter().Text("Printed Name, Designation and Signature of Supervisor").FontSize(7);
            });
        });
    }

    // B. CERTIFIED (accountant, with checkboxes) + C. APPROVED FOR PAYMENT (agency head).
    // Rendered as one 4-column table so the B and C signatory rows (Signature / Printed Name /
    // Position / Date) line up exactly and the B|C divider runs unbroken from the header band down.
    private void ComposeCertifyApprove(IContainer container)
    {
        container.BorderTop(1).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(58);   // B label column
                c.RelativeColumn();     // B value column
                c.ConstantColumn(58);   // C label column
                c.RelativeColumn();     // C value column
            });

            // ── Header band: B (Certified + checkboxes) | C (Approved for Payment) ──
            table.Cell().ColumnSpan(2).Border(0.5f).Padding(6).Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.ConstantItem(16).Border(1).AlignCenter().AlignMiddle().Text("B").Bold().FontSize(9);
                    r.RelativeItem().PaddingLeft(6).AlignMiddle().Text("Certified:").Bold().FontSize(9);
                });
                col.Item().PaddingTop(4).Column(c =>
                {
                    CertCheck(c, "Cash available");
                    CertCheck(c, "Subject to Authority to Debit Account (when applicable)");
                    CertCheck(c, "Supporting documents complete and amount claimed proper");
                });
            });

            table.Cell().ColumnSpan(2).Border(0.5f).Padding(6).Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.ConstantItem(16).Border(1).AlignCenter().AlignMiddle().Text("C").Bold().FontSize(9);
                    r.RelativeItem().PaddingLeft(6).AlignMiddle().Text("Approved for Payment").Bold().FontSize(9);
                });
                col.Item().MinHeight(48);
            });

            // ── Signatory grid (B and C share aligned rows) ──
            SignatoryRows(table,
                org?.AccountantName,
                org?.AccountantDesignation,
                "Head, Accounting Unit/Authorized Representative",
                org?.ApprovingOfficialName,
                org?.ApprovingOfficialDesignation,
                "Agency Head/Authorized Representative");
        });
    }

    // Emits the five label/value row pairs for both signatory columns. Each cell is a bordered box,
    // matching the COA form grid. The "Position" designation sits on its own row above an unlabeled
    // role row (e.g. "Head, Accounting Unit/Authorized Representative").
    private static void SignatoryRows(
        TableDescriptor table,
        string? nameB, string? designationB, string roleB,
        string? nameC, string? designationC, string roleC)
    {
        // Signature
        LabelCell(table, "Signature");
        table.Cell().Border(0.5f).MinHeight(24);
        LabelCell(table, "Signature");
        table.Cell().Border(0.5f).MinHeight(24);

        // Printed Name
        LabelCell(table, "Printed Name");
        ValueCell(table, nameB, bold: true, size: 9);
        LabelCell(table, "Printed Name");
        ValueCell(table, nameC, bold: true, size: 9);

        // Position (designation)
        LabelCell(table, "Position");
        ValueCell(table, designationB, bold: false, size: 8);
        LabelCell(table, "Position");
        ValueCell(table, designationC, bold: false, size: 8);

        // Role (unlabeled continuation of Position)
        table.Cell().Border(0.5f);
        ValueCell(table, roleB, bold: false, size: 8);
        table.Cell().Border(0.5f);
        ValueCell(table, roleC, bold: false, size: 8);

        // Date
        LabelCell(table, "Date");
        table.Cell().Border(0.5f).MinHeight(16);
        LabelCell(table, "Date");
        table.Cell().Border(0.5f).MinHeight(16);
    }

    private static void LabelCell(TableDescriptor table, string text) =>
        table.Cell().Border(0.5f).Padding(3).AlignMiddle().Text(text).FontSize(7);

    private static void ValueCell(TableDescriptor table, string? text, bool bold, float size) =>
        table.Cell().Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text(t =>
        {
            var span = t.Span(text ?? string.Empty).FontSize(size);
            if (bold) span.Bold();
        });

    // D. RECEIPT OF PAYMENT — bordered grid matching the COA form cells.
    private static void ComposeReceiptOfPayment(IContainer container)
    {
        container.BorderTop(1).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(70);   // labels (Check/ADA No., Signature)
                c.RelativeColumn(2);    // value box
                c.ConstantColumn(42);   // "Date :" label
                c.RelativeColumn(3);    // Bank Name & Account Number / Printed Name
                c.RelativeColumn(2);    // JEV No. / Date value
            });

            // Header row: D. Receipt of Payment | JEV No.
            table.Cell().ColumnSpan(4).Padding(4).Row(r =>
            {
                r.ConstantItem(16).Border(1).AlignCenter().AlignMiddle().Text("D").Bold().FontSize(9);
                r.RelativeItem().PaddingLeft(6).AlignMiddle().Text("Receipt of Payment").Bold().FontSize(9);
            });
            table.Cell().BorderLeft(0.5f).Padding(4).Text("JEV  No.").FontSize(8);

            // Row 1: Check/ADA No. | value | Date | Bank Name & Account Number | value
            table.Cell().Border(0.5f).Padding(4).Text("Check/\nADA No. :").FontSize(8);
            table.Cell().Border(0.5f).MinHeight(26);
            table.Cell().Border(0.5f).Padding(4).Text("Date :").FontSize(8);
            table.Cell().Border(0.5f).Padding(4).Text("Bank Name & Account Number:").FontSize(8);
            table.Cell().Border(0.5f).MinHeight(26);

            // Row 2: Signature | value | Date | Printed Name | Date value
            table.Cell().Border(0.5f).Padding(4).Text("Signature :").FontSize(8);
            table.Cell().Border(0.5f).MinHeight(26);
            table.Cell().Border(0.5f).Padding(4).Text("Date :").FontSize(8);
            table.Cell().Border(0.5f).Padding(4).Text("Printed Name:").FontSize(8);
            table.Cell().Border(0.5f).Padding(4).Text("Date").FontSize(8);

            // Footer spanning the full width — boxed to the same height as the signature cells
            table.Cell().ColumnSpan(5).Border(0.5f).MinHeight(26).Padding(4)
                .Text("Official Receipt No. & Date/Other Documents").FontSize(8);
        });
    }

    // ── small helpers ──────────────────────────────────────────────────────────

    private static void CheckBox(RowDescriptor row, string label, bool on)
    {
        row.AutoItem().AlignMiddle().Border(1).Width(10).Height(10).AlignCenter().AlignMiddle()
            .Text(on ? "X" : string.Empty).FontSize(8);
        row.AutoItem().PaddingLeft(3).AlignMiddle().Text(label).FontSize(8);
    }

    private static void CertCheck(ColumnDescriptor col, string text)
    {
        col.Item().PaddingTop(6).Row(r =>
        {
            r.ConstantItem(20).Border(1).Height(14);
            r.RelativeItem().PaddingLeft(8).AlignMiddle().Text(text).FontSize(8);
        });
    }

    // A deduction breakdown line: label on the left, computed peso amount right-aligned.
    private static void DeductionLine(ColumnDescriptor col, string label, decimal amount)
    {
        col.Item().PaddingHorizontal(4).PaddingTop(3).Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(8);
            r.ConstantItem(70).AlignRight().Text(amount.ToString("N2")).FontSize(8);
        });
    }

    // "5% Withholding Tax" for percentage lines; the bare name for fixed-amount lines.
    private static string DeductionLabel(DvDeductionDto d) =>
        d.Type == DvDeductionType.Percentage
            ? $"{d.Value:0.##}% {d.Name}"
            : d.Name;
}
