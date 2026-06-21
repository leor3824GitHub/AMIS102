using System.Reflection;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.Settings;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.BudgetDisbursement.PrintDisbursementVoucher;

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
    BudgetDisbursementSettingsDto?     financeSettings      = null,
    string?                 responsibilityCenter = null,
    string                  paperSize   = "a4",
    string                  orientation = "portrait",
    float                   marginMm    = 10f) : IDocument
{
    // Signatory resolution: Budget Disbursement Settings override takes precedence over org profile fallback.
    private string? SectionAName        => Coalesce(financeSettings?.DvSectionAName,        org?.AssistantRegionalManagerName);
    private string? SectionADesignation => Coalesce(financeSettings?.DvSectionADesignation, org?.AssistantRegionalManagerDesignation);
    private string? SectionBName        => Coalesce(financeSettings?.DvSectionBName,        org?.AccountantName);
    private string? SectionBDesignation => Coalesce(financeSettings?.DvSectionBDesignation, org?.AccountantDesignation);
    private string? SectionCName        => Coalesce(financeSettings?.DvSectionCName,        org?.ApprovingOfficialName);
    private string? SectionCDesignation => Coalesce(financeSettings?.DvSectionCDesignation, org?.ApprovingOfficialDesignation);

    private static string? Coalesce(string? primary, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(primary)) return primary;
        if (!string.IsNullOrWhiteSpace(fallback)) return fallback;
        return null;
    }

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
                // Particulars takes only the height its content needs (a fixed minimum) so the
                // A/B/C/D blocks follow directly beneath it and the whole form stays on one page.
                col.Item().Element(ComposeParticulars);
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
        container.BorderBottom(1).Padding(2).Row(row =>
        {
            // Logo on the left; an equal-width spacer on the right keeps the agency text optically
            // centered on the page. When no logo asset is bundled, both sides collapse to nothing.
            var logo = LogoBytes.Value;
            if (logo is not null)
                row.ConstantItem(40).AlignMiddle().Height(32).Image(logo).FitArea();
            else
                row.ConstantItem(40);

            row.RelativeItem().AlignMiddle().Column(col =>
            {
                col.Item().AlignCenter().Text("Republic of the Philippines").FontSize(7);
                col.Item().AlignCenter().Text("NATIONAL FOOD AUTHORITY").Bold().FontSize(9);
                if (!string.IsNullOrWhiteSpace(org?.Name))
                    col.Item().AlignCenter().Text(org!.Name).Bold().FontSize(8);
                if (!string.IsNullOrWhiteSpace(org?.Address))
                    col.Item().AlignCenter().Text(org!.Address).FontSize(7);
            });

            row.ConstantItem(40);
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
            row.RelativeItem().BorderRight(1).PaddingVertical(4).PaddingHorizontal(3).AlignCenter().AlignMiddle()
                .Text("DISBURSEMENT VOUCHER").Bold().FontSize(12);

            row.ConstantItem(150).Padding(3).AlignMiddle().Row(r =>
            {
                r.AutoItem().Text("DV No. :").FontSize(8);
                r.RelativeItem().PaddingLeft(2).AlignBottom().Text(dv.DvNumber).Bold().FontSize(8);
            });
        });
    }

    private void ComposeModeOfPayment(IContainer container)
    {
        container.BorderBottom(1).Row(row =>
        {
            row.ConstantItem(65).BorderRight(1).Padding(2).AlignMiddle().Text("Mode of\nPayment").FontSize(7);
            row.RelativeItem().Padding(2).AlignMiddle().Row(modes =>
            {
                CheckBox(modes, "MDS Check", IsMdsCheck);
                modes.ConstantItem(10);
                CheckBox(modes, "Commercial Check", IsCommercialCheck);
                modes.ConstantItem(10);
                CheckBox(modes, "ADA", IsAda);
                modes.ConstantItem(10);
                CheckBox(modes, "Others", IsOthers);
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
                row.ConstantItem(65).BorderRight(1).Padding(2).AlignMiddle().Text("Payee").FontSize(7);
                row.RelativeItem(3).BorderRight(1).Padding(2).AlignMiddle().Text(dv.Payee).Bold().FontSize(8);
                row.RelativeItem(2).BorderRight(1).Padding(2).Column(c =>
                {
                    c.Item().Text("TIN/Employee No.:").FontSize(7);
                    c.Item().Text(dv.TinNo ?? string.Empty).Bold().FontSize(7);
                });
                row.RelativeItem(2).Padding(2).Column(c =>
                {
                    c.Item().Text("ORS/BURS No.:").FontSize(7);
                    c.Item().Text(dv.BurNumber ?? string.Empty).Bold().FontSize(7);
                });
            });

            // Address
            col.Item().Row(row =>
            {
                row.ConstantItem(65).BorderRight(1).Padding(2).AlignMiddle().Text("Address").FontSize(7);
                row.RelativeItem().Padding(2).MinHeight(18).AlignMiddle().Text(dv.PayeeAddress ?? string.Empty).Bold().FontSize(8);
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
                row.RelativeItem(6).BorderRight(1).Padding(2).AlignCenter().Text("Particulars").Bold().FontSize(8);
                row.RelativeItem(2).BorderRight(1).Padding(2).AlignCenter().Text("Responsibility\nCenter").Bold().FontSize(7);
                row.RelativeItem(2).BorderRight(1).Padding(2).AlignCenter().Text("MFO/PAP").Bold().FontSize(7);
                row.RelativeItem(3).Padding(2).AlignCenter().Text("Amount").Bold().FontSize(8);
            });

            // Body row — compact height to fit on one page
            col.Item().BorderBottom(1).Row(row =>
            {
                row.RelativeItem(6).BorderRight(1).Padding(3).MinHeight(350).Text(dv.Particulars).FontSize(8);
                row.RelativeItem(2).BorderRight(1).Padding(2).AlignCenter().Text(responsibilityCenter ?? string.Empty).FontSize(7);
                row.RelativeItem(2).BorderRight(1).Padding(2);
                row.RelativeItem(3).Column(amt =>
                {
                    amt.Item().Padding(2).Row(a =>
                    {
                        a.ConstantItem(12).Text("₱").FontSize(8);
                        a.RelativeItem().AlignRight().Text(dv.Amount.ToString("N2")).FontSize(8);
                    });

                    // Configurable deductions (tax, withholding, fees). Each line prints its label —
                    // suffixed with the rate for percentage lines — and its computed peso amount. When
                    // there are no deductions the breakdown collapses and Amount Due equals the gross.
                    if (dv.Deductions.Count > 0)
                    {
                        amt.Item().PaddingHorizontal(2).PaddingTop(1).Text("Less: Withholding Tax").FontSize(7);
                        foreach (var d in dv.Deductions)
                            DeductionLine(amt, DeductionLabel(d), d.Amount);
                        amt.Item().PaddingHorizontal(2).PaddingTop(1).AlignRight().Width(70).LineHorizontal(0.4f);
                        amt.Item().PaddingHorizontal(2).PaddingTop(1).AlignRight()
                            .Text(dv.TotalDeductions.ToString("N2")).FontSize(7);
                    }
                });
            });

            // Amount Due row
            col.Item().Row(row =>
            {
                row.RelativeItem(10).BorderRight(1).Padding(2).AlignRight().AlignMiddle().Text("Amount Due").Bold().FontSize(9);
                row.RelativeItem(3).Padding(2).Row(a =>
                {
                    a.ConstantItem(12).Text("₱").Bold().FontSize(8);
                    a.RelativeItem().AlignRight().Text(dv.AmountDue.ToString("N2")).Bold().FontSize(8);
                });
            });
        });
    }

    // A. CERTIFIED — by the supervisor (Assistant Regional Manager)
    private void ComposeCertifiedBySupervisor(IContainer container)
    {
        container.BorderTop(1).Padding(2).Column(col =>
        {
            col.Item().Row(r =>
            {
                r.ConstantItem(14).Border(1).AlignCenter().Text("A").Bold().FontSize(8);
                r.RelativeItem().PaddingLeft(3).AlignMiddle()
                    .Text("Certified: Expenses/Cash Advance necessary, lawful and incurred under my direct supervision.")
                    .FontSize(7);
            });
            col.Item().PaddingTop(30).AlignCenter().Column(c =>
            {
                c.Item().AlignCenter().Text((SectionAName ?? string.Empty).ToUpperInvariant()).Bold().Underline().FontSize(8);
                c.Item().AlignCenter().Text(SectionADesignation ?? string.Empty).FontSize(7);
                c.Item().PaddingTop(1).AlignCenter().Text("Printed Name, Designation and Signature of Supervisor").FontSize(6);
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
                c.ConstantColumn(50);   // B label column
                c.RelativeColumn();     // B value column
                c.ConstantColumn(50);   // C label column
                c.RelativeColumn();     // C value column
            });

            // ── Header band: B (Certified + checkboxes) | C (Approved for Payment) ──
            table.Cell().ColumnSpan(2).Border(0.5f).Padding(2).Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.ConstantItem(14).Border(1).AlignCenter().AlignMiddle().Text("B").Bold().FontSize(8);
                    r.RelativeItem().PaddingLeft(3).AlignMiddle().Text("Certified:").Bold().FontSize(8);
                });
                col.Item().PaddingTop(2).Column(c =>
                {
                    // First row: both items side by side
                    c.Item().PaddingTop(2).Row(r =>
                    {
                        r.ConstantItem(16).Border(1).Height(10);
                        r.AutoItem().PaddingLeft(4).AlignMiddle().Text("Cash available").FontSize(7);
                        r.ConstantItem(12);
                        r.ConstantItem(16).Border(1).Height(10);
                        r.RelativeItem().PaddingLeft(4).AlignMiddle().Text("Subject to Authority to Debit Account").FontSize(7);
                    });
                    CertCheck(c, "Supporting documents complete and proper");
                });
            });

            table.Cell().ColumnSpan(2).Border(0.5f).Padding(2).Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.ConstantItem(14).Border(1).AlignCenter().AlignMiddle().Text("C").Bold().FontSize(8);
                    r.RelativeItem().PaddingLeft(3).AlignMiddle().Text("Approved for Payment").Bold().FontSize(8);
                });
                col.Item().MinHeight(18);
            });

            // ── Signatory grid (B and C share aligned rows) ──
            SignatoryRows(table,
                SectionBName?.ToUpperInvariant(),
                SectionBDesignation,
                "Head, Accounting Unit/Authorized Representative",
                SectionCName?.ToUpperInvariant(),
                SectionCDesignation,
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
        table.Cell().Border(0.5f).MinHeight(20);
        LabelCell(table, "Signature");
        table.Cell().Border(0.5f).MinHeight(20);

        // Printed Name
        LabelCell(table, "Printed Name");
        ValueCell(table, nameB, bold: true, size: 8);
        LabelCell(table, "Printed Name");
        ValueCell(table, nameC, bold: true, size: 8);

        // Position (designation)
        LabelCell(table, "Position");
        ValueCell(table, designationB, bold: false, size: 7);
        LabelCell(table, "Position");
        ValueCell(table, designationC, bold: false, size: 7);

        // Role (unlabeled continuation of Position)
        table.Cell().Border(0.5f);
        ValueCell(table, roleB, bold: false, size: 7);
        table.Cell().Border(0.5f);
        ValueCell(table, roleC, bold: false, size: 7);

        // Date
        LabelCell(table, "Date");
        table.Cell().Border(0.5f).MinHeight(10);
        LabelCell(table, "Date");
        table.Cell().Border(0.5f).MinHeight(10);
    }

    private static void LabelCell(TableDescriptor table, string text) =>
        table.Cell().Border(0.5f).Padding(2).AlignMiddle().Text(text).FontSize(6);

    private static void ValueCell(TableDescriptor table, string? text, bool bold, float size) =>
        table.Cell().Border(0.5f).Padding(2).AlignCenter().AlignMiddle().Text(t =>
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
                c.ConstantColumn(60);   // labels (Check/ADA No., Signature)
                c.RelativeColumn(2);    // value box
                c.ConstantColumn(35);   // "Date :" label
                c.RelativeColumn(3);    // Bank Name & Account Number / Printed Name
                c.RelativeColumn(2);    // JEV No. / Date value
            });

            // Header row: D. Receipt of Payment | JEV No.
            table.Cell().ColumnSpan(4).Padding(2).Row(r =>
            {
                r.ConstantItem(14).Border(1).AlignCenter().AlignMiddle().Text("D").Bold().FontSize(8);
                r.RelativeItem().PaddingLeft(3).AlignMiddle().Text("Receipt of Payment").Bold().FontSize(8);
            });
            table.Cell().BorderLeft(0.5f).Padding(2).Text("JEV No.").FontSize(7);

            // Row 1: Check/ADA No. | value | Date | Bank Name & Account Number | value
            table.Cell().Border(0.5f).Padding(2).Text("Check/ADA No. :").FontSize(7);
            table.Cell().Border(0.5f).MinHeight(11);
            table.Cell().Border(0.5f).Padding(2).Text("Date :").FontSize(7);
            table.Cell().Border(0.5f).Padding(2).Text("Bank Name & Account Number:").FontSize(7);
            table.Cell().Border(0.5f).MinHeight(11);

            // Row 2: Signature | value | Date | Printed Name | Date value
            table.Cell().Border(0.5f).Padding(2).Text("Signature :").FontSize(7);
            table.Cell().Border(0.5f).MinHeight(11);
            table.Cell().Border(0.5f).Padding(2).Text("Date :").FontSize(7);
            table.Cell().Border(0.5f).Padding(2).Text("Printed Name:").FontSize(7);
            table.Cell().Border(0.5f).Padding(2).Text("Date").FontSize(7);

            // Footer spanning the full width — boxed to the same height as the signature cells
            // table.Cell().ColumnSpan(5).Border(0.5f).MinHeight(11).Padding(2)
            //     .Text("Official Receipt No. & Date/Other Documents").FontSize(7);
            table.Cell().ColumnSpan(5).Border(0.5f).MinHeight(11).ExtendVertical().Padding(2)
            .Text("Official Receipt No. & Date/Other Documents").FontSize(7);
        });
    }

    // ── small helpers ──────────────────────────────────────────────────────────

    private static void CheckBox(RowDescriptor row, string label, bool on)
    {
        row.AutoItem().AlignMiddle().Border(1).Width(8).Height(8).AlignCenter().AlignMiddle()
            .Text(on ? "X" : string.Empty).FontSize(6);
        row.AutoItem().PaddingLeft(2).AlignMiddle().Text(label).FontSize(7);
    }

    private static void CertCheck(ColumnDescriptor col, string text)
    {
        col.Item().PaddingTop(2).Row(r =>
        {
            r.ConstantItem(16).Border(1).Height(10);
            r.RelativeItem().PaddingLeft(4).AlignMiddle().Text(text).FontSize(7);
        });
    }

    // A deduction breakdown line: label on the left, computed peso amount right-aligned.
    private static void DeductionLine(ColumnDescriptor col, string label, decimal amount)
    {
        col.Item().PaddingHorizontal(2).PaddingTop(1).Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(7);
            r.ConstantItem(60).AlignRight().Text(amount.ToString("N2")).FontSize(7);
        });
    }

    // Percentage lines: show "5% Name" only when the name doesn't already contain the rate.
    // Fixed-amount lines: show the bare name.
    private static string DeductionLabel(DvDeductionDto d)
    {
        if (d.Type != DvDeductionType.Percentage)
            return d.Name;

        var rateLabel = $"{d.Value * 100:0.##}%";
        return d.Name.StartsWith(rateLabel, StringComparison.OrdinalIgnoreCase)
            ? d.Name
            : $"{rateLabel} {d.Name}";
    }
}
