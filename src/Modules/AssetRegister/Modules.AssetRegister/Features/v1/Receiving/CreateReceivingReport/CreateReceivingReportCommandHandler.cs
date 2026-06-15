using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;
using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Receiving;
using AMIS.Modules.AssetRegister.Domain.Services;
using AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.CreateReceivingReport;

public sealed class CreateReceivingReportCommandHandler(
    AssetRegisterDbContext db,
    IReceivingReportNumberGenerator reportNumbers,
    IMediator mediator)
    : ICommandHandler<CreateReceivingReportCommand, ReceivingReportDto>
{
    public async ValueTask<ReceivingReportDto> Handle(CreateReceivingReportCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        await EnforceCapitalizationThresholdAsync(cmd, cancellationToken).ConfigureAwait(false);

        // Resolve catalog rows referenced by the request. As of Phase 3, every line MUST carry an explicit
        // CatalogItemId — the old fuzzy fallback (PropertyClassHint / substring / token overlap) is gone.
        var ids = cmd.Items
            .Where(i => i.CatalogItemId.HasValue && i.CatalogItemId.Value != Guid.Empty)
            .Select(i => i.CatalogItemId!.Value)
            .Distinct()
            .ToList();

        var catalogsById = await db.PropertyItemCatalogs
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken)
            .ConfigureAwait(false);

        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var reportNo = await reportNumbers.NextAsync(cmd.DocumentKind, cmd.Date, cancellationToken).ConfigureAwait(false);

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

        foreach (var line in cmd.Items)
        {
            var catalog = ResolveCatalog(line, catalogsById);

            report.AddItem(
                catalog.Id, line.Reference, line.PropertyNo, line.Description,
                line.AcquisitionDate, quantity: 1, line.UnitCost,
                line.SerialNo, line.Brand, line.Model,
                line.UacsObjectCode ?? catalog.UacsObjectCode,
                line.SourceAgencyName, line.SourcePropertyNo, line.SourceDocumentRef, line.OriginalAcquisitionDate);

            // For donations/transfers the receiving agency carries the original timeline (COA GAM —
            // depreciation continuity), so AssetRegistry's AcquisitionDate is the OriginalAcquisitionDate
            // when supplied. For purchases it stays the line's booked date.
            var assetAcquisitionDate = line.OriginalAcquisitionDate ?? line.AcquisitionDate;

            var propertyNo = PropertyNumber.Create(line.PropertyNo);
            var asset = AssetRegistry.Register(
                tenantId, catalog, assetType, category, propertyNo,
                line.Description, line.SerialNo, line.Brand, line.Model,
                fundCluster, assetAcquisitionDate, line.UnitCost,
                sourceIARId: line.SourceIARId, sourcePurchaseOrderId: null);
            db.AssetRegistries.Add(asset);
        }

        db.ReceivingReports.Add(report);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ReceivingMapper.ToDto(report);
    }

    private static (AssetType assetType, AssetCategory category) ClassifyFor(ReceivingDocumentKind kind) =>
        kind == ReceivingDocumentKind.PPERR
            ? (AssetType.PPE, AssetCategory.PPE)
            : (AssetType.SE, AssetCategory.HighValuedSemi);

    /// <summary>
    /// Resolves an explicit <c>CatalogItemId</c> to its row. Throws a clean 400-mapped exception when the
    /// id is missing, unknown, deactivated, or still <see cref="CatalogItemStatus.Draft"/> (no UACS yet —
    /// an Accountant must certify the source PR first).
    /// </summary>
    private static Domain.Catalog.PropertyItemCatalog ResolveCatalog(
        CreateReceivingReportItemRequest line,
        IReadOnlyDictionary<Guid, Domain.Catalog.PropertyItemCatalog> byId)
    {
        if (!line.CatalogItemId.HasValue || line.CatalogItemId.Value == Guid.Empty)
            throw new CustomException(
                $"Line '{line.Description}' (Property No '{line.PropertyNo}'): CatalogItemId is required. " +
                "Pick a catalog item on the source PR or PPERR form before saving.",
                [], System.Net.HttpStatusCode.BadRequest);

        if (!byId.TryGetValue(line.CatalogItemId.Value, out var c))
            throw new CustomException(
                $"Line '{line.Description}': PropertyItemCatalog '{line.CatalogItemId}' was not found.",
                [], System.Net.HttpStatusCode.BadRequest);

        if (!c.IsActive)
            throw new CustomException(
                $"Line '{line.Description}': catalog item '{c.Code}' is deactivated and cannot be received.",
                [], System.Net.HttpStatusCode.BadRequest);

        if (c.Status == CatalogItemStatus.Draft || string.IsNullOrWhiteSpace(c.UacsObjectCode))
            throw new CustomException(
                $"Line '{line.Description}': catalog item '{c.Code}' is still Draft (UACS not assigned). " +
                "An Accountant must certify Funds Available on a PR that uses this item before assets can be registered against it.",
                [], System.Net.HttpStatusCode.BadRequest);

        return c;
    }

    // No-op when no active threshold is configured — the master data is optional and back-compatible.
    private async Task EnforceCapitalizationThresholdAsync(CreateReceivingReportCommand cmd, CancellationToken cancellationToken)
    {
        var threshold = await mediator.Send(new GetActiveCapitalizationThresholdQuery(), cancellationToken).ConfigureAwait(false);
        if (threshold is null)
            return;

        var isPpe = cmd.DocumentKind == ReceivingDocumentKind.PPERR;

        var offending = cmd.Items
            .Select((line, idx) => (line, idx))
            .Where(t => threshold.IsCapitalAsset(t.line.UnitCost) != isPpe)
            .ToList();

        if (offending.Count == 0)
            return;

        var expected = isPpe ? $">= ₱{threshold.CapitalizationAmount:N2}" : $"< ₱{threshold.CapitalizationAmount:N2}";
        var otherKind = isPpe ? ReceivingDocumentKind.SMRR : ReceivingDocumentKind.PPERR;
        var problems = string.Join("; ",
            offending.Select(t => $"line {t.idx + 1} '{t.line.Description}' @ ₱{t.line.UnitCost:N2}"));

        throw new CustomException(
            $"{cmd.DocumentKind} requires every line's unit cost to be {expected} per COA Circular '{threshold.CircularName}'. " +
            $"Mismatched line(s): {problems}. Move these to a {otherKind} or split the receipt.",
            [], System.Net.HttpStatusCode.BadRequest);
    }
}
