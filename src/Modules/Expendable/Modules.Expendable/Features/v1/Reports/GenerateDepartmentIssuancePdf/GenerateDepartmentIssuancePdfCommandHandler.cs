using FSH.Modules.Expendable.Contracts.v1.Reports;
using QuestPDF.Fluent;
using FSH.Modules.Expendable.Contracts.v1.Requests;
using FSH.Modules.Expendable.Domain.Requests;
using FSH.Modules.Expendable.Data;
using FSH.Modules.MasterData.Contracts.v1.OrganizationProfile;
using FSH.Modules.MasterData.Contracts.v1.ReportSignatories;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Expendable.Features.v1.Reports.GenerateDepartmentIssuancePdf;

public sealed class GenerateDepartmentIssuancePdfCommandHandler(
    ExpendableDbContext db,
    IMediator mediator)
    : ICommandHandler<GenerateDepartmentIssuancePdfCommand, byte[]>
{
    public async ValueTask<byte[]> Handle(
        GenerateDepartmentIssuancePdfCommand command, CancellationToken cancellationToken)
    {
        // 1. Fetch all matching fulfilled supply requests
        var requestsQuery = db.SupplyRequests
            .AsNoTracking()
            .Where(r => r.Status == SupplyRequestStatus.Fulfilled);

        if (!string.IsNullOrWhiteSpace(command.DepartmentId))
            requestsQuery = requestsQuery.Where(r => r.DepartmentId == command.DepartmentId);
        if (command.From.HasValue)
            requestsQuery = requestsQuery.Where(r => r.LastModifiedOnUtc >= command.From.Value);
        if (command.To.HasValue)
            requestsQuery = requestsQuery.Where(r => r.LastModifiedOnUtc <= command.To.Value);

        var requests = await requestsQuery.ToListAsync(cancellationToken).ConfigureAwait(false);

        var productIds = requests
            .SelectMany(r => r.Items)
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();

        var products = await db.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.SKU, p.UnitOfMeasure })
            .ToDictionaryAsync(p => p.Id, cancellationToken)
            .ConfigureAwait(false);

        // Build report data matching the existing query handler's aggregation logic
        var grouped = requests
            .GroupBy(r => r.DepartmentId)
            .OrderBy(g => g.Key)
            .Select(deptGroup =>
            {
                var products2 = deptGroup
                    .SelectMany(r => r.Items.Where(i => i.FulfilledQuantity > 0))
                    .GroupBy(i => i.ProductId)
                    .Select(pg =>
                    {
                        var product = products.GetValueOrDefault(pg.Key);
                        var totalQty = pg.Sum(i => i.FulfilledQuantity);
                        var totalVal = pg.Sum(i => i.FulfilledValue);
                        return new DepartmentProductBreakdownDto(
                            pg.Key,
                            product?.Name ?? "Unknown",
                            product?.SKU ?? string.Empty,
                            totalQty,
                            totalVal,
                            product?.UnitOfMeasure ?? string.Empty,
                            totalQty > 0 ? Math.Round(totalVal / totalQty, 4) : 0m);
                    })
                    .OrderBy(p => p.ProductName)
                    .ToList();

                return new DepartmentIssuanceSummaryDto(
                    deptGroup.Key,
                    deptGroup.Count(),
                    products2.Sum(p => p.TotalQuantityIssued),
                    products2.Sum(p => p.TotalValue),
                    products2);
            })
            .ToList();

        // 2. Build department name lookup from the data we already have
        var departmentNames = grouped
            .Select(g => g.DepartmentId)
            .Distinct()
            .ToDictionary(id => id, id => id); // fallback: show ID if no lookup available

        // 3. Fetch org profile and signatories sequentially to avoid concurrent use of MasterDataDbContext
        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken)
            .ConfigureAwait(false);
        var signatories = await mediator.Send(new GetReportSignatoriesQuery("DepartmentIssuance"), cancellationToken)
            .ConfigureAwait(false);

        // 4. Build and return the PDF
        var document = new DepartmentIssuancePdfDocument(
            grouped, org, signatories, command.From, command.To, departmentNames);

        return document.GeneratePdf();
    }
}
