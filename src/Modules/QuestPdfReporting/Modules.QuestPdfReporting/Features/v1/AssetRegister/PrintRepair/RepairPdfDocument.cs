using AMIS.Modules.AssetRegister.Contracts.v1.Repairs;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintRepair;

/// <summary>
/// Request for Pre/Post Repair Inspection (RPRI) — NFA Exhibit 6. Mandatory pre-repair inspection,
/// optional procurement/finance references (outsourced repairs), custodian acceptance.
/// </summary>
internal sealed class RepairPdfDocument(
    PropertyRepairDto repair,
    OrganizationProfileDto? org,
    string paperSize = "longbond",
    string orientation = "portrait",
    float marginMm = 14f) : IDocument
{
    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"RPRI — {repair.RpriNo}",
        Author = org?.Name ?? string.Empty
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            QuestPdfPaperSize.Apply(page, paperSize, orientation, marginMm);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));
            page.Content().Element(ComposeBody);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeBody(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text("EXHIBIT 6").Italic().FontSize(9);

            col.Item().PaddingTop(2).AlignCenter().Text("Republic of the Philippines").FontSize(9);
            col.Item().AlignCenter().Text("NATIONAL FOOD AUTHORITY").Bold().FontSize(10);
            if (org is not null)
            {
                if (!string.IsNullOrWhiteSpace(org.Name))
                    col.Item().AlignCenter().Text(org.Name).Bold().FontSize(10);
                if (!string.IsNullOrWhiteSpace(org.Address))
                    col.Item().AlignCenter().Text(org.Address).FontSize(8);
            }

            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem(3).AlignCenter().Text("REQUEST FOR PRE/POST REPAIR INSPECTION").Bold().FontSize(12);
                row.RelativeItem(1).Border(1).Padding(3).Column(c =>
                {
                    c.Item().Text("RPRI NUMBER").SemiBold().FontSize(8);
                    c.Item().Text(repair.RpriNo).Bold().FontSize(9);
                    c.Item().PaddingTop(2).Text("STATUS").SemiBold().FontSize(8);
                    c.Item().Text(DisplayStatus(repair.Status)).FontSize(8);
                });
            });

            col.Item().PaddingTop(6).Element(ComposeDescriptionOfProperty);
            col.Item().PaddingTop(6).Element(ComposeDefects);
            col.Item().PaddingTop(6).Element(ComposePreInspection);
            col.Item().PaddingTop(6).Element(ComposePostInspection);
            col.Item().PaddingTop(6).Element(ComposeProcurementRefs);
            col.Item().PaddingTop(6).Element(ComposeAcceptance);
        });
    }

    private static string DisplayStatus(string status) => status switch
    {
        RepairStatusValues.PreInspected => "Pre-Inspected",
        RepairStatusValues.PostInspected => "Post-Inspected",
        _ => status
    };

    private static IContainer SectionTitle(IContainer container, string title)
    {
        container.Background(Colors.Grey.Lighten3).Border(0.75f).Padding(3).Text(title).Bold().FontSize(9);
        return container;
    }

    private void ComposeDescriptionOfProperty(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionTitle(c, "DESCRIPTION OF PROPERTY"));
            col.Item().Border(0.75f).Padding(4).Column(inner =>
            {
                inner.Item().Row(r =>
                {
                    Field(r.RelativeItem(), "Property No.", repair.PropertyNo);
                    Field(r.RelativeItem(), "Acquisition Date", repair.AcquisitionDate.ToString("MM/dd/yyyy"));
                    Field(r.RelativeItem(), "Acquisition Cost", repair.AcquisitionCost.ToString("N2"));
                });
                inner.Item().PaddingTop(3).Row(r =>
                {
                    Field(r.RelativeItem(2), "Description", repair.Description);
                    Field(r.RelativeItem(), "Brand", repair.Brand);
                    Field(r.RelativeItem(), "Model", repair.Model);
                });
                inner.Item().PaddingTop(3).Row(r =>
                {
                    Field(r.RelativeItem(), "Serial No.", repair.SerialNo);
                    Field(r.RelativeItem(), "Engine No.", repair.EngineNo);
                    Field(r.RelativeItem(), "Chassis No.", repair.ChassisNo);
                    Field(r.RelativeItem(), "Odometer", repair.OdometerReading?.ToString());
                });
                inner.Item().PaddingTop(3).Row(r =>
                {
                    Field(r.RelativeItem(2), "Nature of Last Repair", repair.NatureOfLastRepair);
                    Field(r.RelativeItem(), "Date of Last Repair", repair.DateOfLastRepair?.ToString("MM/dd/yyyy"));
                });
            });
        });
    }

    private void ComposeDefects(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionTitle(c, "DEFECTS / COMPLAINTS"));
            col.Item().Border(0.75f).Padding(4).Column(inner =>
            {
                Field(inner.Item(), "Nature & Scope of Work", repair.NatureOfWork);
                Field(inner.Item().PaddingTop(3), "Parts to be Supplied / Replaced", repair.PartsToReplace);
                inner.Item().PaddingTop(3).Row(r =>
                {
                    Field(r.RelativeItem(), "Requested By", repair.RequestedBy);
                    Field(r.RelativeItem(), "Date Requested", repair.RequestedOn.ToString("MM/dd/yyyy"));
                });
            });
        });
    }

    private void ComposePreInspection(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionTitle(c, "PRE-REPAIR INSPECTION (MANDATORY)"));
            col.Item().Border(0.75f).Padding(4).Column(inner =>
            {
                Field(inner.Item(), "Findings", repair.PreInspectionFindings);
                // "Noted By" is the Head of Office — sourced from the Organization Profile (Approving
                // Official). Falls back to the name captured on the inspection only when the profile is unset.
                var headOfOffice = !string.IsNullOrWhiteSpace(org?.ApprovingOfficialName)
                    ? org!.ApprovingOfficialName
                    : repair.NotedBy;
                inner.Item().PaddingTop(8).Row(r =>
                {
                    SignatureCell(r.RelativeItem(), "Pre-Inspected By (Property Inspector)", repair.PreInspectedBy, repair.PreInspectedOn);
                    r.ConstantItem(20);
                    SignatureCell(r.RelativeItem(), "Noted By (Head of Office)", headOfOffice, repair.PreInspectedOn);
                });
            });
        });
    }

    private void ComposePostInspection(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionTitle(c, "POST-REPAIR INSPECTION"));
            col.Item().Border(0.75f).Padding(4).Column(inner =>
            {
                inner.Item().Row(r =>
                {
                    Field(r.RelativeItem(2), "Repair Shop / Contractor", repair.RepairShop);
                    Field(r.RelativeItem(), "Job Order / Contract No.", repair.JobOrderNo);
                });
                inner.Item().PaddingTop(3).Row(r =>
                {
                    Field(r.RelativeItem(), "Invoice No.", repair.InvoiceNo);
                    Field(r.RelativeItem(), "Invoice Date", repair.InvoiceDate?.ToString("MM/dd/yyyy"));
                    Field(r.RelativeItem(), "Amount per JO / Payable", repair.AmountPerJO?.ToString("N2"));
                });
                Field(inner.Item().PaddingTop(3), "Findings", repair.PostInspectionFindings);
                inner.Item().PaddingTop(8).Row(r =>
                {
                    SignatureCell(r.RelativeItem(), "Post-Inspected By", repair.PostInspectedBy, repair.PostInspectedOn);
                    r.ConstantItem(20);
                    r.RelativeItem();
                });
            });
        });
    }

    private void ComposeProcurementRefs(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionTitle(c, "PROCUREMENT / FINANCE REFERENCES (outsourced repairs only)"));
            col.Item().Border(0.75f).Padding(4).Row(r =>
            {
                Field(r.RelativeItem(), "PR No.", repair.PrNo);
                Field(r.RelativeItem(), "PO / JO No.", repair.PoJoNo);
                Field(r.RelativeItem(), "BUR No.", repair.BurNo);
                Field(r.RelativeItem(), "DV No.", repair.DvNo);
            });
        });
    }

    private void ComposeAcceptance(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionTitle(c, "PROPERTY CUSTODIAN ACCEPTANCE"));
            // The accepting signatory is the Property Custodian / Supply Officer — sourced from the
            // Organization Profile. Falls back to the name captured on acceptance when the profile is unset.
            var custodian = !string.IsNullOrWhiteSpace(org?.PropertyCustodianName)
                ? org!.PropertyCustodianName
                : repair.AcceptedBy;
            col.Item().Border(0.75f).Padding(4).Row(r =>
            {
                SignatureCell(r.RelativeItem(), "Accepted By (Property Custodian)", custodian, repair.AcceptedOn);
                r.ConstantItem(20);
                r.RelativeItem();
            });
        });
    }

    private static void Field(IContainer container, string label, string? value)
    {
        container.Text(t =>
        {
            t.Span($"{label}: ").SemiBold().FontSize(8);
            t.Span(string.IsNullOrWhiteSpace(value) ? "—" : value).FontSize(8);
        });
    }

    private static void SignatureCell(IContainer container, string label, string? name, DateOnly? date)
    {
        container.Column(cell =>
        {
            cell.Item().PaddingTop(10).Text(string.IsNullOrWhiteSpace(name) ? string.Empty : name).Bold().FontSize(9);
            cell.Item().LineHorizontal(0.5f);
            cell.Item().Text(label).FontSize(7);
            cell.Item().Text(date.HasValue ? $"Date: {date.Value:MM/dd/yyyy}" : "Date: ____________").FontSize(7);
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.AlignRight().Text(x =>
        {
            x.Span("Page ").FontSize(7);
            x.CurrentPageNumber().FontSize(7);
            x.Span(" of ").FontSize(7);
            x.TotalPages().FontSize(7);
        });
    }
}
