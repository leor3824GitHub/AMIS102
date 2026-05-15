using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Receiving;
using AMIS.Modules.AssetRegister.Domain.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.CreateReceivingReport;

public sealed class CreateReceivingReportCommandHandler(
    AssetRegisterDbContext db,
    IReceivingReportNumberGenerator reportNumbers)
    : ICommandHandler<CreateReceivingReportCommand, ReceivingReportDto>
{
    public async ValueTask<ReceivingReportDto> Handle(CreateReceivingReportCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        // Load every active catalog once; we resolve per-line below — either by
        // the explicit CatalogItemId or by PropertyClassHint coming from an IAR.
        var allCatalogs = await db.PropertyItemCatalogs
            .Where(c => c.IsActive)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var catalogsById = allCatalogs.ToDictionary(c => c.Id);

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

        foreach (var line in cmd.Items)
        {
            var catalog = ResolveCatalog(line, allCatalogs, catalogsById);

            report.AddItem(
                catalog.Id, line.Reference, line.Description,
                line.AcquisitionDate, quantity: 1, line.UnitCost,
                line.SerialNo, line.Brand, line.Model);

            var propertyNo = PropertyNumber.Create(line.PropertyNo);
            var asset = AssetRegistry.Register(
                tenantId, catalog, assetType, category, propertyNo,
                line.Description, line.SerialNo, line.Brand, line.Model,
                fundCluster, line.AcquisitionDate, line.UnitCost,
                sourceIARId: line.SourceIARId, sourcePurchaseOrderId: null);
            db.AssetRegistries.Add(asset);
        }

        db.ReceivingReports.Add(report);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ReceivingMapper.ToDto(report);
    }

    private static (AssetType assetType, AssetCategory category) ClassifyFor(ReceivingDocumentKind kind) =>
        kind == ReceivingDocumentKind.PPERR
            ? (AssetType.PPE, AssetCategory.PPE)
            : (AssetType.SE, AssetCategory.HighValuedSemi);

    /// <summary>
    /// Pick a catalog for the line: prefer an explicit CatalogItemId; otherwise resolve
    /// from PropertyClassHint, then by description substring, then by token overlap —
    /// mirrors <c>AssetIARAcceptedEventConsumer.ResolveCatalog</c> so IAR-driven and
    /// event-driven flows pick the same catalog row.
    /// </summary>
    private static Domain.Catalog.PropertyItemCatalog ResolveCatalog(
        CreateReceivingReportItemRequest line,
        IReadOnlyList<Domain.Catalog.PropertyItemCatalog> all,
        IReadOnlyDictionary<Guid, Domain.Catalog.PropertyItemCatalog> byId)
    {
        if (line.CatalogItemId.HasValue && line.CatalogItemId.Value != Guid.Empty)
        {
            if (!byId.TryGetValue(line.CatalogItemId.Value, out var c))
                throw new KeyNotFoundException($"PropertyItemCatalog '{line.CatalogItemId}' not found.");
            if (!c.IsActive)
                throw new InvalidOperationException($"Catalog item '{c.Code}' is deactivated and cannot be received.");
            return c;
        }

        if (!string.IsNullOrWhiteSpace(line.PropertyClassHint))
        {
            var byClass = all.FirstOrDefault(c =>
                string.Equals(c.DefaultPropertyClass, line.PropertyClassHint, StringComparison.OrdinalIgnoreCase));
            if (byClass is not null) return byClass;
        }

        var bySubstring = all.FirstOrDefault(c =>
            line.Description.Contains(c.Description, StringComparison.OrdinalIgnoreCase) ||
            c.Description.Contains(line.Description, StringComparison.OrdinalIgnoreCase));
        if (bySubstring is not null) return bySubstring;

        var lineTokens = Tokenize(line.Description);
        if (lineTokens.Count > 0)
        {
            var byTokens = all
                .Select(c => new { Catalog = c, Score = Tokenize(c.Description).Count(lineTokens.Contains) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();
            if (byTokens is not null) return byTokens.Catalog;
        }

        throw new InvalidOperationException(
            $"Could not match line '{line.Description}' to any PropertyItemCatalog. " +
            "Provide CatalogItemId explicitly or add a matching catalog entry.");
    }

    private static HashSet<string> Tokenize(string text) =>
        text.Split([' ', ',', '-', '/', '(', ')', '.'], StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 4)
            .Select(w => w.ToUpperInvariant())
            .ToHashSet();
}

