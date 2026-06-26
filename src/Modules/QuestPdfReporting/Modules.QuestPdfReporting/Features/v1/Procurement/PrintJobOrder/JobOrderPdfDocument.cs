using System.Globalization;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Procurement.PrintJobOrder;

/// <summary>
/// Job Order (JO) — NFA government form layout for works (renovation / repair / fabrication). A bordered
/// form with the agency header, JOB ORDER title, supplier + JO/Job-Request bands, Particulars table,
/// Total Amount in Words, penalty &amp; performance-bond terms, the Authorized Official / Supplier conforme
/// signatures, the Funds Available (BUR) band, and the Inspection / Acceptance boxes.
/// </summary>
internal sealed class JobOrderPdfDocument(
    JobOrderDto jo,
    OrganizationProfileDto? org,
    string paperSize = "a4",
    string orientation = "portrait",
    float marginMm = 10f) : IDocument
{
    private static readonly CultureInfo Nf = CultureInfo.InvariantCulture;

    private string AuthorizedOfficialName => (jo.IssuedByName ?? org?.ApprovingOfficialName ?? string.Empty).ToUpperInvariant();
    private string AuthorizedOfficialDesignation => jo.IssuedByDesignation ?? org?.ApprovingOfficialDesignation ?? "Authorized NFA Official";
    private string AccountantName => (jo.FundsAvailableCertifiedByName ?? org?.AccountantName ?? string.Empty).ToUpperInvariant();
    private string AccountantDesignation => jo.FundsAvailableCertifiedByDesignation ?? org?.AccountantDesignation ?? "Authorized Officer / Regional / Provincial Accountant";

    // Inspector — assigned (and named) on the JO at creation; printed in the INSPECTION box from the start.
    private string InspectorName => (jo.InspectorName ?? string.Empty).ToUpperInvariant();
    // Supply Officer — sourced from the Organization Profile's Property/Supply Custodian, not the JO.
    private string SupplyOfficerName => (org?.PropertyCustodianName ?? string.Empty).ToUpperInvariant();
    private string SupplyOfficerDesignation => org?.PropertyCustodianDesignation ?? "PMSDS-GSD / F.O. Supply Officer";

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"JO — {jo.JoNumber}",
        Author = org?.Name ?? string.Empty
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            QuestPdfPaperSize.Apply(page, paperSize, orientation, marginMm);
            page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));
            page.Content().Border(1).Column(col =>
            {
                col.Item().Element(ComposeHeader);
                col.Item().Element(ComposeInfoBands);
                col.Item().Element(ComposeReserveNotice);
                col.Item().Element(ComposeParticulars);
                col.Item().Element(ComposeTotalInWords);
                col.Item().Element(ComposePenaltyTerms);
                col.Item().Element(ComposeSignatures);
                col.Item().Element(ComposePerformanceBond);
                col.Item().Element(ComposeFundsAvailable);
                col.Item().Element(ComposeInspectionAcceptance);
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
        container.BorderBottom(1).Padding(3).Column(col =>
        {
            col.Item().AlignCenter().Text("NATIONAL FOOD AUTHORITY").Bold().FontSize(11);
            col.Item().AlignCenter().Text("JOB ORDER").Bold().FontSize(13);
            if (!string.IsNullOrWhiteSpace(org?.Name))
                col.Item().AlignCenter().Text(org!.Name).FontSize(8);
            if (!string.IsNullOrWhiteSpace(org?.Address))
                col.Item().AlignCenter().Text(org!.Address).FontSize(7);
        });
    }

    // Two-column band: supplier details (left) and JO/Job-Request references (right).
    private void ComposeInfoBands(IContainer container)
    {
        container.BorderBottom(1).Row(row =>
        {
            row.RelativeItem().BorderRight(1).Padding(3).Column(c =>
            {
                LabelValue(c, "Supplier's Name", jo.SupplierName);
                LabelValue(c, "Address", jo.SupplierAddress);
                LabelValue(c, "TIN", jo.SupplierTin ?? string.Empty);
                LabelValue(c, "Mode of Procurement", FormatMode(jo.ModeOfProcurement));
            });
            row.RelativeItem().Padding(3).Column(c =>
            {
                LabelValue(c, "Job Order No.", jo.JoNumber);
                LabelValue(c, "Date Prepared", jo.JoDate.ToString("MM/dd/yyyy", Nf));
                LabelValue(c, "Job Request No.", jo.JobRequestNo ?? string.Empty);
                LabelValue(c, "Requisitioning Office/Dept.", jo.RequisitioningOffice ?? string.Empty);
            });
        });
    }

    private void ComposeReserveNotice(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().BorderBottom(1).Row(row =>
            {
                row.RelativeItem().BorderRight(1).Padding(3).Column(c =>
                {
                    LabelValue(c, "Place of delivery", jo.PlaceOfDelivery);
                    LabelValue(c, "Date of delivery", jo.DateOfDelivery?.ToString("MM/dd/yyyy", Nf) ?? string.Empty);
                });
                row.RelativeItem().Padding(3).Column(c =>
                {
                    LabelValue(c, "Delivery Term", jo.DeliveryTerm);
                    LabelValue(c, "Payment Term", jo.PaymentTerm);
                });
            });

            col.Item().BorderBottom(1).Padding(2).AlignCenter()
                .Text("We reserve the right to reject and return at your expense any material not specified in this order.")
                .Italic().FontSize(7);
        });
    }

    private void ComposeParticulars(IContainer container)
    {
        container.Column(col =>
        {
            // Header row — PARTICULARS | Amount
            col.Item().BorderBottom(1).Row(row =>
            {
                row.RelativeItem().BorderRight(1).Padding(2).AlignCenter().Text("PARTICULARS").Bold().FontSize(9);
                row.ConstantItem(110).Padding(2).AlignCenter().Text("Amount").Bold().FontSize(9);
            });

            // Intro line
            col.Item().Row(row =>
            {
                row.RelativeItem().BorderRight(1).PaddingHorizontal(3).PaddingTop(2)
                    .Text("Renovation / Repair / Fabrication of the following is required.").Italic().FontSize(8);
                row.ConstantItem(110);
            });

            // Line items
            foreach (var li in jo.LineItems.OrderBy(x => x.ItemNo))
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().BorderRight(1).PaddingHorizontal(3).PaddingVertical(1).Text(t =>
                    {
                        t.Span($"{li.ItemNo}. ").FontSize(8);
                        t.Span(li.Description).FontSize(8);
                        var qtyUnit = $"   ({li.Quantity.ToString("N2", Nf)} {li.Unit ?? string.Empty} @ {li.UnitCost.ToString("N2", Nf)})";
                        t.Span(qtyUnit).FontSize(7).Light();
                    });
                    row.ConstantItem(110).PaddingHorizontal(3).PaddingVertical(1).AlignRight()
                        .Text(li.Amount.ToString("N2", Nf)).FontSize(8);
                });
            }

            // Filler to give the particulars block a minimum height like the paper form.
            col.Item().Row(row =>
            {
                row.RelativeItem().BorderRight(1).MinHeight(120);
                row.ConstantItem(110);
            });

            // Total row
            col.Item().BorderTop(1).Row(row =>
            {
                row.RelativeItem().BorderRight(1).Padding(2).AlignRight().Text("Total").Bold().FontSize(9);
                row.ConstantItem(110).Padding(2).Row(a =>
                {
                    a.ConstantItem(10).Text("₱").Bold().FontSize(8);
                    a.RelativeItem().AlignRight().Text(jo.TotalAmount.ToString("N2", Nf)).Bold().FontSize(9);
                });
            });
        });
    }

    private void ComposeTotalInWords(IContainer container)
    {
        container.BorderTop(1).BorderBottom(1).Padding(3).Row(row =>
        {
            row.AutoItem().Text("Total Amount in Words: ").Bold().FontSize(8);
            row.RelativeItem().Text(jo.TotalAmountInWords).Italic().FontSize(8);
        });
    }

    private static void ComposePenaltyTerms(IContainer container)
    {
        container.Padding(3).Column(col =>
        {
            col.Spacing(3);
            col.Item().Text(
                "In case of failure to make the full delivery within the time specified above, a penalty of one-tenth (1/10) " +
                "of one percent (1%) of the undelivered amount of JO for every day of delay shall be imposed. In addition, " +
                "the performance bond shall be automatically forfeited.").FontSize(7);
            col.Item().Text(
                "Jobs / services done which were rejected for failure to conform to specifications shall be replaced within 7 " +
                "government working days from date of rejection but subject to penalty of 1/10 of 1% per day of delay based on " +
                "value of items rejected, after which Supplier shall also be considered in default. Moreover, NFA shall have the " +
                "option to have the job / service done by other Suppliers and any price difference shall be charged to Supplier in default.").FontSize(7);
            col.Item().Text(
                "In addition, Supplier shall be liable to pay 15% of the contract price should NFA resort to court action to enforce " +
                "the provisions of this agreement. Supplier authorizes NFA to deduct the penalty / liquidated damages from any of " +
                "its receivables from NFA.").FontSize(7);
        });
    }

    private void ComposeSignatures(IContainer container)
    {
        container.PaddingTop(6).PaddingBottom(4).Row(row =>
        {
            // Authorized NFA Official (left)
            row.RelativeItem().Padding(3).Column(c =>
            {
                c.Item().Text("Very truly yours,").FontSize(8);
                c.Item().PaddingTop(24).AlignCenter().Text(AuthorizedOfficialName).Bold().Underline().FontSize(8);
                c.Item().AlignCenter().Text("Signature over printed name of").FontSize(7);
                c.Item().AlignCenter().Text(AuthorizedOfficialDesignation).FontSize(7);
            });

            // Conforme — Supplier (right)
            row.RelativeItem().Padding(3).Column(c =>
            {
                c.Item().Text("Conforme:").FontSize(8);
                c.Item().PaddingTop(24).AlignCenter().Text((jo.SupplierName ?? string.Empty).ToUpperInvariant()).Bold().Underline().FontSize(8);
                c.Item().AlignCenter().Text("Signature over Printed Name of Supplier").FontSize(7);
                c.Item().PaddingTop(8).AlignCenter().Text("_______________________").FontSize(7);
                c.Item().AlignCenter().Text("Date").FontSize(7);
            });
        });
    }

    private static void ComposePerformanceBond(IContainer container)
    {
        container.BorderTop(1).Padding(3).Column(col =>
        {
            col.Spacing(3);
            col.Item().Text(
                "PERFORMANCE BOND - the Supplier shall within five (5) banking days from the date of receipt of the approved JO " +
                "post a Performance Bond in any of the following form with amount equivalent to: 5% of the total contract price if " +
                "in the form of cash, certified check, cashier's check, manager's check, bank draft or irrevocable Letter of Credit; " +
                "10% of the total contract price if bank guarantee; 30% of the total contract price if surety bond, issued in favor of " +
                "the NATIONAL FOOD AUTHORITY to assure faithful compliance with the terms and conditions of the Job Order and to answer " +
                "for any damages or penalties that may be suffered by the NATIONAL FOOD AUTHORITY by reason of any violation/s in the " +
                "terms and conditions of the Job Order.").FontSize(7);
            col.Item().Text(
                "The cash, certified check, cashier's/manager's check, bank draft/guarantee should be confirmed by a reputable local " +
                "bank. The irrevocable LC should be issued by a reputable commercial bank with validity of 60 days after date of issue. " +
                "The surety bond should be callable upon demand and issued by GSIS.").FontSize(7);
            col.Item().Text(
                "Performance bond is not needed for COD or advance delivery or if delivery was made within five (5) banking days after " +
                "receipt of approved JO.").Italic().FontSize(7);
        });
    }

    private void ComposeFundsAvailable(IContainer container)
    {
        container.BorderTop(1).Padding(3).Column(col =>
        {
            col.Item().Text(t =>
            {
                t.Span("Funds Available:  BUR No. ").FontSize(8);
                t.Span(string.IsNullOrWhiteSpace(jo.OursBursNumber) ? "_______________" : jo.OursBursNumber!).Bold().FontSize(8);
            });
            col.Item().PaddingTop(14).AlignCenter().Text(AccountantName).Bold().Underline().FontSize(8);
            col.Item().AlignCenter().Text(AccountantDesignation).FontSize(7);
        });
    }

    // Bottom INSPECTION (left) / ACCEPTANCE (right) boxes.
    private void ComposeInspectionAcceptance(IContainer container)
    {
        container.BorderTop(1).Row(row =>
        {
            // INSPECTION
            row.RelativeItem().BorderRight(1).Padding(3).Column(c =>
            {
                c.Item().AlignCenter().Text("INSPECTION").Bold().FontSize(9);
                c.Item().Text($"Invoice No.: {jo.InspectionInvoiceNo ?? string.Empty}").FontSize(7);
                c.Item().Text($"Date: {jo.InspectionInvoiceDate?.ToString("MM/dd/yyyy", Nf) ?? string.Empty}").FontSize(7);
                c.Item().Text($"Date Inspected: {jo.DateInspected?.ToString("MM/dd/yyyy", Nf) ?? string.Empty}").FontSize(7);
                CheckLine(c, "Findings and recommendations", !string.IsNullOrWhiteSpace(jo.InspectionFindings));
                if (!string.IsNullOrWhiteSpace(jo.InspectionFindings))
                    c.Item().PaddingLeft(14).Text(jo.InspectionFindings).Italic().FontSize(7);
                CheckLine(c, "Inspected, verified and found in order as to quantity and specifications.", jo.FoundInOrder);
                c.Item().PaddingTop(16).AlignCenter().Text(InspectorName).Bold().Underline().FontSize(8);
                c.Item().AlignCenter().Text("Signature over printed name of").FontSize(7);
                c.Item().AlignCenter().Text("C.O. / F.O. Inspector").FontSize(7);
            });

            // ACCEPTANCE
            row.RelativeItem().Padding(3).Column(c =>
            {
                c.Item().AlignCenter().Text("ACCEPTANCE").Bold().FontSize(9);
                c.Item().Text($"Invoice No.: {jo.AcceptanceInvoiceNo ?? string.Empty}").FontSize(7);
                c.Item().Text($"Date Received: {jo.DateReceived?.ToString("MM/dd/yyyy", Nf) ?? string.Empty}").FontSize(7);
                CheckLine(c, "Complete delivery", jo.AcceptedOnUtc is not null && jo.IsCompleteDelivery);
                CheckLine(c, "Partial delivery (pls. specify quantity)", jo.AcceptedOnUtc is not null && !jo.IsCompleteDelivery);
                if (jo.AcceptedOnUtc is not null && !jo.IsCompleteDelivery && !string.IsNullOrWhiteSpace(jo.PartialDeliveryNote))
                    c.Item().PaddingLeft(14).Text(jo.PartialDeliveryNote).Italic().FontSize(7);
                c.Item().PaddingTop(16).AlignCenter().Text(SupplyOfficerName).Bold().Underline().FontSize(8);
                c.Item().AlignCenter().Text("Signature over printed name of").FontSize(7);
                c.Item().AlignCenter().Text(SupplyOfficerDesignation).FontSize(7);
            });
        });
    }

    // ── small helpers ──────────────────────────────────────────────────────────

    private static void LabelValue(ColumnDescriptor col, string label, string value)
    {
        col.Item().PaddingVertical(1).Row(r =>
        {
            r.ConstantItem(110).Text($"{label} :").FontSize(7);
            r.RelativeItem().Text(value).Bold().FontSize(8);
        });
    }

    private static void CheckLine(ColumnDescriptor col, string label, bool on)
    {
        col.Item().PaddingTop(2).Row(r =>
        {
            r.ConstantItem(10).Border(1).Height(10).AlignCenter().AlignMiddle().Text(on ? "X" : string.Empty).FontSize(7);
            r.RelativeItem().PaddingLeft(3).AlignMiddle().Text(label).FontSize(7);
        });
    }

    private static string FormatMode(ModeOfProcurement mode) => mode switch
    {
        ModeOfProcurement.DirectAcquisition => "Direct Acquisition",
        ModeOfProcurement.SmallValueProcurement => "Small Value Procurement",
        ModeOfProcurement.PublicBidding => "Public Bidding",
        ModeOfProcurement.NegotiatedProcurement => "Negotiated Procurement",
        ModeOfProcurement.ShoppingA => "Shopping (A)",
        ModeOfProcurement.ShoppingB => "Shopping (B)",
        _ => mode.ToString()
    };
}
