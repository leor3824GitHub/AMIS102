using System.Reflection;
using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Finance.PrintDisbursementVoucher;

/// <summary>
/// Disbursement Voucher (DV) — NFA form layout. A single-page bordered government form with the
/// agency header, Mode of Payment / Payee / Responsibility Center band, Particulars &amp; Amount table,
/// supporting-documents list, and the Certified (A) / Approved (B) / Received Payment (C) boxes.
/// </summary>
internal sealed class DisbursementVoucherPdfDocument(
    DisbursementVoucherDto  dv,
    OrganizationProfileDto? org,
    string                  paperSize   = "a4",
    string                  orientation = "portrait",
    float                   marginMm    = 14f) : IDocument
{
    private bool IsCheck => dv.ModeOfPayment.Contains("Check", StringComparison.OrdinalIgnoreCase);
    private bool IsCash => dv.ModeOfPayment.Equals("Cash", StringComparison.OrdinalIgnoreCase);
    private bool IsOthers => !IsCheck && !IsCash;

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
                col.Item().Element(ComposePayeeBand);
                col.Item().Element(ComposeParticulars);
                col.Item().Element(ComposeSupportingDocs);
                col.Item().Element(ComposeCertifyApprove);
                col.Item().Element(ComposeReceivedPayment);
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
            row.RelativeItem(3).BorderRight(1).PaddingVertical(6).PaddingHorizontal(6).AlignMiddle()
                .Text("DISBURSEMENT VOUCHER").Bold().FontSize(15);

            row.RelativeItem(1).BorderRight(1).Padding(3).Column(c =>
            {
                c.Item().Text("No.").FontSize(8);
                c.Item().PaddingTop(6).AlignCenter().Text(dv.DvNumber).Bold().FontSize(9);
            });
            row.RelativeItem(1).Padding(3).Column(c =>
            {
                c.Item().Text("Date").FontSize(8);
                c.Item().PaddingTop(6).AlignCenter().Text(dv.DvDate.ToString("MMMM dd, yyyy")).FontSize(9);
            });
        });
    }

    private void ComposePayeeBand(IContainer container)
    {
        container.BorderBottom(1).Row(row =>
        {
            // Left column: Mode of Payment, Payee, Address
            row.RelativeItem(3).BorderRight(1).Column(left =>
            {
                left.Item().BorderBottom(1).Padding(4).Row(r =>
                {
                    r.ConstantItem(58).AlignMiddle().Text("Mode of\nPayment").FontSize(8);
                    r.RelativeItem().AlignMiddle().Row(modes =>
                    {
                        CheckBox(modes, "Check", IsCheck);
                        modes.ConstantItem(14);
                        CheckBox(modes, "Cash", IsCash);
                        modes.ConstantItem(14);
                        CheckBox(modes, "Others", IsOthers);
                    });
                });

                left.Item().BorderBottom(1).Padding(4).Row(r =>
                {
                    r.ConstantItem(58).AlignMiddle().Text("Payee").FontSize(8);
                    r.RelativeItem().AlignMiddle().Text(dv.Payee).Bold().FontSize(10);
                });

                left.Item().Padding(4).MinHeight(38).Row(r =>
                {
                    r.ConstantItem(58).Text("Address").FontSize(8);
                    r.RelativeItem().Text(dv.PayeeAddress ?? string.Empty).Bold().FontSize(9);
                });
            });

            // Right column: TIN / Employee No, then Responsibility Center (Office/Unit/Project, Code)
            row.RelativeItem(2).Column(right =>
            {
                right.Item().BorderBottom(1).Padding(4).MinHeight(48).Column(c =>
                {
                    c.Item().Text("TIN/ Employee No.").FontSize(8);
                    c.Item().PaddingTop(4).AlignCenter().Text(dv.TinNo ?? string.Empty).Bold().FontSize(9);
                });

                right.Item().BorderBottom(1).Padding(2).AlignCenter().Text("Responsibility Center").Bold().FontSize(9);

                right.Item().Row(r =>
                {
                    r.RelativeItem().BorderRight(1).Padding(3).MinHeight(40).Column(c =>
                    {
                        c.Item().Text("Office/ Unit/ Project").FontSize(7);
                    });
                    r.RelativeItem().Padding(3).MinHeight(40).Column(c =>
                    {
                        c.Item().Text("Code").FontSize(7);
                        c.Item().PaddingTop(2).Text(dv.FundCluster ?? string.Empty).FontSize(8);
                    });
                });
            });
        });
    }

    private void ComposeParticulars(IContainer container)
    {
        container.Column(col =>
        {
            // Header row
            col.Item().BorderBottom(1).Row(row =>
            {
                row.RelativeItem(7).BorderRight(1).Padding(3).AlignCenter().Text("PARTICULARS").Bold().FontSize(10);
                row.RelativeItem(3).Padding(3).AlignCenter().Text("AMOUNT").Bold().FontSize(10);
            });

            // Body row
            col.Item().BorderBottom(1).Row(row =>
            {
                row.RelativeItem(7).BorderRight(1).Padding(6).MinHeight(150).Text(dv.Particulars).FontSize(9);
                row.RelativeItem(3).Padding(6).Row(a =>
                {
                    a.ConstantItem(26).Text("Php").FontSize(9);
                    a.RelativeItem().AlignRight().Text(dv.Amount.ToString("N2")).FontSize(9);
                });
            });

            // Total row
            col.Item().Row(row =>
            {
                row.RelativeItem(7).BorderRight(1).Padding(4).AlignRight().Text("TOTAL").Bold().FontSize(10);
                row.RelativeItem(3).Padding(4).Row(a =>
                {
                    a.ConstantItem(26).Text("Php").Bold().FontSize(9);
                    a.RelativeItem().AlignRight().Text(dv.Amount.ToString("N2")).Bold().FontSize(9);
                });
            });
        });
    }

    private void ComposeSupportingDocs(IContainer container)
    {
        container.BorderTop(1).Padding(6).MinHeight(60).Column(col =>
        {
            col.Item().Text("List of Supporting Documents").SemiBold().FontSize(9);
            if (!string.IsNullOrWhiteSpace(dv.PurchaseOrderNumber))
                col.Item().PaddingLeft(20).Text($"Purchase Order — {dv.PurchaseOrderNumber}").FontSize(8);
            if (!string.IsNullOrWhiteSpace(dv.Remarks))
                col.Item().PaddingLeft(20).Text(dv.Remarks).FontSize(8);
        });
    }

    private void ComposeCertifyApprove(IContainer container)
    {
        container.BorderTop(1).Row(row =>
        {
            // A. CERTIFIED
            row.RelativeItem().BorderRight(1).Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.ConstantItem(16).Border(1).AlignCenter().Text("A").Bold().FontSize(9);
                    r.RelativeItem().PaddingLeft(4).AlignMiddle().Text("CERTIFIED").Bold().FontSize(9);
                });
                col.Item().PaddingTop(4).PaddingLeft(6).Column(c =>
                {
                    CertLine(c, "Allotment obligated for the purpose as indicated above.");
                    CertLine(c, "Supporting documents complete.");
                    CertLine(c, "Funds Available.");
                });
                SignatoryBlock(col,
                    org?.AccountantName,
                    org?.AccountantDesignation ?? "Head, Accounting Unit/ Authorized Representative");
            });

            // B. APPROVED
            row.RelativeItem().Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.ConstantItem(16).Border(1).AlignCenter().Text("B").Bold().FontSize(9);
                    r.RelativeItem().PaddingLeft(4).AlignMiddle().Text("APPROVED").Bold().FontSize(9);
                });
                col.Item().MinHeight(54);
                SignatoryBlock(col,
                    org?.ApprovingOfficialName,
                    org?.ApprovingOfficialDesignation ?? "Agency Head/ Authorized Representative");
            });
        });
    }

    private static void SignatoryBlock(ColumnDescriptor col, string? printedName, string designation)
    {
        col.Item().PaddingTop(10).PaddingHorizontal(6).Column(c =>
        {
            c.Item().Row(r =>
            {
                r.ConstantItem(54).Text("Signature").FontSize(7);
                r.RelativeItem().AlignBottom().LineHorizontal(0.5f);
            });
            c.Item().PaddingTop(2).Row(r =>
            {
                r.ConstantItem(54).Text("Printed Name").FontSize(7);
                r.RelativeItem().AlignCenter().Text(printedName ?? string.Empty).Bold().FontSize(9);
            });
            c.Item().Row(r =>
            {
                r.ConstantItem(54).Text("Position").FontSize(7);
                r.RelativeItem().AlignCenter().Text(designation).FontSize(8);
            });
            c.Item().PaddingTop(2).Row(r =>
            {
                r.ConstantItem(54).Text("Date").FontSize(7);
                r.RelativeItem().AlignBottom().LineHorizontal(0.5f);
            });
        });
    }

    private void ComposeReceivedPayment(IContainer container)
    {
        container.BorderTop(1).Padding(6).Column(col =>
        {
            col.Item().Row(r =>
            {
                r.ConstantItem(16).Border(1).AlignCenter().Text("C").Bold().FontSize(9);
                r.RelativeItem().PaddingLeft(4).AlignMiddle().Text("RECEIVED PAYMENT").Bold().FontSize(9);
            });

            col.Item().PaddingTop(8).Row(r =>
            {
                Field(r, "Check No.");
                r.ConstantItem(16);
                Field(r, "Signature");
                r.ConstantItem(16);
                Field(r, "JEV No.");
            });
            col.Item().PaddingTop(8).Row(r =>
            {
                Field(r, "Date");
                r.ConstantItem(16);
                Field(r, "Printed Name");
                r.ConstantItem(16);
                Field(r, "Date");
            });
            col.Item().PaddingTop(8).Row(r =>
            {
                Field(r, "Bank Name");
                r.ConstantItem(16);
                Field(r, "Date");
                r.ConstantItem(16);
                r.RelativeItem();
            });
        });
    }

    // ── small helpers ──────────────────────────────────────────────────────────

    private static void CheckBox(RowDescriptor row, string label, bool on)
    {
        row.AutoItem().AlignMiddle().Border(1).Width(10).Height(10).AlignCenter().AlignMiddle()
            .Text(on ? "X" : string.Empty).FontSize(8);
        row.AutoItem().PaddingLeft(3).AlignMiddle().Text(label).FontSize(8);
    }

    private static void CertLine(ColumnDescriptor col, string text)
    {
        col.Item().PaddingTop(3).Row(r =>
        {
            r.ConstantItem(12).Border(1).Height(10);
            r.RelativeItem().PaddingLeft(4).Text(text).FontSize(7);
        });
    }

    private static void Field(RowDescriptor row, string label)
    {
        row.RelativeItem().Column(c =>
        {
            c.Item().LineHorizontal(0.5f);
            c.Item().Text(label).FontSize(7);
        });
    }
}
