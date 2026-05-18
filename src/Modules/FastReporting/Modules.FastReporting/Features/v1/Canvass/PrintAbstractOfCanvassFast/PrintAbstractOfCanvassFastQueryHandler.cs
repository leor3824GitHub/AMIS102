using System.Data;
using System.Globalization;
using System.Reflection;
using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using AMIS.Modules.FastReporting.Services;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using FastReport;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.Canvass.PrintAbstractOfCanvassFast;

public sealed class PrintAbstractOfCanvassFastQueryHandler(IMediator mediator)
    : IQueryHandler<PrintAbstractOfCanvassFastQuery, ReportFileDto>
{
    private static readonly Assembly Assembly = typeof(PrintAbstractOfCanvassFastQueryHandler).Assembly;
    private const string TemplateName = "AbstractOfCanvassFast";

    // The template predeclares 5 supplier columns; the handler hides + resizes
    // them to match the canvass's actual supplier count.
    private const int MaxSupplierColumns = 5;
    private const float DescColRight = 333f;
    private const float ContentRight = 733f;
    private const float SupplierAreaWidth = ContentRight - DescColRight; // 400

    public async ValueTask<ReportFileDto> Handle(PrintAbstractOfCanvassFastQuery query, CancellationToken ct)
    {
        var canvass = await mediator.Send(new GetCanvassRequestQuery(query.Id), ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Canvass request '{query.Id}' not found.");

        var pr = await mediator.Send(new GetPurchaseRequestQuery(canvass.PurchaseRequestId), ct).ConfigureAwait(false);
        var org = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);

        var nf = CultureInfo.InvariantCulture;
        var quotations = canvass.Quotations.ToList();
        var supplierCount = Math.Min(quotations.Count, MaxSupplierColumns);

        // Supplier names for the 5 column-header slots (unused slots stay blank).
        var supplierNames = new string[MaxSupplierColumns];
        for (var i = 0; i < MaxSupplierColumns; i++)
            supplierNames[i] = i < quotations.Count ? quotations[i].SupplierName : string.Empty;

        var headerData = new List<AocFastHeader>
        {
            new(
                OrgName:          org?.Name ?? string.Empty,
                OrgShortName:     org?.ShortName ?? string.Empty,
                OrgAddress:       org?.Address ?? string.Empty,
                RivNumber:        canvass.RivNumber,
                PrNumber:         canvass.PrNumber,
                CanvassDate:      canvass.CreatedOnUtc.ToLocalTime().ToString("MM/dd/yyyy", nf),
                ReturnDeadline:   canvass.ReturnDeadline.ToString("MM/dd/yyyy", nf),
                Purpose:          $"Purpose: {pr?.Purpose ?? string.Empty}",
                Sup1Name:         supplierNames[0],
                Sup2Name:         supplierNames[1],
                Sup3Name:         supplierNames[2],
                Sup4Name:         supplierNames[3],
                Sup5Name:         supplierNames[4],
                Member1Name:      string.Empty,
                Member1Role:      "TWG Goods/Services-Chairperson",
                Member2Name:      (org?.AccountantName ?? string.Empty).ToUpperInvariant(),
                Member2Role:      org?.AccountantDesignation ?? "Accountant IV",
                Member3Name:      string.Empty,
                Member3Role:      "ROPC Member",
                Member4Name:      string.Empty,
                Member4Role:      "ROPC Member",
                ViceChairName:    (org?.SupervisingAdminOfficerName ?? string.Empty).ToUpperInvariant(),
                ViceChairRole:    org?.SupervisingAdminOfficerDesignation ?? "Supervising Administrative Officer",
                ChairName:        (org?.AssistantRegionalManagerName ?? org?.RegionalManagerName ?? string.Empty).ToUpperInvariant(),
                ChairRole:        org?.AssistantRegionalManagerDesignation ?? org?.RegionalManagerDesignation ?? "Assistant Regional Manager II")
        };

        var lineItemsTable = BuildLineItemsTable(quotations, query.MinRows);

        return await FastReportService.GenerateAsync(
            Assembly,
            TemplateName,
            [
                new ReportDataSource("CanvassDS", headerData),
                new ReportDataSource("LineItemsDS", lineItemsTable),
            ],
            format: ReportFormat.Pdf,
            configureReport: report =>
            {
                FastReportPaperSize.Apply(report, query.PaperSize, query.Orientation);
                AdjustSupplierColumns(report, supplierCount);
            },
            configureDataBindings: report =>
            {
                if (report.FindObject("Data1") is DataBand dataBand)
                    dataBand.DataSource = report.GetDataSource("LineItemsDS");
            },
            fileName: $"AOC-{canvass.RivNumber}",
            ct: ct).ConfigureAwait(false);
    }

    // Build the cross-supplier price table. For each unique item description across
    // all quotations, emit one row with qty/unit/description and up to 5 supplier prices.
    private static DataTable BuildLineItemsTable(IReadOnlyList<CanvassQuotationDto> quotations, int minRows)
    {
        var nf = CultureInfo.InvariantCulture;
        var table = new DataTable("LineItemsDS") { Locale = CultureInfo.InvariantCulture };
        table.Columns.Add("Quantity", typeof(string));
        table.Columns.Add("Unit", typeof(string));
        table.Columns.Add("Description", typeof(string));
        table.Columns.Add("Sup1Price", typeof(string));
        table.Columns.Add("Sup2Price", typeof(string));
        table.Columns.Add("Sup3Price", typeof(string));
        table.Columns.Add("Sup4Price", typeof(string));
        table.Columns.Add("Sup5Price", typeof(string));

        // Preserve the item order from the first quotation, then append items that
        // only appear in later quotations. Match by normalized description.
        var items = new List<(string Key, decimal Qty, string Unit, string Description)>();
        var keyIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var q in quotations)
        {
            foreach (var li in q.LineItems.OrderBy(x => x.ItemNo))
            {
                var key = NormalizeKey(li.Description);
                if (key.Length == 0 || keyIndex.ContainsKey(key))
                    continue;

                keyIndex[key] = items.Count;
                items.Add((key, li.Quantity, li.Unit, li.Description));
            }
        }

        foreach (var (key, qty, unit, desc) in items)
        {
            var row = table.NewRow();
            row["Quantity"] = qty.ToString("N0", nf);
            row["Unit"] = unit;
            row["Description"] = desc;

            for (var i = 0; i < MaxSupplierColumns; i++)
            {
                var col = $"Sup{i + 1}Price";
                if (i < quotations.Count)
                {
                    var match = quotations[i].LineItems
                        .FirstOrDefault(li => string.Equals(NormalizeKey(li.Description), key, StringComparison.OrdinalIgnoreCase));
                    row[col] = match is null ? string.Empty : match.UnitPrice.ToString("N2", nf);
                }
                else
                {
                    row[col] = string.Empty;
                }
            }

            table.Rows.Add(row);
        }

        // Pad with blank rows so the data band always renders at least MinRows rows.
        var padTo = Math.Max(minRows, table.Rows.Count);
        while (table.Rows.Count < padTo)
        {
            table.Rows.Add(string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        return table;
    }

    // Resize and hide supplier columns (HSupN/DSupN) so the visible columns fill the
    // available 400-unit area evenly. If supplierCount == 0, hide all five columns.
    private static void AdjustSupplierColumns(Report report, int supplierCount)
    {
        var visible = Math.Clamp(supplierCount, 0, MaxSupplierColumns);
        var perColWidth = visible == 0 ? 0f : SupplierAreaWidth / visible;

        for (var i = 1; i <= MaxSupplierColumns; i++)
        {
            var isVisible = i <= visible;
            var left = DescColRight + (i - 1) * perColWidth;

            if (report.FindObject($"HSup{i}") is TextObject header)
            {
                header.Visible = isVisible;
                if (isVisible)
                {
                    header.Left = left;
                    header.Width = perColWidth;
                }
            }

            if (report.FindObject($"DSup{i}") is TextObject cell)
            {
                cell.Visible = isVisible;
                if (isVisible)
                {
                    cell.Left = left;
                    cell.Width = perColWidth;
                }
            }
        }
    }

    private static string NormalizeKey(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
}

internal sealed record AocFastHeader(
    string OrgName,
    string OrgShortName,
    string OrgAddress,
    string RivNumber,
    string PrNumber,
    string CanvassDate,
    string ReturnDeadline,
    string Purpose,
    string Sup1Name,
    string Sup2Name,
    string Sup3Name,
    string Sup4Name,
    string Sup5Name,
    string Member1Name,
    string Member1Role,
    string Member2Name,
    string Member2Role,
    string Member3Name,
    string Member3Role,
    string Member4Name,
    string Member4Role,
    string ViceChairName,
    string ViceChairRole,
    string ChairName,
    string ChairRole);
