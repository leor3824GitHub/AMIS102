# Receiving Report — IAR Integration Plan

**Objective:** Refactor the AssetRegister receiving flow so both PPE Receiving Report (PPERR) and Supplies and Materials Receiving Report (SMRR) accept items from accepted Inspection & Acceptance Reports (IARs) sourced directly from the ProcurementAcquisition module via in-process Mediator queries — with document-kind-aware filtering, correct PPE vs. semi-expendable classification, auto-assigned property numbers, and full audit trail traceability.

**Date:** May 15, 2026  
**Status:** Planning  
**Modules Affected:** AssetRegister, ProcurementAcquisition

---

## 1. Overview & Rationale

### Current Flow

```
Manual Entry → Catalog Selection → Manual Property No Input → Receiving Report
```

### New Flow (Recommended)

```
ProcurementAcquisition (PO → IAR → Accepted)
    ↓ [in-process Mediator query from AssetRegister]
AssetRegister queries ProcurementAcquisition.Contracts directly (no event, no local copy)
    ↓ [PPERR shows PPE-only accepted items; SMRR shows SE-only accepted items]
    ↓
AssetRegistry (Assets registered with SourceIARId link for audit trail)
```

**Architecture note:** This is a modular monolith — all modules run in the same process. AssetRegister references `ProcurementAcquisition.Contracts` (public, allowed) and sends Mediator queries in-process. No HTTP calls, no event subscription, no local materialized copy of IAR data.

### Benefits

✅ Quality control enforced (items must pass inspection before receiving)  
✅ Full audit trail (items traced back to source IAR)  
✅ Auto-assigned property numbers (COA 2020-006 format, no manual entry)  
✅ Prevents duplicate registration (track "already received" items)  
✅ Shared receiving UI works for both PPERR and SMRR without cross-mixing PPE and SE items  
✅ Government compliance (follows procurement workflow)

---

## 1A. Inter-Office Transfer-In (Cross-Tenant)

**Clarification:** This plan is primarily for **new procurement** (`PO -> IAR -> PPERR/SMRR`).

For **inter-branch / inter-office transfer-in**, the project uses **1 tenant per office**. That means the sending office and receiving office are different tenants. Because of that:

- transfer-in is **not** a local reactivation of the same `AssetRegistry` row
- transfer-in is **not** sourced from `AssetIARAcceptedEvent`
- transfer-in should be sourced from the sending office's **posted issuance document**:
  - `PPEIR` for PPE
  - `SMIR` for semi-expendable

### Transfer-In Flow

```
Source Office Tenant
    ↓
Post PPEIR / SMIR
    ↓
Local asset marked TransferredOut in source tenant
    ↓ [cross-tenant integration event / transfer packet]
Destination Office Tenant
    ↓
Search inbound transfer candidates
    ↓
Create PPERR (PPE) or SMRR (SE) with ReceiptType.Transfer
    ↓
Materialize NEW AssetRegistry row in destination tenant
```

### Key Rule

Because each office is a separate tenant, the receiving office **must create a new destination-tenant asset row**. The source office keeps its historical row in `LifecycleState.TransferredOut`.

### Property Number Rule

For transfer-in, the destination tenant should **preserve the source office's existing PropertyNo / StockPropertyNo by default** rather than minting a procurement-style new number. This is a custody transfer, not a fresh acquisition.

If agency policy requires restickering or re-numbering across offices, make that an explicit destination-tenant option. Default recommendation: **preserve the transferred number**.

### Required Provenance Fields for Transfer-In

Do **not** overload `SourceIARId` for transfer receipts. Add separate provenance for issuance-based receipts, e.g.:

```csharp
Guid? SourceIssuanceReportId,
Guid? SourceIssuanceLineId,
Guid? SourceAssetRegistryId,
string? SourceTenantId,
string? SourceReportNo
```

### Recommended Design

Keep procurement and transfer as two sibling inbound sources:

- **Procurement inbound**: AssetRegister queries ProcurementAcquisition.Contracts via Mediator → PPERR/SMRR → new asset rows
- **Transfer inbound**: `IncomingTransferItemView` → PPERR/SMRR with `ReceiptType.Transfer` → new asset rows in destination tenant

---

## 2. Frontend Changes (COMPLETED ✅)

### File: `src/Playground/Playground.Blazor/Components/Pages/AssetRegister/ReceivingReportForm.razor`

**Changes Made:**

1. ✅ Updated line item display to show "From IAR" dropdown instead of catalog selection
2. ✅ Added auto-display of property number (read-only, generated on save)
3. ✅ Made description, quantity, unit cost, serial no, brand, model read-only (pulled from IAR)
4. ✅ Updated LineModel to track `IARLineItem`, `PropertyNumber`, `IARId`, `SourceLineItemId`
5. ✅ Added `SearchAcceptedIARItemsAsync()` method to fetch accepted IAR items
6. ✅ Updated save logic to pass `SourceIARId` to backend
7. ✅ Same form is reused by both PPERR and SMRR pages; backend must filter the accepted-IAR search by `DocumentKind`

**Frontend-Backend Contract:**

- Frontend calls: `POST /api/v1/asset-register/receiving` with `CreateReceivingReportRequest` containing `SourceIARId` for each item
- Frontend searches: `GET /api/v1/asset-register/receiving/accepted-iar-items?documentKind=PPERR|SMRR&searchTerm=X&pageNumber=1&pageSize=20`
- `PPERR` page must receive PPE-only accepted items; `SMRR` page must receive SE-only accepted items
- Expected response: `PagedResponse<AcceptedIARLineItemDto>` with flattened IAR line items already filtered for the requested receiving document kind

---

## 3. Backend Changes (TODO)

> Scope note: the detailed code samples below are for the **procurement / IAR-backed path**. Inter-office transfer-in is a second inbound flow and should be implemented as a sibling feature, not by forcing transfer data through `SourceIARId`.

### 3.1 Contracts Updates

**File:** `src/Modules/AssetRegister/Modules.AssetRegister.Contracts/v1/Receiving/`

#### New DTO: `AcceptedIARLineItemDto.cs`

```csharp
namespace AMIS.Modules.AssetRegister.Contracts.v1.Receiving;

/// <summary>
/// Represents a line item from an accepted IAR, ready for receiving.
/// Flattened view of AssetProcurement.AssetInspectionAcceptanceReport line items.
/// </summary>
public sealed record AcceptedIARLineItemDto(
    Guid Id,                    // Transient ID for UI row tracking
    Guid IARId,                 // AssetInspectionAcceptanceReport.Id
    string IARNumber,           // e.g., "IAR-2026-001" for dropdown display
    string Description,         // Item description
    DateOnly? AcquisitionDate,  // When acquired
    decimal Quantity,           // Quantity accepted
    decimal UnitCost,           // Unit price
    ReceivingDocumentKind TargetDocumentKind, // Auto-determined: PPERR if >= CapitalizationAmount, else SMRR
    AssetType AssetType,        // Auto-determined: PPE or SE
    AssetCategory Category,     // Auto-determined: PPE, HighValuedSemi, or LowValuedSemi
    string? SerialNo,           // Optional serial number
    string? Brand,              // Optional brand
    string? Model,              // Optional model
    string? PropertyClass);     // PropertyClassHint from IAR line item
```

#### Update: `CreateReceivingReportItemRequest.cs`

```csharp
namespace AMIS.Modules.AssetRegister.Contracts.v1.Receiving;

public sealed record CreateReceivingReportItemRequest(
    Guid CatalogItemId,         // Used server-side for catalog lookup only — NOT stored on the line item
    string? Reference,
    string Description,
    DateOnly AcquisitionDate,
    int Quantity,
    decimal UnitCost,
    string? SerialNo,
    string? Brand,
    string? Model,
    Guid? SourceIARId = null);  // ← NEW: Track source IAR for audit trail
                                // PropertyNos are auto-generated server-side; not sent by client.
```

#### New Query: `SearchAcceptedIARItemsQuery.cs`

```csharp
namespace AMIS.Modules.AssetRegister.Contracts.v1.Receiving;

using Mediator;
using AMIS.Framework.Shared.Persistence;

public sealed record SearchAcceptedIARItemsQuery(
    string? SearchTerm = null,
    ReceivingDocumentKind? DocumentKind = null,  // Optional: null = return all; PPERR or SMRR to filter
    int PageNumber = 1,
    int PageSize = 20)
    : IQuery<PagedResponse<AcceptedIARLineItemDto>>;

// Classification is auto-determined from MasterData CapitalizationThreshold:
// - UnitCost >= CapitalizationAmount              → PPERR (PPE asset)
// - UnitCost > SemiExpendableLowValueThreshold    → SMRR (HighValuedSemi)
// - UnitCost <= SemiExpendableLowValueThreshold   → SMRR (LowValuedSemi)
// DocumentKind filter (if provided) is applied AFTER auto-classification.
```

#### New additions to `ProcurementAcquisition.Contracts`: `AssetIARContracts.cs`

**File:** `src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition.Contracts/v1/AssetInspectionAcceptanceReports/AssetIARContracts.cs`

Add these two types. No other changes to existing ProcurementAcquisition contracts.

```csharp
/// <summary>
/// Flattened line item result for cross-module queries.
/// Used by AssetRegister to search accepted IAR items without a local materialized copy.
/// </summary>
public sealed record AcceptedIARLineItemResult(
    Guid IARId,
    string IARNumber,
    DateOnly IARDate,
    Guid PurchaseOrderId,
    string PoNumber,
    Guid SupplierId,
    string SupplierName,
    int ItemNo,
    string Description,
    string? Brand,
    string? Model,
    string? SerialNo,
    string? PropertyClassHint,
    string Unit,
    decimal Quantity,
    decimal UnitCost);

/// <summary>
/// Returns all accepted IAR line items (Status = Accepted), flattened across IARs.
/// Handled by ProcurementAcquisition; called in-process by AssetRegister via Mediator.
/// </summary>
public sealed record SearchAcceptedIARLineItemsQuery(
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 50) : IQuery<PagedResponse<AcceptedIARLineItemResult>>;
```

---

### 3.2 Domain Updates

**File:** `src/Modules/AssetRegister/Modules.AssetRegister/Domain/Receiving/ReceivingReportItem.cs`

```csharp
public sealed class ReceivingReportItem
{
    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public Guid CatalogItemId { get; private set; }

    /// <summary>
    /// NEW: Reference to source AssetInspectionAcceptanceReport.
    /// Enables traceability: Receiving → IAR → Purchase Order
    /// </summary>
    public Guid? SourceIARId { get; private set; }

    /// <summary>
    /// Supplier reference (SMRR) or IAR number (PPERR) for traceability.
    /// System-generated PropertyNumbers on materialized AssetRegistry rows are authoritative.
    /// </summary>
    public string? Reference { get; private set; }

    public string Description { get; private set; } = default!;
    public DateOnly AcquisitionDate { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal Amount => Quantity * UnitCost;
    public string? SerialNo { get; private set; }
    public string? Brand { get; private set; }
    public string? Model { get; private set; }

    private ReceivingReportItem() { }

    /// <summary>
    /// Factory method to create a receiving report line item.
    /// SourceIARId links this back to the originating inspection report.
    /// </summary>
    public static ReceivingReportItem Create(
        Guid catalogItemId,
        string? reference,
        string description,
        DateOnly acquisitionDate,
        int quantity,
        decimal unitCost,
        string? serialNo,
        string? brand,
        string? model,
        Guid? sourceIARId = null)  // ← NEW
    {
        if (catalogItemId == Guid.Empty)
            throw new ArgumentException("CatalogItemId is required");
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero");
        if (unitCost <= 0)
            throw new ArgumentException("UnitCost must be greater than zero");

        return new ReceivingReportItem
        {
            Id = Guid.NewGuid(),
            CatalogItemId = catalogItemId,
            Reference = reference,
            Description = description,
            AcquisitionDate = acquisitionDate,
            Quantity = quantity,
            UnitCost = unitCost,
            SerialNo = serialNo,
            Brand = brand,
            Model = model,
            SourceIARId = sourceIARId  // ← NEW
        };
    }
}
```

#### Update: `src/Modules/AssetRegister/Modules.AssetRegister/Domain/Receiving/ReceivingReport.cs`

```csharp
public class ReceivingReport : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    // ... existing fields ...

    /// <summary>
    /// Add item to receiving report. PropertyNos are the auto-generated agency numbers
    /// (one per unit), produced by IPropertyNumberGenerator before calling this method.
    /// </summary>
    public void AddItem(
        IReadOnlyList<string> propertyNos,  // Auto-generated agency property numbers
        string? reference,
        string description,
        DateOnly acquisitionDate,
        int quantity,
        decimal unitCost,
        string? serialNo,
        string? brand,
        string? model,
        Guid? sourceIARId = null)
    {
        _items.Add(ReceivingReportItem.Create(
            propertyNos, reference, description, acquisitionDate,
            quantity, unitCost, serialNo, brand, model, sourceIARId));
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
```

#### Update: `src/Modules/AssetRegister/Modules.AssetRegister/Domain/Assets/AssetRegistry.cs`

```csharp
public sealed class AssetRegistry : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    // ... existing fields ...

    /// <summary>
    /// Reference to source AssetInspectionAcceptanceReport (if registered from IAR).
    /// Enables audit trail: Asset → ReceivingReport → IAR → PurchaseOrder
    /// </summary>
    public Guid? SourceIARId { get; private set; }

    // ... existing code ...

    public static AssetRegistry Register(
        string tenantId,
        PropertyItemCatalog catalog,
        AssetType assetType,
        AssetCategory category,
        PropertyNumber propertyNo,
        string description,
        string? serialNo,
        string? brand,
        string? model,
        string fundCluster,
        DateOnly acquisitionDate,
        decimal unitCost,
        Guid? sourceIARId,              // ← NEW
        Guid? sourcePurchaseOrderId)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(propertyNo);

        if (unitCost <= 0)
            throw new InvalidOperationException("UnitCost must be greater than zero.");
        if (string.IsNullOrWhiteSpace(catalog.UacsObjectCode))
            throw new InvalidOperationException(
                "Catalog item must carry a UacsObjectCode before an asset can be registered against it.");

        var registry = new AssetRegistry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PropertyNo = propertyNo,
            ItemId = catalog.Id,
            AssetType = assetType,
            Category = category,
            PropertyClass = catalog.DefaultPropertyClass,
            CategoryCode = catalog.DefaultCategoryCode,
            Description = description,
            SerialNo = serialNo,
            Brand = brand,
            Model = model,
            Unit = catalog.DefaultUnit,
            FundCluster = fundCluster,
            UacsObjectCode = catalog.UacsObjectCode!,
            AcquisitionDate = acquisitionDate,
            UnitCost = unitCost,
            EstimatedUsefulLifeYears = catalog.EstimatedUsefulLifeYears,
            AccumulatedDepreciation = 0m,
            AccumulatedImpairmentLosses = 0m,
            LifecycleState = LifecycleState.Available,
            CurrentCondition = AssetCondition.InGoodCondition,
            SourceIARId = sourceIARId,              // ← NEW
            SourcePurchaseOrderId = sourcePurchaseOrderId,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        registry.AddDomainEvent(new AssetRegisteredEvent(
            registry.Id, propertyNo.Value, assetType, tenantId));
        return registry;
    }
}
```

---

### 3.3 Features / Command Handler

**File:** `src/Modules/AssetRegister/Modules.AssetRegister/Features/v1/Receiving/CreateReceivingReport/CreateReceivingReportCommandHandler.cs`

```csharp
public sealed class CreateReceivingReportCommandHandler(
    AssetRegisterDbContext db,
    IMediator mediator,
    IReceivingReportNumberGenerator reportNumbers,
    IPropertyNumberGenerator propertyNumbers)
    : ICommandHandler<CreateReceivingReportCommand, ReceivingReportDto>
{
    private const string DefaultLocationCode = "00";

    public async ValueTask<ReceivingReportDto> Handle(
        CreateReceivingReportCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var reportNumber = await reportNumbers.NextAsync(ct).ConfigureAwait(false);

        var report = ReceivingReport.Create(
            tenantId, reportNumber,
            cmd.DocumentKind, cmd.Date, cmd.ReceivedFrom, cmd.Address,
            cmd.ReceiptType, cmd.OtherReceiptType, cmd.FundCluster,
            cmd.ReceivedBy, cmd.NotedBy, cmd.DateReceived);

        var requestedAssetType = cmd.DocumentKind == ReceivingDocumentKind.PPERR
            ? AssetType.PPE
            : AssetType.SE;

        // Get active capitalization threshold from MasterData.
        var threshold = await mediator
            .Send(new GetActiveCapitalizationThresholdQuery(), ct)
            .ConfigureAwait(false);
        var capAmount   = threshold?.CapitalizationAmount           ?? 50_000m;
        var seLowAmount = threshold?.SemiExpendableLowValueThreshold ?? 5_000m;

        // Load all catalog items referenced in the command
        var catalogs = await db.PropertyItemCatalogs
            .Where(c => cmd.Items.Select(x => x.CatalogItemId).Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct)
            .ConfigureAwait(false);

        foreach (var line in cmd.Items)
        {
            if (!catalogs.TryGetValue(line.CatalogItemId, out var catalog))
                throw new KeyNotFoundException(
                    $"PropertyItemCatalog '{line.CatalogItemId}' not found.");

            if (!catalog.IsActive)
                throw new InvalidOperationException(
                    "Cannot receive items against a deactivated catalog item.");

            var (assetType, category) = ClassifyFor(line.UnitCost, capAmount, seLowAmount);
            if (assetType != requestedAssetType)
            {
                throw new InvalidOperationException(
                    $"Item '{line.Description}' (unit cost {line.UnitCost:N2}) is classified as {assetType} " +
                    $"and cannot be received on a {cmd.DocumentKind} document.");
            }

            // Auto-generate property numbers first (one per unit).
            // PropertyNo is the agency's unique identifier — generated before AddItem so the
            // domain entity can store them as the authoritative reference.
            var generatedPropertyNos = new List<string>();
            for (var unit = 0; unit < line.Quantity; unit++)
            {
                var propertyNo = await propertyNumbers.NextAsync(
                    assetType,
                    catalog.DefaultPropertyClass,
                    catalog.DefaultCategoryCode,
                    DefaultLocationCode,
                    line.AcquisitionDate,
                    ct).ConfigureAwait(false);

                generatedPropertyNos.Add(propertyNo.Value);

                var asset = AssetRegistry.Register(
                    tenantId, catalog, assetType, category, propertyNo,
                    line.Description, line.SerialNo, line.Brand, line.Model,
                    cmd.FundCluster, line.AcquisitionDate, line.UnitCost,
                    sourceIARId: line.SourceIARId,
                    sourcePurchaseOrderId: null);

                db.AssetRegistries.Add(asset);
            }

            // Add line to receiving report with generated PropertyNos and IAR traceability.
            report.AddItem(
                generatedPropertyNos,
                line.Reference,                    // IAR Number
                line.Description,
                line.AcquisitionDate,
                line.Quantity,
                line.UnitCost,
                line.SerialNo,
                line.Brand,
                line.Model,
                sourceIARId: line.SourceIARId);
        }

        db.ReceivingReports.Add(report);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ReceivingMapper.ToDto(report);
    }

    private static (AssetType assetType, AssetCategory category) ClassifyFor(
        decimal unitCost,
        decimal capitalizationAmount,
        decimal seLowValueThreshold)
    {
        if (unitCost >= capitalizationAmount)
            return (AssetType.PPE, AssetCategory.PPE);

        return unitCost > seLowValueThreshold
            ? (AssetType.SE, AssetCategory.HighValuedSemi)
            : (AssetType.SE, AssetCategory.LowValuedSemi);
    }
}
```

---

### 3.4 New Query Handler

**File:** `src/Modules/AssetRegister/Modules.AssetRegister/Features/v1/Receiving/SearchAcceptedIARItems/SearchAcceptedIARItemsHandler.cs`

> **Approach: Direct in-process Mediator query to ProcurementAcquisition**
>
> AssetRegister references `Modules.ProcurementAcquisition.Contracts` (public — allowed by architecture).
> The handler sends `SearchAcceptedIARLineItemsQuery` via Mediator. No HTTP. No event. No local copy.
> AssetRegister then filters by document kind and excludes items already registered.

Project references to add to `Modules.AssetRegister.csproj`:

```xml
<ProjectReference Include="..\..\ProcurementAcquisition\Modules.ProcurementAcquisition.Contracts\Modules.ProcurementAcquisition.Contracts.csproj" />
<ProjectReference Include="..\..\MasterData\Modules.MasterData.Contracts\Modules.MasterData.Contracts.csproj" />
```

```csharp
namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.SearchAcceptedIARItems;

using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using Mediator;
using Microsoft.EntityFrameworkCore;
using AMIS.Framework.Shared.Persistence;

public sealed class SearchAcceptedIARItemsHandler(
    IMediator mediator,
    AssetRegisterDbContext db,
    ILogger<SearchAcceptedIARItemsHandler> logger)
    : IQueryHandler<SearchAcceptedIARItemsQuery, PagedResponse<AcceptedIARLineItemDto>>
{
    public async ValueTask<PagedResponse<AcceptedIARLineItemDto>> Handle(
        SearchAcceptedIARItemsQuery query, CancellationToken ct)
    {
        // 1. Delegate to ProcurementAcquisition in-process via Mediator.
        //    Fetch a wider page to allow for document-kind filtering below.
        var procResult = await mediator.Send(
            new SearchAcceptedIARLineItemsQuery(
                SearchTerm: query.SearchTerm,
                PageNumber: query.PageNumber,
                PageSize: query.PageSize * 4),  // over-fetch to compensate for kind filter
            ct).ConfigureAwait(false);

        // 2. Get active capitalization threshold from MasterData (in-process Mediator).
        //    CapitalizationAmount  = PPE line (items ≥ this are PPE → PPERR)
        //    SemiExpendableLowValueThreshold = SE split (items ≤ this are LowValuedSemi)
        var threshold = await mediator
            .Send(new GetActiveCapitalizationThresholdQuery(), ct)
            .ConfigureAwait(false);
        var capAmount   = threshold?.CapitalizationAmount           ?? 50_000m;
        var seLowAmount = threshold?.SemiExpendableLowValueThreshold ?? 5_000m;

        // 3. Auto-classify every result item; then optionally filter by requested kind.
        var classified = procResult.Items
            .Select(x => (Item: x, Kind: x.UnitCost >= capAmount
                ? ReceivingDocumentKind.PPERR
                : ReceivingDocumentKind.SMRR))
            .ToList();

        var kindFiltered = query.DocumentKind.HasValue
            ? classified.Where(x => x.Kind == query.DocumentKind.Value).Select(x => x.Item).ToList()
            : classified.Select(x => x.Item).ToList();

        // 4. Exclude items already received (SourceIARId + ItemNo already in ReceivingReportItems).
        var iarIds = kindFiltered.Select(x => x.IARId).Distinct().ToList();
        var alreadyReceivedIarItemKeys = await db.ReceivingReportItems
            .Where(x => x.SourceIARId.HasValue && iarIds.Contains(x.SourceIARId!.Value))
            .Select(x => new { x.SourceIARId, x.SourceIARItemNo })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var receivedKeys = alreadyReceivedIarItemKeys
            .ToHashSet(x => (x.SourceIARId!.Value, x.SourceIARItemNo ?? 0));

        var pending = kindFiltered
            .Where(x => !receivedKeys.Contains((x.IARId, x.ItemNo)))
            .Take(query.PageSize)
            .ToList();

        // 5. Classify and map to DTOs.
        var dtos = pending.Select(x =>
        {
            var (assetType, category, targetKind) = Classify(x.UnitCost, capAmount, seLowAmount);
            return new AcceptedIARLineItemDto(
                Id: Guid.NewGuid(),   // transient — not persisted
                IARId: x.IARId,
                IARNumber: x.IARNumber,
                Description: x.Description,
                AcquisitionDate: x.IARDate,
                Quantity: x.Quantity,
                UnitCost: x.UnitCost,
                TargetDocumentKind: targetKind,
                AssetType: assetType,
                Category: category,
                SerialNo: x.SerialNo,
                Brand: x.Brand,
                Model: x.Model,
                PropertyClass: x.PropertyClassHint);
        }).ToList();

        logger.LogInformation(
            "SearchAcceptedIARItems: kind={Kind} — {Count} items (search: {Search}, threshold: {Cap})",
            query.DocumentKind?.ToString() ?? "All", dtos.Count, query.SearchTerm ?? "<none>", capAmount);

        return new PagedResponse<AcceptedIARLineItemDto>(dtos, pending.Count, query.PageNumber, query.PageSize);
    }

    /// <summary>
    /// Auto-determines document kind and asset classification from unit cost vs MasterData thresholds.
    /// No PropertyClassHint string matching — purely amount-based per COA Circular.
    /// </summary>
    private static (AssetType, AssetCategory, ReceivingDocumentKind) Classify(
        decimal unitCost,
        decimal capitalizationAmount,
        decimal seLowValueThreshold)
    {
        if (unitCost >= capitalizationAmount)
            return (AssetType.PPE, AssetCategory.PPE, ReceivingDocumentKind.PPERR);

        var cat = unitCost > seLowValueThreshold
            ? AssetCategory.HighValuedSemi
            : AssetCategory.LowValuedSemi;
        return (AssetType.SE, cat, ReceivingDocumentKind.SMRR);
    }
}
```

---

### 3.5 New Endpoint

**File:** `src/Modules/AssetRegister/Modules.AssetRegister/Features/v1/Receiving/SearchAcceptedIARItems/SearchAcceptedIARItemsEndpoint.cs`

```csharp
namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.SearchAcceptedIARItems;

using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using Mediator;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.AssetRegisterModuleConstants;

public static class SearchAcceptedIARItemsEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/accepted-iar-items", HandleAsync)
            .WithName("AssetRegister_SearchAcceptedIARItems")
            .WithSummary("Search accepted IAR line items ready for receiving")
            .WithDescription(
                "Returns line items from accepted Inspection & Acceptance Reports (IARs) " +
                "that are ready to be materialized into asset receiving reports. " +
                "Results are filtered by receiving document kind: PPERR returns PPE-only items, " +
                "while SMRR returns semi-expendable-only items. Items that have already been received are excluded.")
            .Produces<PagedResponse<AcceptedIARLineItemDto>>(StatusCodes.Status200OK)
            .RequirePermission(AssetRegisterPermissions.ReceivingReports.View);

    private static async Task<IResult> HandleAsync(
        IMediator mediator,
        ReceivingDocumentKind? documentKind,
        string? searchTerm,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new SearchAcceptedIARItemsQuery(documentKind, searchTerm, pageNumber, pageSize);
        var result = await mediator.Send(query, ct).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }
}
```

**Endpoint Registration in `AssetRegisterModule.cs`:**

```csharp
public void MapEndpoints(IEndpointRouteBuilder endpoints)
{
    // ... existing code ...

    var receiving = moduleGroup.MapGroup("/receiving");
    Features.v1.Receiving.CreateReceivingReport.CreateReceivingReportEndpoint.Map(receiving);
    Features.v1.Receiving.SearchAcceptedIARItems.SearchAcceptedIARItemsEndpoint.Map(receiving);  // ← NEW

    // ... rest of endpoints ...
}
```

---

### 3.6 Data / EF Configuration

> **No new domain entity or table in AssetRegister.** IAR data lives in ProcurementAcquisition and is queried in-process. The only EF changes here are two nullable `SourceIARId` / `SourceIARItemNo` columns for audit trail.

**File:** `src/Modules/AssetRegister/Modules.AssetRegister/Data/Configurations/ReceivingReportItemConfiguration.cs`

```csharp
internal sealed class ReceivingReportItemConfiguration : IEntityTypeConfiguration<ReceivingReportItem>
{
    public void Configure(EntityTypeBuilder<ReceivingReportItem> builder)
    {
        builder.ToTable("ReceivingReportItems", AssetRegisterModuleConstants.SchemaName);
        builder.HasKey(x => x.Id);

        // NEW: Audit trail — which IAR and line item did this come from?
        builder.Property(x => x.SourceIARId).IsRequired(false);
        builder.Property(x => x.SourceIARItemNo).IsRequired(false);  // ItemNo within the IAR

        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.AcquisitionDate).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.UnitCost).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.SerialNo).HasMaxLength(100);
        builder.Property(x => x.Brand).HasMaxLength(100);
        builder.Property(x => x.Model).HasMaxLength(100);

        builder.HasIndex(x => x.ReportId);
        builder.HasIndex(x => new { x.SourceIARId, x.SourceIARItemNo });  // for already-received check
    }
}
```

#### Update: `AssetRegistryConfiguration.cs`

**File:** `src/Modules/AssetRegister/Modules.AssetRegister/Data/Configurations/AssetRegistryConfiguration.cs`

Add index for `SourceIARId`:

```csharp
internal sealed class AssetRegistryConfiguration : IEntityTypeConfiguration<AssetRegistry>
{
    public void Configure(EntityTypeBuilder<AssetRegistry> builder)
    {
        // ... existing configuration ...

        // NEW: Track source IAR for audit trail
        builder.Property(x => x.SourceIARId)
            .HasColumnName("SourceIARId")
            .IsRequired(false);

        // ... existing properties ...

        // Indexes
        builder.HasIndex(x => new { x.TenantId, x.PropertyNo }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.AssetType });
        builder.HasIndex(x => x.SourceIARId);  // ← NEW: Query by source IAR
    }
}
```

> No changes to `AssetRegisterDbContext` — no new `DbSet` required.

---

### 3.7 ProcurementAcquisition — New Query Handler

**File:** `src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Features/v1/AssetIARs/SearchAcceptedIARLineItems/SearchAcceptedIARLineItemsHandler.cs`

This handler lives in ProcurementAcquisition. It owns the data; it answers the query.

```csharp
public sealed class SearchAcceptedIARLineItemsHandler(ProcurementDbContext db)
    : IQueryHandler<SearchAcceptedIARLineItemsQuery, PagedResponse<AcceptedIARLineItemResult>>
{
    public async ValueTask<PagedResponse<AcceptedIARLineItemResult>> Handle(
        SearchAcceptedIARLineItemsQuery query, CancellationToken ct)
    {
        var baseQuery = db.AssetIARs
            .AsNoTracking()
            .Where(x => x.Status == AssetIARStatus.Accepted)
            .SelectMany(iar => iar.LineItems
                .Where(l => l.InspectionResult != LineInspectionResult.Rejected)
                .Select(l => new AcceptedIARLineItemResult(
                    IARId: iar.Id,
                    IARNumber: iar.IarNumber,
                    IARDate: iar.IarDate,
                    PurchaseOrderId: iar.PurchaseOrderId,
                    PoNumber: iar.PoNumber,
                    SupplierId: iar.SupplierId,
                    SupplierName: iar.SupplierName,
                    ItemNo: l.ItemNo,
                    Description: l.Description,
                    Brand: l.Brand,
                    Model: l.Model,
                    SerialNo: l.SerialNo,
                    PropertyClassHint: l.PropertyClassHint,
                    Unit: l.Unit,
                    Quantity: l.Quantity,
                    UnitCost: l.UnitCost)));

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLower();
            baseQuery = baseQuery.Where(x =>
                x.Description.ToLower().Contains(term) ||
                x.IARNumber.ToLower().Contains(term));
        }

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        var items = await baseQuery
            .OrderByDescending(x => x.IARDate)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResponse<AcceptedIARLineItemResult>(items, total, query.PageNumber, query.PageSize);
    }
}
```

### 3.8 Mapper Updates

**File:** `src/Modules/AssetRegister/Modules.AssetRegister/Features/v1/Receiving/ReceivingMapper.cs`

```csharp
internal static class ReceivingMapper
{
    internal static ReceivingReportItemDto ToDto(this ReceivingReportItem item) =>
        new(
            item.Id,
            item.PropertyNos,       // Agency property numbers (PAR No. / StockPropertyNo per unit)
            item.Reference,
            item.Description,
            item.AcquisitionDate,
            item.Quantity,
            item.UnitCost,
            item.SerialNo,
            item.Brand,
            item.Model,
            SourceIARId: item.SourceIARId,
            SourceIARItemNo: item.SourceIARItemNo);
}
```

### 3.9 EF Core Migration

```bash
dotnet ef migrations add AddIARProvenance \
    --context AssetRegisterDbContext \
    --project src/Playground/Migrations.PostgreSQL \
    --output-dir Migrations/AssetRegister
```

**Migration adds only 4 columns — no new tables:**

```sql
-- ReceivingReportItems
ALTER TABLE asset_register."ReceivingReportItems"
    ADD COLUMN "SourceIARId" uuid NULL,
    ADD COLUMN "SourceIARItemNo" integer NULL;

CREATE INDEX idx_rri_source_iar
    ON asset_register."ReceivingReportItems"("SourceIARId", "SourceIARItemNo");

-- AssetRegistries
ALTER TABLE asset_register."AssetRegistries"
    ADD COLUMN "SourceIARId" uuid NULL;

CREATE INDEX idx_ar_source_iar
    ON asset_register."AssetRegistries"("SourceIARId");
```

---

## 4. Additional Design For Cross-Tenant Transfer-In

This is **separate** from the IAR-backed procurement flow above.

### Source Documents

- PPE transfer-in source: posted `PPEIR` from source office tenant
- SE transfer-in source: posted `SMIR` from source office tenant

### Destination Receiving Documents

- `PPERR` + `ReceiptType.Transfer` for PPE
- `SMRR` + `ReceiptType.Transfer` for SE

### Recommended Backend Shape

1. Publish a new cross-tenant integration payload when a source tenant posts `PPEIR` or `SMIR` for transfer.
2. Materialize that payload in the destination tenant as `IncomingTransferItemView`.
3. Add `SearchIncomingTransferItems` endpoint for the destination receiving form.
4. Extend receiving commands with issuance provenance fields (`SourceIssuanceReportId`, etc.).
5. On destination save, create a **new** `AssetRegistry` row in the destination tenant using the transferred snapshot.
6. Preserve transferred `PropertyNo` by default.

### Why This Is Different From Procurement

- Procurement creates assets from newly accepted goods.
- Transfer-in copies custody and asset identity from another tenant's already-existing asset record.
- The source-of-truth document is `PPEIR/SMIR`, not `IAR`.
- The source tenant's row remains historical and filtered as `TransferredOut`; the destination tenant gets its own local row.

## 6. Testing Checklist

### Unit Tests

- [ ] `CreateReceivingReportCommandHandler` passes `SourceIARId` + `SourceIARItemNo` correctly
- [ ] `AssetRegistry.Register()` accepts and stores `SourceIARId`
- [ ] `ReceivingReportItem.Create()` validates inputs
- [ ] `SearchAcceptedIARItemsHandler.Classify()` returns correct `AssetType`/`Category`/`DocumentKind`
- [ ] PPERR filter: only items with PPE `PropertyClassHint` returned
- [ ] SMRR filter: only items without PPE `PropertyClassHint` returned

### Integration Tests

- [ ] `SearchAcceptedIARLineItemsHandler` queries accepted IARs from ProcurementAcquisition
- [ ] `SearchAcceptedIARItemsHandler` delegates to ProcurementAcquisition and filters correctly
- [ ] Already-received items excluded from search results
- [ ] Pagination works in search
- [ ] Search term filters by description and IAR number

### API Tests

- [ ] `GET /api/v1/asset-register/receiving/accepted-iar-items?documentKind=PPERR` returns PPE-only items
- [ ] `GET /api/v1/asset-register/receiving/accepted-iar-items?documentKind=SMRR` returns SE-only items
- [ ] `?searchTerm=chair&pageNumber=1&pageSize=20` filters correctly
- [ ] Unauthorized access blocked (permission check)

### UI Tests (Blazor)

- [ ] Form loads without errors
- [ ] Dropdown searches and shows IAR items (correct kind per form)
- [ ] Selecting item populates description, quantity, unit cost (read-only)
- [ ] Property number shown as read-only
- [ ] Save creates receiving report with `SourceIARId` + `SourceIARItemNo` set

### Scenarios

**Scenario 1 — Happy path (PPE)**

```
1. Create PO + IAR in ProcurementAcquisition
2. Accept IAR → Status = Accepted
3. Open PPERR form → search shows only PPE items from that IAR
4. Select item → auto-fill description/qty/cost
5. Save → AssetRegistry row with SourceIARId set
6. Re-open PPERR search → that item no longer appears (already received)
```

**Scenario 2 — SMRR / SE only**

```
1. Accept IAR with SE items (PropertyClassHint = "FURN")
2. Open PPERR search → items do NOT appear (SE not PPE)
3. Open SMRR search → items DO appear
4. Confirm category: unitCost < 50,000 → LowValuedSemi; ≥ 50,000 → HighValuedSemi
```

**Scenario 3 — Mixed IAR (PPE + SE lines)**

```
1. IAR has line 1 PPE (PropertyClassHint = "EQUIP") and line 2 SE (PropertyClassHint = "FURN")
2. PPERR search shows only line 1
3. SMRR search shows only line 2
4. Receive line 1 on PPERR → re-search PPERR → line 1 gone; SMRR search → line 2 still visible
```

**Scenario 4 — Partial receive**

```
1. IAR has 3 line items
2. Receive lines 1 and 2 on separate receiving reports
3. Search still shows line 3
4. Receive line 3 → search shows nothing (all items received)
```

**Scenario 5 — No accepted IARs**

```
1. All IARs are in Draft/PendingInspection/Inspected/Cancelled status
2. Receiving search returns empty list with 200 OK
3. Form shows "No items found" message
```

**Scenario 6 — Cross-tenant transfer-in (future, separate track)**

```
1. Source office posts PPEIR → source assets go TransferredOut
2. Cross-tenant payload arrives at destination office tenant
3. Destination user creates PPERR with ReceiptType.Transfer
4. New AssetRegistry row created in destination tenant
5. SourceIssuanceReportId (not SourceIARId) carries provenance
```

---

## 7. Deployment Steps

1. **Database Migration**
   - Run migration on staging first
   - Verify all tables/indexes created
   - Back up production database
   - Run migration on production

2. **Backend Deployment**
   - Deploy updated API with new endpoints
   - Verify `/api/v1/asset-register/receiving/accepted-iar-items` responds

3. **Frontend Deployment**
   - Deploy updated Blazor app
   - Clear browser cache

4. **Validation**
   - Test full workflow end-to-end
   - Verify old receiving reports still load (backward compatible)
   - Confirm new items linked to IAR via `SourceIARId`

---

## 8. Rollback Plan

If issues arise:

1. **Database:** Reverse migration (create down script if needed)
2. **Code:** Revert to previous version
3. **API:** Restart service
4. **Frontend:** Clear cache, reload

**Note:** If `SourceIARId` is NULL, old data is still valid and system falls back gracefully.

---

## 9. Future Enhancements

- [ ] Batch receiving — select multiple IAR items at once
- [ ] Receiving vs. Acceptance workflow (split QC and receiving)
- [ ] Return-to-supplier workflow for rejected IAR items
- [ ] Auto-sync with procurement system for stock levels
- [ ] IAR → Receiving auto-scheduling (receive on date X)
- [ ] Dashboard showing "Pending Receiving" count by IAR

---

## Files Summary

### New Files to Create

```
# AssetRegister — Contracts
src/Modules/AssetRegister/Modules.AssetRegister.Contracts/v1/Receiving/
  └── AcceptedIARLineItemDto.cs
  └── SearchAcceptedIARItemsQuery.cs

# AssetRegister — Feature
src/Modules/AssetRegister/Modules.AssetRegister/Features/v1/Receiving/SearchAcceptedIARItems/
  └── SearchAcceptedIARItemsHandler.cs
  └── SearchAcceptedIARItemsEndpoint.cs

# ProcurementAcquisition — Feature (new query handler)
src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Features/v1/AssetIARs/SearchAcceptedIARLineItems/
  └── SearchAcceptedIARLineItemsHandler.cs
```

### Files to Modify

```
# ProcurementAcquisition — Contracts (add 2 records, no breaking changes)
src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition.Contracts/v1/AssetInspectionAcceptanceReports/
  └── AssetIARContracts.cs   ← add AcceptedIARLineItemResult + SearchAcceptedIARLineItemsQuery

# AssetRegister — Contracts
src/Modules/AssetRegister/Modules.AssetRegister.Contracts/v1/Receiving/
  └── CreateReceivingReportItemRequest.cs   ← add SourceIARId, SourceIARItemNo

# AssetRegister — Domain
src/Modules/AssetRegister/Modules.AssetRegister/Domain/Receiving/
  └── ReceivingReportItem.cs   ← add SourceIARId, SourceIARItemNo
  └── ReceivingReport.cs       ← pass through SourceIARItemNo

src/Modules/AssetRegister/Modules.AssetRegister/Domain/Assets/
  └── AssetRegistry.cs   ← add SourceIARId

# AssetRegister — Handlers / Mapper
src/Modules/AssetRegister/Modules.AssetRegister/Features/v1/Receiving/CreateReceivingReport/
  └── CreateReceivingReportCommandHandler.cs

src/Modules/AssetRegister/Modules.AssetRegister/Features/v1/Receiving/
  └── ReceivingMapper.cs

# AssetRegister — EF
src/Modules/AssetRegister/Modules.AssetRegister/Data/Configurations/
  └── ReceivingReportItemConfiguration.cs   ← SourceIARId, SourceIARItemNo columns + index
  └── AssetRegistryConfiguration.cs         ← SourceIARId column + index

# AssetRegister — Module wiring
src/Modules/AssetRegister/Modules.AssetRegister/
  └── AssetRegisterModule.cs   ← register new endpoint
  └── Modules.AssetRegister.csproj   ← add references to ProcurementAcquisition.Contracts + MasterData.Contracts

# Blazor
src/Playground/Playground.Blazor/ApiClient/
  └── IArReceivingReportClient.cs

src/Playground/Playground.Blazor/Components/Pages/AssetRegister/
  └── ReceivingReportForm.razor (ALREADY DONE)
```

### Migration

```
src/Playground/Migrations.PostgreSQL/Migrations/AssetRegister/
  └── {timestamp}_AddIARProvenance.cs
     — adds SourceIARId + SourceIARItemNo to ReceivingReportItems
     — adds SourceIARId to AssetRegistries
     — NO new tables
```

---

## Implementation Order

1. ✅ Frontend changes (DONE)
2. **Add `AcceptedIARLineItemResult` + `SearchAcceptedIARLineItemsQuery` to `ProcurementAcquisition.Contracts`** ← no breaking changes
3. **Add `SearchAcceptedIARLineItemsHandler` in ProcurementAcquisition** (owns the IAR data)
4. Add `AcceptedIARLineItemDto` + `SearchAcceptedIARItemsQuery` to `AssetRegister.Contracts`
5. Update `CreateReceivingReportItemRequest` (add `SourceIARId`, `SourceIARItemNo`)
6. Update domain models (`ReceivingReportItem`, `ReceivingReport`, `AssetRegistry`)
7. **Add project reference**: `Modules.AssetRegister.csproj` → `Modules.ProcurementAcquisition.Contracts`
8. Update `CreateReceivingReportCommandHandler`
9. **Create `SearchAcceptedIARItemsHandler`** (delegates to ProcurementAcquisition, filters, checks already-received)
10. Create `SearchAcceptedIARItemsEndpoint` + register in `AssetRegisterModule.cs`
11. Update EF configurations (`ReceivingReportItemConfiguration`, `AssetRegistryConfiguration`)
12. Update `ReceivingMapper`
13. Create and run migration (`AddIARProvenance`)
14. Update Blazor API client
15. Test all 5 scenarios
16. Deploy
