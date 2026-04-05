using FSH.Modules.Expendable.Contracts.v1.Reports;
using FSH.Modules.Expendable.Contracts.v1.Requests;
using FSH.Modules.Expendable.Data;
using FSH.Modules.MasterData.Contracts.v1.OrganizationProfile;
using FSH.Modules.MasterData.Contracts.v1.ReportSignatories;
using Mediator;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace FSH.Modules.Expendable.Features.v1.Reports.GeneratePhysicalCountPdf;

public sealed class GeneratePhysicalCountPdfCommandHandler(
    ExpendableDbContext db,
    IMediator mediator)
    : ICommandHandler<GeneratePhysicalCountPdfCommand, byte[]>
{
    public async ValueTask<byte[]> Handle(
        GeneratePhysicalCountPdfCommand command, CancellationToken cancellationToken)
    {
        // 1. Fetch physical count data (mirrors GetPhysicalCountReportQueryHandler)
        var products = await db.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.SKU)
            .Select(p => new { p.Id, p.SKU, p.Name, p.UnitOfMeasure, p.UnitPrice })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var inventoryQuery = db.ProductInventories.AsNoTracking();
        if (command.WarehouseLocationId.HasValue && command.WarehouseLocationId != Guid.Empty)
            inventoryQuery = inventoryQuery.Where(i => i.WarehouseLocationId == command.WarehouseLocationId.Value);

        var inventories = await inventoryQuery
            .Select(i => new
            {
                i.ProductId,
                QuantityOnHand = i.QuantityAvailable + i.QuantityReserved,
                i.TotalValue,
                i.AverageUnitPrice
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var inventoryByProduct = inventories
            .GroupBy(i => i.ProductId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    QuantityOnHand = g.Sum(i => i.QuantityOnHand),
                    AverageUnitPrice = g.Sum(i => i.QuantityOnHand) > 0
                        ? Math.Round(g.Sum(i => i.TotalValue) / g.Sum(i => i.QuantityOnHand), 4)
                        : 0m
                });

        var items = products.Select((p, idx) =>
        {
            var inv = inventoryByProduct.GetValueOrDefault(p.Id);
            var unitValue = inv?.AverageUnitPrice > 0 ? inv.AverageUnitPrice : 0m;
            var qty = inv?.QuantityOnHand ?? 0;
            return new PhysicalCountItemDto(
                ArticleNumber: idx + 1,
                Description: p.Name,
                StockNo: p.SKU,
                UnitOfMeasure: p.UnitOfMeasure,
                UnitValue: unitValue,
                BalancePerCard: qty,
                OnHandPerCount: qty,
                ShortageQuantity: 0,
                ShortageValue: 0m,
                Remarks: null);
        }).ToList();

        // 2. Fetch org profile + signatories sequentially to avoid concurrent use of MasterDataDbContext
        var organization = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken)
            .ConfigureAwait(false);
        var signatories = await mediator.Send(new GetReportSignatoriesQuery("PhysicalCount"), cancellationToken)
            .ConfigureAwait(false);

        var document = new PhysicalCountPdfDocument(
            items, organization, signatories, command.AsOfDate);

        return document.GeneratePdf();
    }
}
