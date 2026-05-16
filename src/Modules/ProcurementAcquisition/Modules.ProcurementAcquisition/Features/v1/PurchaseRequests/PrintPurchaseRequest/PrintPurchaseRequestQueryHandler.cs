using System.Collections;
using System.Reflection;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Framework.Reporting;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.PrintPurchaseRequest;

public sealed class PrintPurchaseRequestQueryHandler(
    ProcurementDbContext dbContext,
    IMediator mediator)
    : IQueryHandler<PrintPurchaseRequestQuery, byte[]>
{
    private static readonly Assembly Assembly = typeof(PrintPurchaseRequestQueryHandler).Assembly;
    private const string ReportResource = "AMIS.Modules.ProcurementAcquisition.Reports.PurchaseRequest.rdlc";

    public async ValueTask<byte[]> Handle(PrintPurchaseRequestQuery query, CancellationToken ct)
    {
        var pr = await dbContext.PurchaseRequests
            .AsNoTracking()
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Purchase request '{query.Id}' not found.");

        var department = await mediator
            .Send(new GetDepartmentReferenceByIdQuery(pr.DepartmentId), ct)
            .ConfigureAwait(false);

        var orgProfile = await mediator
            .Send(new GetOrganizationProfileQuery(), ct)
            .ConfigureAwait(false);

        var headerData = new List<PrReportHeader>
        {
            new(
                PrNumber:                pr.PrNumber,
                PrDate:                  pr.PrDate.ToString("MMMM dd, yyyy"),
                SaiNumber:               pr.SaiNumber,
                SaiDate:                 pr.SaiDate?.ToString("MMMM dd, yyyy"),
                AlobsNumber:             pr.AlobsNumber,
                AlobsDate:               pr.AlobsDate?.ToString("MMMM dd, yyyy"),
                DepartmentName:          department?.Name ?? string.Empty,
                ResponsibilityCenterCode: pr.ResponsibilityCenterCode,
                Purpose:                 pr.Purpose,
                Justification:           pr.Justification,
                RequestedByName:         pr.RequestedByName,
                ApprovedByName:          pr.ApprovedByName)
        };

        var lineItems = pr.LineItems
            .OrderBy(x => x.ItemNo)
            .Select(li => new PrReportLineItem(
                li.ItemNo, li.UnitOfIssue, li.ItemDescription,
                li.Quantity, li.EstimatedUnitCost, li.EstimatedTotalCost))
            .ToList();

        var orgData = new List<PrReportOrgProfile>
        {
            new(orgProfile?.Name ?? string.Empty, orgProfile?.Address)
        };

        return RdlcReportHelper.Render(
            Assembly,
            ReportResource,
            [
                ("PurchaseRequestDS",    (IEnumerable)headerData),
                ("LineItemsDS",          (IEnumerable)lineItems),
                ("OrganizationProfileDS",(IEnumerable)orgData)
            ],
            pageWidth:  query.PageWidth,
            pageHeight: query.PageHeight);
    }
}

// ── Internal flat records for RDLC data binding ──────────────────────────────

internal sealed record PrReportHeader(
    string  PrNumber,
    string  PrDate,
    string? SaiNumber,
    string? SaiDate,
    string? AlobsNumber,
    string? AlobsDate,
    string  DepartmentName,
    string? ResponsibilityCenterCode,
    string  Purpose,
    string? Justification,
    string  RequestedByName,
    string? ApprovedByName);

internal sealed record PrReportLineItem(
    int     ItemNo,
    string  UnitOfIssue,
    string  ItemDescription,
    decimal Quantity,
    decimal EstimatedUnitCost,
    decimal EstimatedTotalCost);

internal sealed record PrReportOrgProfile(
    string  Name,
    string? Address);
