using System.Data;
using System.Globalization;
using System.Reflection;
using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using AMIS.Modules.FastReporting.Services;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
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

        // The ROPC committee is frozen at award time (faithful reprint). For canvasses NOT yet awarded —
        // the abstract is being printed for the committee to sign and recommend the award — and for
        // legacy awarded canvasses with no snapshot, fall back to the live configured ReportSignatories +
        // org officers, mirroring how the PR/PO reports show org defaults before the signing action.
        IReadOnlyList<CanvassAwardSignatoryDto> committeeList = canvass.AwardSignatories is { Count: > 0 } snapshot
            ? snapshot
            : BuildLiveCommittee(
                await mediator.Send(new GetReportSignatoriesQuery("AbstractOfCanvass"), ct).ConfigureAwait(false), org);
        var committee = committeeList.GroupBy(s => s.SortOrder).ToDictionary(g => g.Key, g => g.First());

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
                Member1Name:      CommitteeName(committee, 1),
                Member1Role:      CommitteeRole(committee, 1),
                Member2Name:      CommitteeName(committee, 2),
                Member2Role:      CommitteeRole(committee, 2),
                Member3Name:      CommitteeName(committee, 3),
                Member3Role:      CommitteeRole(committee, 3),
                Member4Name:      CommitteeName(committee, 4),
                Member4Role:      CommitteeRole(committee, 4),
                ViceChairName:    CommitteeName(committee, 5),
                ViceChairRole:    CommitteeRole(committee, 5),
                ChairName:        CommitteeName(committee, 6),
                ChairRole:        CommitteeRole(committee, 6))
        };

        var lineItemsTable = BuildLineItemsTable(canvass.LineItems, quotations, query.MinRows);

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

    // Live committee resolution for un-awarded / legacy canvasses (no frozen snapshot). MUST stay in sync
    // with AwardCanvassCommandHandler.ResolveCommitteeAsync, which freezes the same slots at award time.
    private static IReadOnlyList<CanvassAwardSignatoryDto> BuildLiveCommittee(
        IReadOnlyList<ReportSignatoryDto> signatories, OrganizationProfileDto? org)
    {
        string Name(int order, string? orgFallback = null) =>
            signatories.FirstOrDefault(s => s.SortOrder == order && s.IsActive)?.Name ?? orgFallback ?? string.Empty;
        string Role(int order, string? fallback = null) =>
            signatories.FirstOrDefault(s => s.SortOrder == order && s.IsActive)?.Label ?? fallback ?? string.Empty;

        return
        [
            new(1, Name(1), Role(1, "TWG Goods/Services-Chairperson")),
            new(2, Name(2, org?.AccountantName), Role(2, org?.AccountantDesignation ?? "Accountant")),
            new(3, Name(3), Role(3, "ROPC Member")),
            new(4, Name(4), Role(4, "ROPC Member")),
            new(5, Name(5, org?.SupervisingAdminOfficerName),
                Role(5, org?.SupervisingAdminOfficerDesignation ?? "Administrative Officer")),
            new(6, Name(6, org?.AssistantRegionalManagerName ?? org?.ApprovingOfficialName),
                Role(6, org?.AssistantRegionalManagerDesignation ?? org?.ApprovingOfficialDesignation ?? "Co-Approving Official")),
        ];
    }

    private static string CommitteeName(IReadOnlyDictionary<int, CanvassAwardSignatoryDto> committee, int order) =>
        committee.TryGetValue(order, out var s) ? (s.Name ?? string.Empty).ToUpperInvariant() : string.Empty;

    private static string CommitteeRole(IReadOnlyDictionary<int, CanvassAwardSignatoryDto> committee, int order) =>
        committee.TryGetValue(order, out var s) ? s.Role ?? string.Empty : string.Empty;

    // Build the cross-supplier price table. One row per covered PR line (keyed by PrItemNo), with up to 5
    // supplier prices. The split-award winner for a line is flagged with a leading marker on that supplier's
    // price so the Abstract shows, per line, which supplier was awarded it.
    private static DataTable BuildLineItemsTable(
        IReadOnlyList<CanvassLineItemDto> lineItems,
        IReadOnlyList<CanvassQuotationDto> quotations,
        int minRows)
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

        foreach (var line in lineItems.OrderBy(x => x.PrItemNo))
        {
            var row = table.NewRow();
            row["Quantity"] = line.Quantity.ToString("N0", nf);
            row["Unit"] = line.Unit;
            row["Description"] = line.Description;

            for (var i = 0; i < MaxSupplierColumns; i++)
            {
                var col = $"Sup{i + 1}Price";
                if (i < quotations.Count)
                {
                    var match = quotations[i].LineItems
                        .FirstOrDefault(li => li.PrItemNo == line.PrItemNo);
                    if (match is null)
                    {
                        row[col] = string.Empty;
                    }
                    else
                    {
                        var awarded = line.AwardedSupplierId is { } sid && quotations[i].SupplierId == sid;
                        row[col] = (awarded ? "* " : string.Empty) + match.UnitPrice.ToString("N2", nf);
                    }
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
