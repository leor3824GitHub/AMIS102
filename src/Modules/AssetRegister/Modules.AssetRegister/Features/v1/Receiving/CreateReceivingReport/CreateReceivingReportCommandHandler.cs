using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using AMIS.Modules.AssetRegister.Domain.Receiving;
using AMIS.Modules.AssetRegister.Domain.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.CreateReceivingReport;

public sealed class CreateReceivingReportCommandHandler(
    AssetRegisterDbContext db,
    IReceivingReportNumberGenerator reportNumbers,
    IPropertyNumberGenerator propertyNumbers)
    : ICommandHandler<CreateReceivingReportCommand, ReceivingReportDto>
{
    public async ValueTask<ReceivingReportDto> Handle(CreateReceivingReportCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var catalogIds = cmd.Items.Select(i => i.CatalogItemId).Distinct().ToList();
        var catalogs = await db.PropertyItemCatalogs
            .Where(c => catalogIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct)
            .ConfigureAwait(false);

        foreach (var id in catalogIds)
        {
            if (!catalogs.TryGetValue(id, out var c))
                throw new KeyNotFoundException($"PropertyItemCatalog '{id}' not found.");
            if (!c.IsActive)
                throw new InvalidOperationException($"Catalog item '{c.Code}' is deactivated and cannot be received.");
            if (string.IsNullOrWhiteSpace(c.UacsObjectCode))
                throw new InvalidOperationException(
                    $"Catalog item '{c.Code}' must carry a UacsObjectCode before assets can be registered against it.");
        }

        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var reportNo = await reportNumbers.NextAsync(cmd.DocumentKind, cmd.Date, ct).ConfigureAwait(false);

        var receivedBy = EmployeeRef.Create(cmd.ReceivedBy.EmployeeId, cmd.ReceivedBy.PrintedName, cmd.ReceivedBy.Designation);
        var notedBy = cmd.NotedBy is null
            ? null
            : EmployeeRef.Create(cmd.NotedBy.EmployeeId, cmd.NotedBy.PrintedName, cmd.NotedBy.Designation);

        var report = ReceivingReport.Create(
            tenantId, cmd.DocumentKind, reportNo, cmd.Date,
            cmd.ReceivedFrom, cmd.Address, cmd.ReceiptType, cmd.OtherReceiptType,
            cmd.FundCluster, receivedBy, notedBy, cmd.DateReceived);

        var (assetType, category) = ClassifyFor(cmd.DocumentKind);
        var fundCluster = cmd.FundCluster ?? string.Empty;
        const string DefaultLocationCode = "00";

        foreach (var line in cmd.Items)
        {
            report.AddItem(
                line.CatalogItemId, line.Reference, line.Description,
                line.AcquisitionDate, line.Quantity, line.UnitCost,
                line.SerialNo, line.Brand, line.Model);

            var catalog = catalogs[line.CatalogItemId];
            for (var i = 0; i < line.Quantity; i++)
            {
                var propertyNo = await propertyNumbers.NextAsync(
                    assetType,
                    catalog.DefaultPropertyClass,
                    catalog.DefaultCategoryCode,
                    DefaultLocationCode,
                    line.AcquisitionDate,
                    ct).ConfigureAwait(false);

                var asset = AssetRegistry.Register(
                    tenantId, catalog, assetType, category, propertyNo,
                    line.Description, line.SerialNo, line.Brand, line.Model,
                    fundCluster, line.AcquisitionDate, line.UnitCost,
                    sourceIARId: null, sourcePurchaseOrderId: null);
                db.AssetRegistries.Add(asset);
            }
        }

        db.ReceivingReports.Add(report);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ReceivingMapper.ToDto(report);
    }

    private static (AssetType assetType, AssetCategory category) ClassifyFor(ReceivingDocumentKind kind) =>
        kind == ReceivingDocumentKind.PPERR
            ? (AssetType.PPE, AssetCategory.PPE)
            : (AssetType.SE, AssetCategory.HighValuedSemi);
}

