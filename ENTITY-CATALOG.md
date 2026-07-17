# AMIS Entity Catalog

> Auto-generated inventory of every domain entity, owned/child type, value object, enum, and persisted support type across all modules. Source of truth = the `Domain/` folders (plus a few persisted types outside `Domain/`). Use this as the working sheet for refactoring / (de)normalization decisions.

> **⚠️ Caveat — this documents the C# domain model, not the physical schema.** Everything below is taken from the `Domain/` classes as the source of truth. Actual database column names and types may differ where EF Core configurations remap them — owned-type column prefixes (e.g. `AssetSnapshot`, `EmployeeRef`, `Money`-style value objects), `xmin`/`Version` concurrency mappings, `.IsMultiTenant()` query filters, `HasColumnName` overrides, and table/schema names. Cross-check the matching `Data/Configurations/*.cs` (and the `Migrations.PostgreSQL` snapshot) before finalizing any column-level refactor.

## Cross-cutting conventions (legend)

Rather than repeat the same columns on every table, these interface-driven column sets are referenced by name per entity:

| Marker | Columns it adds | Notes |
|--------|-----------------|-------|
| `AggregateRoot<Guid>` | `Guid Id` (PK) + in-memory domain events | Aggregate root base. |
| `BaseEntity<Guid>` | `Guid Id` (PK) | Plain entity base (no domain events surfaced). |
| `IHasTenant` | `string TenantId` | Finbuckle tenant discriminator. **Not** auto-filtered unless the EF config calls `.IsMultiTenant()`. |
| `IAuditableEntity` | `DateTimeOffset CreatedOnUtc`, `string? CreatedBy`, `DateTimeOffset? LastModifiedOnUtc`, `string? LastModifiedBy` | Populated by the persistence audit interceptor. |
| `ISoftDeletable` | `bool IsDeleted`, `DateTimeOffset? DeletedOnUtc`, `string? DeletedBy` | Some entities expose these getter-only + a `SoftDelete(by)` method. |
| Concurrency | `byte[] Version` (app-managed / xmin), or Postgres `xmin` system column (no field) | Varies per entity — noted individually. |

**Property access legend:** most business properties are `{ get; private set; }` with mutation only through domain methods; audit fields are usually `{ get; set; }`. Deviations are called out.

**Module → schema / DbContext:** each module owns one DbContext and schema. Entities below are grouped by module.

---

## Table of contents

1. [MasterData](#1-masterdata)
2. [Expendable](#2-expendable)
3. [AssetRegister](#3-assetregister)
4. [ProcurementPlanning](#4-procurementplanning)
5. [ProcurementAcquisition](#5-procurementacquisition)
6. [BudgetDisbursement](#6-budgetdisbursement)
7. [Vehicle](#7-vehicle)
8. [Identity](#8-identity)
9. [Multitenancy](#9-multitenancy)
10. [Chat](#10-chat)
11. [Notifications](#11-notifications)
12. [Auditing](#12-auditing)
13. [Shared value objects (AssetRegister.Contracts)](#13-shared-value-objects)

---

## 1. MasterData

Reference data. Most entities here are **shared** (not `.IsMultiTenant()`) except `OrganizationProfile` and `ReportSignatory` which carry `TenantId`. All implement `AggregateRoot<Guid>` + `IAuditableEntity`. The simple lookup entities (`Category`, `Department`, `Office`, `Position`, `UnitOfMeasure`, `ModeOfProcurement`, `FundCluster`, `Supplier`, `FundingSourceCode`) share an identical audit + soft-delete + `byte[] Version` shape.

### Category
- Base: `AggregateRoot<Guid>`, `IAuditableEntity`
- Concurrency: `byte[] Version`; soft-delete via getter-only `IsDeleted/DeletedOnUtc/DeletedBy` + `SoftDelete`

| Property | Type | Notes |
|----------|------|-------|
| Code | string | |
| Name | string | |
| Description | string? | |
| OfficeCode | string? | owner office scoping |
| IsActive | bool | default true |
- Methods: `Create(code, name, description, officeCode?)`, `Update(code, name, description, isActive)`, `SoftDelete(deletedBy)`

### Department
- Base: `AggregateRoot<Guid>`, `IAuditableEntity` · `byte[] Version` · soft-delete

| Property | Type | Notes |
|----------|------|-------|
| Code | string | |
| Name | string | |
| Description | string? | |
| FundCluster | string? | |
| ResponsibilityCenterCode | string? | |
| OfficeCode | string? | |
| IsActive | bool | default true |
- Methods: `Create(code, name, description, fundCluster?, responsibilityCenterCode?, officeCode?)`, `Update(code, name, description, fundCluster, responsibilityCenterCode, isActive)`, `SoftDelete(deletedBy)`

### Office
- Base: `AggregateRoot<Guid>`, `IAuditableEntity` · `byte[] Version` · soft-delete

| Property | Type | Notes |
|----------|------|-------|
| Code | string | |
| Name | string | |
| Description | string? | |
| Address | string? | |
| LocationCode | string? | NFA 4-digit location code (e.g. 8300) |
| RegProvCode | string? | NFA Regional/Provincial code; null for Central Office |
| OfficeCode | string? | |
| IsActive | bool | default true |
- Methods: `Create(code, name, description, regProvCode?, locationCode?, address?, officeCode?)`, `Update(code, name, description, isActive, regProvCode?, locationCode?, address?)`, `SoftDelete(deletedBy)`

### Position
- Base: `AggregateRoot<Guid>`, `IAuditableEntity` · `byte[] Version` · soft-delete

| Property | Type | Notes |
|----------|------|-------|
| Code | string | |
| Name | string | |
| Description | string? | |
| OfficeCode | string? | |
| IsActive | bool | default true |
- Methods: `Create(code, name, description, officeCode?)`, `Update(code, name, description, isActive)`, `SoftDelete(deletedBy)`

### UnitOfMeasure
- Base: `AggregateRoot<Guid>`, `IAuditableEntity` · `byte[] Version` · soft-delete

| Property | Type | Notes |
|----------|------|-------|
| Code | string | |
| Name | string | |
| Description | string? | |
| OfficeCode | string? | |
| IsActive | bool | default true |
- Methods: `Create(code, name, description, officeCode?)`, `Update(...)`, `SoftDelete(deletedBy)`

### ModeOfProcurement
- Base: `AggregateRoot<Guid>`, `IAuditableEntity` · `byte[] Version` · soft-delete
- Note: **no Code** (Name is the identity)

| Property | Type | Notes |
|----------|------|-------|
| Name | string | |
| Description | string? | |
| IsActive | bool | default true |
- Methods: `Create(name, description?)`, `Update(name, description, isActive)`, `SoftDelete(deletedBy)`

### Supplier
- Base: `AggregateRoot<Guid>`, `IAuditableEntity` · `byte[] Version` · soft-delete

| Property | Type | Notes |
|----------|------|-------|
| Code | string | |
| Name | string | |
| TinNo | string? | |
| BusinessTaxType | string | "VAT" / "NON-VAT" (default NON-VAT), normalized |
| Description | string? | |
| ContactPerson | string? | |
| Email | string? | |
| Phone | string? | |
| Address | string? | |
| OfficeCode | string? | |
| IsActive | bool | default true |
- Methods: `Create(...)` (full + legacy overload), `Update(...)` (full + legacy overload), `SoftDelete(deletedBy)`, private `NormalizeBusinessTaxType`

### FundCluster
- Base: `AggregateRoot<Guid>`, `IAuditableEntity` · `byte[] Version` · soft-delete
- COA fund-cluster rollup (01–07), shared across tenants.

| Property | Type | Notes |
|----------|------|-------|
| Code | string | |
| Name | string | |
| Description | string? | |
| IsActive | bool | default true |
- Methods: `Create(code, name, description?)`, `Update(code, name, description, isActive)`, `SoftDelete(deletedBy)`

### FundingSourceCode
- Base: `AggregateRoot<Guid>`, `IAuditableEntity` · `byte[] Version` · soft-delete
- Full UACS funding-source taxonomy; rolls up to `FundCluster` via `FundClusterCode`.

| Property | Type | Notes |
|----------|------|-------|
| Code | string | 8-digit UACS code |
| FundClusterCode | string | FK-ish link to FundCluster.Code |
| FinancingSource | string? | |
| Authorization | string? | |
| FundCategory | string? | |
| FundSubCategory | string? | |
| Description | string? | |
| DepartmentName | string? | |
| AgencyName | string? | |
| IsActive | bool | default true |
- Methods: `Create(...)`, `Update(...)`, `SoftDelete(deletedBy)`

### EmployeeProfile
- Base: `AggregateRoot<Guid>`, `IAuditableEntity` · Concurrency: **xmin** (no Version field) · soft-delete getter-only + `SoftDelete`

| Property | Type | Notes |
|----------|------|-------|
| EmployeeNumber | string | |
| IdentityUserId | string? | link to Identity AmisUser |
| FirstName | string | |
| LastName | string | |
| WorkEmail | string? | |
| OfficeId | Guid | FK → Office |
| DepartmentId | Guid | FK → Department |
| PositionId | Guid | FK → Position |
| DefaultUnitOfMeasureId | Guid? | FK → UnitOfMeasure |
| OfficeCode | string? | owner office scoping |
| IsActive | bool | default true |
| Office | Office | nav (EF) |
| Department | Department | nav (EF) |
| Position | Position | nav (EF) |
| DefaultUnitOfMeasure | UnitOfMeasure? | nav (EF) |
- Methods: `Create(...)`, `LinkIdentity(userId)`, `UnlinkIdentity()`, `SetOwnerOfficeCode(code?)`, `Deactivate()`, `Activate()`, `Update(...)`, `SoftDelete(deletedBy)`

### OrganizationProfile
- Base: `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` · Concurrency: `byte[] Version` (random 8 bytes) · **no soft-delete**
- Per-tenant agency header + report signatory snapshot. Heavily denormalized (each officer stored as Id + Name + Designation triplet).

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| Name | string | |
| ShortName | string? | |
| Address | string? | |
| LogoUrl | string? | |
| AnnexECode | string? | 3-char office code for property-code gen |
| ApprovingOfficialId / …Name / …Designation | Guid?/string?/string? | |
| AssistantRegionalManagerId / …Name / …Designation | Guid?/string?/string? | |
| AccountantId / …Name / …Designation | Guid?/string?/string? | |
| SupervisingAdminOfficerId / …Name / …Designation | Guid?/string?/string? | |
| BudgetOfficerId / …Name / …Designation | Guid?/string?/string? | |
| PropertyCustodianId / …Name / …Designation | Guid?/string?/string? | |
- Methods: `Create(...)`, `Update(...)`, private `NewVersion()`
- **Normalization note:** 6 repeated (Id, Name, Designation) triplets — candidate for a child `OrgSignatory` table or reuse of `ReportSignatory`.

### ReportSignatory
- Base: `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` · `byte[] Version` · soft-delete fields are `{ get; set; }` (public) + `Delete(by)`

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| ReportType | string | which report the signatory belongs to |
| SortOrder | int | slot order |
| Label | string | |
| Name | string | |
| Title | string | |
| IsActive | bool | default true |
- Methods: `Create(...)`, `Update(...)`, `SetActive(isActive)`, `Delete(deletedBy)`, private `NewVersion()`

### CapitalizationThreshold
- Base: `AggregateRoot<Guid>`, `IAuditableEntity` · **no soft-delete, no Version** · shared (COA-set)

| Property | Type | Notes |
|----------|------|-------|
| CircularName | string | |
| Description | string | |
| CapitalizationAmount | decimal | PPE threshold (₱50,000) |
| SemiExpendableLowValueThreshold | decimal | low/high SE split (₱5,000) |
| EffectivityDate | DateOnly | |
| IsActive | bool | only one active |
- Methods: `Create(...)`, `Update(...)`, `Activate()`, `Deactivate()`

### PropertyClass
- Base: `AggregateRoot<Guid>`, `IAuditableEntity` · **no soft-delete, no Version** · shared · private ctor
- COA GAM Annex A top-level classification (PPRCLSCD).

| Property | Type | Notes |
|----------|------|-------|
| Code | string | 2-char (e.g. "OE"), upper-cased |
| Name | string | |
| Description | string? | |
| IsActive | bool | default true |
| Items | ICollection\<PropertyClassItem\> | children |
- Methods: `Create(code, name, description)`, `Update(name, description, isActive)`

### PropertyClassItem
- Base: `BaseEntity<Guid>`, `IAuditableEntity` · child of PropertyClass · private ctor

| Property | Type | Notes |
|----------|------|-------|
| PropertyClassId | Guid | FK → PropertyClass |
| ClassCode | string | denormalized 2-char parent code |
| ItemCode | string | 1-char category code, upper-cased |
| Name | string | |
| Description | string? | |
| IsActive | bool | default true |
| PropertyClass | PropertyClass | nav |
- Methods: `Create(propertyClassId, classCode, itemCode, name, description)`, `Update(itemCode, name, description, isActive)`

---

## 2. Expendable

Products, requests, cart, employee inventory, warehouse. All aggregates are `IHasTenant`; concurrency via **xmin** except where noted. Contains several **value-object/child** classes and enums.

### Enums
- `ProductStatus`: None, Active, Inactive, Discontinued, OutOfStock
- `SupplyRequestStatus`: None, Draft, Submitted, Approved, Rejected, Fulfilled, Cancelled
- `CartStatus`: None, Active, Converted, Abandoned, Cleared
- `ProductInventoryStatus`: None, Active, Discontinued, Archived

### Product
- Base: `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity`, `ISoftDeletable` (public settable audit + soft-delete)
- Self-referencing variant hierarchy.

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| StockNo | string | |
| Article | string | generic noun/class |
| Name | string | |
| Description | string | |
| UnitPrice | decimal | |
| UnitOfMeasure | string | free-text code (e.g. "PCS") |
| MinimumStockLevel | int | |
| ReorderQuantity | int | |
| Status | ProductStatus | default Active |
| CategoryId | string? | free-text FK (string!) |
| SupplierId | string? | free-text FK (string!) |
| ParentProductId | Guid? | variant parent |
| VariantName | string? | e.g. "A4" |
| ImageUrl | string? | storage key |
| ThumbnailUrl | string? | storage key |
| ParentProduct | Product? | nav |
| Variants | ICollection\<Product\> | nav |
- Methods: `Create(...)`, `CreateVariant(...)`, `Activate()`, `Deactivate()`, `Discontinue()`, `MarkOutOfStock()`, `Update(...)`, `SetImage(imageKey, thumbnailKey?)`, `ClearImage()`, `SetCategory(id?)`, `SetSupplier(id?)`, `SetVariantName(name?)`, `SoftDelete(deletedBy)`
- **Normalization note:** `CategoryId`/`SupplierId` are `string?` here but Guid elsewhere (MasterData). `UnitOfMeasure` is free text vs. the UnitOfMeasure entity.

### ProductRating
- Base: `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` · one row per (tenant, product, rater) · private ctor

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| ProductId | Guid | FK → Product |
| RaterUserId | string | Identity user id |
| Value | int | clamped 1–5 (MinValue/MaxValue consts) |
- Methods: `Create(tenantId, productId, raterUserId, value)`, `UpdateValue(value)`, private `Clamp`

### SupplyRequest (aggregate) + SupplyRequestItem (child)
- Base: `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity`, `ISoftDeletable` · xmin concurrency

**SupplyRequest**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| RequestNumber | string | |
| EmployeeId | string | |
| DepartmentId | string | `{ get; set; }` (public!) |
| RequestDate | DateTimeOffset | |
| NeededByDate | DateTimeOffset? | |
| Status | SupplyRequestStatus | default Draft |
| BusinessJustification | string? | |
| RejectionReason | string? | |
| ApprovedBy | string? | |
| ApprovedOnUtc | DateTimeOffset? | |
| FulfilledOnUtc | DateTimeOffset? | |
| WarehouseLocationId | Guid? | |
| Items | IReadOnlyCollection\<SupplyRequestItem\> | backing list |
- Methods: `Create(...)`, `AddItem(productId, qty, notes?)`, `RemoveItem(productId)`, `Submit()`, `Approve(approvedBy, approvedQuantities, warehouseLocationId)`, `Reject(reason?)`, `MarkFulfilled()`, `Fulfill(fulfillmentDetails)`, `Cancel()`, `SoftDelete(deletedBy)`

**SupplyRequestItem** (plain child, mutable public props)

| Property | Type | Notes |
|----------|------|-------|
| ProductId | Guid | |
| RequestedQuantity | int | |
| ApprovedQuantity | int | |
| FulfilledQuantity | int | |
| FulfilledValue | decimal | |
| Notes | string? | |

### EmployeeShoppingCart (aggregate) + CartItem (child)
- Base: `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity`, `ISoftDeletable` · xmin

**EmployeeShoppingCart**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| EmployeeId | string | |
| Status | CartStatus | `{ get; set; }` default Active |
| ConvertedOnUtc | DateTimeOffset? | `{ get; set; }` |
| ConvertedToRequestId | Guid? | `{ get; set; }` |
| Items | IReadOnlyCollection\<CartItem\> | backing list |
- Methods: `Create(...)`, `AddItem(productId, qty, unitPrice)`, `RemoveItem(productId)`, `UpdateItemQuantity(productId, newQty)`, `GetCartTotal()`, `GetTotalItemCount()`, `ConvertToRequest(supplyRequestId)`, `Clear()`, `SoftDelete(deletedBy)`

**CartItem** (child)

| Property | Type | Notes |
|----------|------|-------|
| ProductId | Guid | |
| Quantity | int | |
| UnitPrice | decimal | |
| AddedOnUtc | DateTimeOffset | |
| LineTotal | decimal | computed (Qty × UnitPrice) |
- Methods: `UpdateQuantity(qty)`

### EmployeeInventory (aggregate) + EmployeeStockBatch (child) + InventoryConsumption (separate entity)
- File: `Inventory/EmployeeInventory.cs`

**EmployeeInventory** — `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` · xmin · **no soft-delete**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| EmployeeId | string | |
| ProductId | Guid | |
| TotalQuantityReceived | int | `{ get; set; }` |
| TotalQuantityConsumed | int | `{ get; set; }` |
| QuantityOnHand | int | computed |
| Batches | IReadOnlyCollection\<EmployeeStockBatch\> | backing list |
- Methods: `Create(...)`, `ReceiveInventory(qty, batchNumber?, expiryDate?)`, `ConsumeInventory(qty)` (FIFO across batches), `GetAvailableBatches()`, `GetExpiredBatches()`

**EmployeeStockBatch** (child; mutable public props + private ctor)

| Property | Type | Notes |
|----------|------|-------|
| Id | Guid | |
| ProductId | Guid | |
| QuantityReceived | int | |
| QuantityConsumed | int | |
| QuantityAvailable | int | computed |
| ReceivedOnUtc | DateTimeOffset | |
| BatchNumber | string? | |
| ExpiryDate | DateTimeOffset? | |
| IsExpired | bool | computed |
- Methods: `Consume(qty)`

**InventoryConsumption** — `BaseEntity<Guid>`, `IHasTenant`, `IAuditableEntity` (audit trail row; mutable public props)

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| EmployeeInventoryId | Guid | |
| ProductId | Guid | |
| EmployeeId | string | |
| QuantityConsumed | int | |
| Reason | string? | |
| ReferenceNumber | string? | |
| ConsumptionDate | DateTimeOffset | |
- Methods: `Create(...)`

### ProductInventory (aggregate) + WarehouseReceiptBatch (child) + IssuanceDetail (DTO)
- File: `Warehouse/ProductInventory.cs` · `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity`, `ISoftDeletable` · xmin · private ctor
- Moving-average warehouse ledger, one row per (product, warehouse location).

**ProductInventory**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| ProductId | Guid | (name resolved via join, not snapshotted) |
| WarehouseLocationId | Guid | |
| QuantityAvailable | int | ready to issue |
| QuantityReserved | int | allocated to requests |
| QuantityIssued | int | total ever issued |
| QuantityOnHand | int | computed (avail+reserved) |
| TotalValue | decimal | moving-average value |
| AverageUnitPrice | decimal | computed |
| ReservedValue | decimal | computed |
| Batches | Collection\<WarehouseReceiptBatch\> | receipt ledger |
| FirstReceiptDate / LastReceiptDate / LastIssueDate | DateTimeOffset? | |
| Status | ProductInventoryStatus | |
- Methods: `Create(...)`, `ReceiveFromPurchase(sourceReceiptId, productId, qtyAccepted, unitPrice, sourceReference?)`, `ReserveForAllocation(qty)`, `CancelReservation(qty)`, `IssueReservedStock(qty)` → IssuanceDetail, `Discontinue()`; `AvailableForAllocation` computed

**WarehouseReceiptBatch** (child; private ctor, private-set props) — append-only receipt row

| Property | Type | Notes |
|----------|------|-------|
| Id | Guid | PK of ProductInventoryBatches |
| PurchaseId | Guid | source receipt id |
| ProductId | Guid | |
| QuantityAvailable | int | qty received in batch |
| UnitPrice | decimal | |
| SourceReference | string? | e.g. IAR number |
| ReceivedDate | DateTimeOffset | |
- Methods: `Create(...)`

**IssuanceDetail** (plain return DTO): ProductId, QuantityIssued, UnitPrice, TotalValue

---

## 3. AssetRegister

The largest module. Unified SE + PPE tracking, depreciation, accountability (ICS/PAR), receiving, issuance, incidents, unserviceable/disposal, physical count, returns, repairs, locations, signed docs. All aggregates `IHasTenant`. Enums/value objects live mostly in `AssetRegister.Contracts.v1` (see [§13](#13-shared-value-objects)).

### AssetRegistry
- Base: `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` · **no soft-delete** (lifecycle-state driven) · private ctor
- The canonical asset record (both SE and PPE).

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| PropertyNo | PropertyNumber (VO) | owned type |
| AssetType | AssetType | SE / PPE |
| Category | AssetCategory | |
| PropertyClass | string | classification code |
| CategoryCode | string | |
| Description | string | |
| SerialNo / Brand / Model | string? | |
| Unit | string | |
| ImageUrl / ThumbnailUrl | string? | storage keys |
| FundCluster | string | |
| UacsObjectCode | string | |
| AcquisitionDate | DateOnly | |
| UnitCost | decimal | |
| EstimatedUsefulLifeYears | int | |
| AccumulatedDepreciation | decimal | |
| AccumulatedImpairmentLosses | decimal | |
| CarryingAmount | decimal | computed |
| ResidualValue | decimal | PPE only |
| DepreciationMethod | DepreciationMethod | |
| DepreciationStartDate | DateOnly | |
| DepreciatedThrough | DateOnly? | |
| IsFullyDepreciated | bool | computed |
| LifecycleState | LifecycleState | Available/Assigned/… |
| CurrentCondition | AssetCondition | |
| CurrentCustodianId / CurrentLocationId / CurrentAccountabilityId | Guid? | |
| SourceIARId / SourcePurchaseOrderId | Guid? | provenance |
- Methods: `Register(...)`, `AssignTo(accountabilityId, custodianId, locationId)`, `Transfer(...)`, `ReturnToAvailable()`, `MarkUnderInvestigation(incidentReportId)`, `MarkMissingFromCount(sessionId)`, `RecordFoundAtStation(sessionId, entryId)`, `MarkRecovered(incidentReportId)`, `MarkUnserviceable(reportId)`, `MarkTransferredOut(issuanceReportId, reportNo, reportType)`, `Dispose(reportId, method)`, `RecordImpairment(amount, reason)`, `RecordDepreciation(amount)`, `MonthlyDepreciation()`, `UpdateDepreciation(residual, life, method)`, `PostDepreciation(period, scheduledAmount)`, `UpdateCondition(condition)`, `SetImage(imageKey, thumbKey)`, `ClearImage()`, `Snapshot()` → AssetSnapshot
- Raises many domain events (see [domain events](#assetregister-domain-events)).

### DepreciationEntry
- Base: `IHasTenant` **only** (plain class, private ctor) — append-only monthly PPE ledger row

| Property | Type | Notes |
|----------|------|-------|
| Id | Guid | |
| TenantId | string | |
| AssetRegistryId | Guid | FK |
| Period | DateOnly | first of month; unique (Tenant, Asset, Period) |
| Amount | decimal | |
| AccumulatedDepreciationAfter | decimal | |
| CarryingAmountAfter | decimal | |
| PostedOnUtc | DateTimeOffset | |
- Methods: `Create(...)`

### PropertyItemCatalog
- Base: `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity`, `ISoftDeletable` (public settable) · private ctor
- Reusable item type + depreciation policy template.

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| Code | string | |
| Description | string | |
| DefaultPropertyClass | string | |
| DefaultCategoryCode | string | |
| DefaultUnit | string | |
| UacsObjectCode | string? | null until certified |
| EstimatedUsefulLifeYears | int | |
| ResidualValuePercent | decimal | default 5% |
| DepreciationMethod | DepreciationMethod | default StraightLine |
| IsActive | bool | |
| Status | CatalogItemStatus | Draft/Ready |
- Methods: `Create(...)`, `Update(...)`, `BackfillUacs(uacs)`, `Deactivate()`, `Reactivate()`

### PropertyCodeCounter
- Base: `AggregateRoot<Guid>`, `IHasTenant` · private ctor · optimistic-concurrency counter
- Unique key (TenantId, Year, Month, CounterKey).

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| Year | int | |
| Month | int | |
| CounterKey | string | SPLV, SPHV, PAR, PPE-*, ITR, RLSDDSP, IIRUSP, IIRUP, RSPI |
| LastSerial | int | |
- Methods: `Create(...)`, `NextSerial()`

### PropertyAccountability (aggregate) + PropertyAccountabilityLine (child) + VehicleAccountabilityProfile (owned)
- ICS (SE) / PAR (PPE) issuance document. `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` · **no soft-delete** · private ctor

**PropertyAccountability**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| DocumentNo | string | |
| AccountabilityType | AccountabilityType | SE_ICS / PPE_PAR |
| FundCluster | string | |
| IssuedOn | DateOnly | |
| ExpiresOn | DateOnly? | PAR = issued + 3y (ParRenewalYears const) |
| Status | AccountabilityStatus | PendingAcceptance/Active/… |
| CancellationReason | string? | |
| AcceptedOn | DateOnly? | |
| SupersededByAccountabilityId / SupersedesAccountabilityId | Guid? | renewal chain |
| IssuedBy | EmployeeRef (VO) | owned |
| ReceivedBy | EmployeeRef (VO) | owned |
| Lines | IReadOnlyCollection\<PropertyAccountabilityLine\> | |
- Methods: `Issue(...)`, `Accept(acceptedOn)`, `UpdateHeader(...)`, `AddLine(asset, itemNo, rcCode?, vehicleProfile?)`, `RemoveLine(lineId)`, `EnsureDeletableDraft()`, `Renew(newDocNo, newIssuedOn, newExpiresOn?)`, `ReturnLines(...)` (2 overloads), `ReportLineLost(lineId, incidentReportId)`, `Cancel(reason)`

**PropertyAccountabilityLine** (child; `IHasTenant`; private ctor)

| Property | Type | Notes |
|----------|------|-------|
| Id / TenantId / AccountabilityId / AssetRegistryId | Guid/string | |
| Snapshot | AssetSnapshot (VO) | owned |
| SnapshotItemNo | string | |
| SnapshotResponsibilityCenterCode | string? | |
| IssuedQty / ReturnedQty | int | |
| LineStatus | AccountabilityLineStatus | |
| ReturnedOn | DateOnly? | |
| ReturnedConditionAtReturn | AssetCondition? | |
| LostOnIncidentId | Guid? | |
| VehicleProfile | VehicleAccountabilityProfile? | owned |
- Methods (internal): `Create(...)`, `MarkReturned(...)`, `MarkLost(...)`

**VehicleAccountabilityProfile** (owned type on line; private ctor)

| Property | Type | Notes |
|----------|------|-------|
| OdometerAtIssue / OdometerAtReturn | int? | |
| PlateNumber / EngineNumber / ChassisNumber | string? | |
- Methods (internal): `Create(...)`, `RecordReturn(odometerAtReturn)`

### ReceivingReport (aggregate) + ReceivingReportItem (child)
- PPERR / SMRR receiving. `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` · no soft-delete · private ctor

**ReceivingReport**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| DocumentKind | ReceivingDocumentKind | PPERR / SMRR |
| ReportNo | string | |
| Date | DateOnly | |
| ReceivedFrom | string | |
| Address | string? | |
| ReceiptType | ReceiptType | |
| OtherReceiptType | string? | |
| FundCluster | string? | |
| ReceivedBy | EmployeeRef (VO) | owned |
| NotedBy | EmployeeRef? (VO) | owned |
| DateReceived | DateOnly? | |
| Items | IReadOnlyCollection\<ReceivingReportItem\> | |
- Methods: `Create(...)`, `AddItem(...)`

**ReceivingReportItem** (child; `IHasTenant`; private ctor)

| Property | Type | Notes |
|----------|------|-------|
| Id / TenantId / ReportId / CatalogItemId | | |
| Reference | string? | |
| PropertyNo | string | |
| Description | string | |
| AcquisitionDate | DateOnly | |
| Quantity | int | |
| UnitCost | decimal | |
| Amount | decimal | computed |
| SerialNo / Brand / Model | string? | |
| UacsObjectCode | string? | |
| SourceAgencyName / SourcePropertyNo / SourceDocumentRef | string? | external-source fields |
| OriginalAcquisitionDate | DateOnly? | depreciation continuity |
- Methods (internal): `Create(...)`

### PPERRFormSeries
- Base: `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` · pre-numbered accountable-form batch · private ctor

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| StartSerial / EndSerial / NextSerial | int | |
| IsActive | bool | |
| IsExhausted / Remaining / IsUnused | computed | |
- Methods: `Create(...)`, `UpdateRange(...)`, `Activate()`, `Deactivate()`, `AllocateNext()`

### PropertyIssuanceReport (aggregate) + PropertyIssuanceReportLine (child)
- SMIR / PPEIR transfer-out doc. `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` · no soft-delete · private ctor

**PropertyIssuanceReport**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| ReportNo | string | |
| ReportType | IssuanceReportType | SMIR / PPEIR |
| FundCluster | string | |
| Date | DateOnly | |
| Nature | IssuanceNature | |
| IssuedBy / ApprovedBy / IssuedTo | EmployeeRef (VO) | owned |
| IssuedToOfficeAddress | string | |
| Remarks | string? | |
| Lines | IReadOnlyCollection\<PropertyIssuanceReportLine\> | |
- Methods: `Create(...)`, `AddLine(assetRegistryId, snapshot, unitCost)`, `MarkIssued()`, `SetLineDepreciation(lineId, accDep, bookValue)`

**PropertyIssuanceReportLine** (child; `IHasTenant`; private ctor)

| Property | Type | Notes |
|----------|------|-------|
| Id / TenantId / ReportId / AssetRegistryId | | |
| ItemNo | int | |
| Snapshot | AssetSnapshot (VO) | owned |
| SnapshotUnitCost / SnapshotAmount | decimal | |
| AccumulatedDepreciation / BookValue | decimal? | PPEIR only |
- Methods: `Create(...)` (internal), `SetDepreciation(accDep, bookValue)`

### PPEIRFormSeries
- Identical shape to **PPERRFormSeries** (pre-numbered PPEIR forms): TenantId, StartSerial, EndSerial, NextSerial, IsActive + computed IsExhausted/Remaining/IsUnused; same methods.

### PropertyIncidentReport (aggregate) + PropertyIncidentItem (child)
- RLSDDSP (lost/stolen/damaged). `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` · no soft-delete · private ctor

**PropertyIncidentReport**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| IncidentNo | string | |
| IncidentType | PropertyIncidentType | |
| IncidentDate | DateOnly | |
| FundCluster / DepartmentOffice / Circumstances | string | |
| AccountableOfficer | EmployeeRef (VO) | owned |
| AccountableOfficerDesignation | string | |
| AccountableOfficerGovIdType / …GovIdNo | string? | |
| AccountableOfficerGovIdIssuedOn | DateOnly? | |
| NotedBy | EmployeeRef? (VO) | owned |
| PoliceNotified | bool | + PoliceStation, PoliceNotifiedOn, PoliceBlotterRef |
| NotarizedOn | DateOnly? | + NotaryDocNo/PageNo/BookNo/SeriesOf |
| Status | PropertyIncidentStatus | |
| ReliefRequestedOn / ReliefGrantedOn | DateOnly? | + ReliefGrantedRef |
| AmountSettled | decimal? | + SettledOn |
| RecoveredOn | DateOnly? | |
| Items | IReadOnlyCollection\<PropertyIncidentItem\> | |
- Methods: `File(...)`, `NotifyPolice(...)`, `Notarize(...)`, `RecordRecovery(...)`, `RecordSettlement(...)`, `GrantRelief(...)`, `MarkDerecognized(...)`, `Close()`

**PropertyIncidentItem** (child; `IHasTenant`; private ctor)

| Property | Type | Notes |
|----------|------|-------|
| Id / TenantId / ReportId / AssetRegistryId | | |
| Snapshot | AssetSnapshot (VO) | owned |
| SnapshotAcquisitionCost / SnapshotCurrentReplacementCost | decimal | |
| AccountabilityLineId | Guid? | |
| ItemResolution | IncidentItemResolution | Pending/Recovered/Paid/… |
| ResolvedOn | DateOnly? | |
- Methods (internal): `Create(...)`, `Resolve(...)`

### UnserviceablePropertyReport (aggregate) + UnserviceablePropertyItem (child)
- IIRUSP / IIRUP (unserviceable + disposal). `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` · no soft-delete · private ctor

**UnserviceablePropertyReport**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| ReportNo | string | |
| ReportType | UnserviceableReportType | IIRUSP / IIRUP |
| AsAt | DateOnly | |
| FundCluster / Station | string | |
| Status | UnserviceableReportStatus | Draft…Closed |
| AccountableOfficer | EmployeeRef (VO) | owned |
| ApprovedBy / InspectedBy / WitnessedBy | EmployeeRef? (VO) | owned |
| InspectedOn / WitnessedOn | DateOnly? | |
| Items | IReadOnlyCollection\<UnserviceablePropertyItem\> | |
- Methods: `CreateDraft(...)`, `UpdateHeader(...)`, `AddItem(asset, remarks?)`, `Submit(approvedBy)`, `RecordInspection(...)`, `RecordDisposal(...)`, `Close()`

**UnserviceablePropertyItem** (child; `IHasTenant`; private ctor)

| Property | Type | Notes |
|----------|------|-------|
| Id / TenantId / ReportId / AssetRegistryId | | |
| Snapshot | AssetSnapshot (VO) | owned |
| SnapshotDateAcquired | DateOnly | |
| SnapshotAcquisitionCost / …AccumulatedDepreciation / …AccumulatedImpairmentLosses | decimal | |
| SnapshotCarryingAmount | decimal | computed |
| Remarks | string? | |
| DisposalMethod | DisposalMethod? | + DisposalOtherSpecify |
| AppraisedValue | decimal? | |
| DisposalRecordedOn | DateOnly? | |
| SaleORNo | string? | + SaleAmount |
- Methods (internal): `Create(...)`, `RecordInspectionDecision(...)`, `RecordDisposal(...)`

### PhysicalCountSession (aggregate) + PhysicalCountEntry (child)
- `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` · no soft-delete · private ctor

**PhysicalCountSession**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| Code | string | |
| Scope | PhysicalCountScope | Both/SEOnly/PPEOnly |
| Status | PhysicalCountStatus | Draft/Ongoing/Reconciled/Closed |
| FundCluster | string | |
| StartedOn | DateOnly | + ClosedOn?, AsAt |
| Remarks | string? | |
| OfficeOrderNo | string? | |
| FrozenOnUtc | DateTimeOffset? | |
| ConductedBy | IReadOnlyCollection\<EmployeeRef\> | owned collection |
| ApprovedBy / WitnessedBy | EmployeeRef? (VO) | owned |
| Entries | IReadOnlyCollection\<PhysicalCountEntry\> | |
- Methods: `Start(...)`, `Freeze(officeOrderNo, frozenOnUtc)`, `RequestRecount(entryId, reason?)`, `RecordEntry(...)`, `AddFoundAtStationEntry(...)`, `MarkMissing(asset, locationId, remarks?)`, `Reconcile()`, `Close(approvedBy, witnessedBy?, closedOn)`, `AttachReconciledAssetToEntry(...)` (internal)

**PhysicalCountEntry** (child; `IHasTenant`; private ctor)

| Property | Type | Notes |
|----------|------|-------|
| Id / TenantId / SessionId | | |
| AssetRegistryId | Guid? | null for FoundAtStation |
| Snapshot | AssetSnapshot? (VO) | owned |
| SnapshotArticle / SnapshotUnit | string | |
| SnapshotUnitCost | decimal | |
| Condition | PhysicalCountCondition | |
| ScannedOnUtc | DateTimeOffset? | |
| PhotoPath | string? | |
| ScannedByEmployeeId | Guid? | |
| LocationId | Guid | |
| Remarks | string? | |
| ProposedPropertyClass / ProposedCategoryCode / ProposedPropertyNo | string? | FoundAtStation only |
| ProposedAcquisitionDate | DateOnly? | |
| ProposedUnitCost | decimal? | |
| ProposedCatalogItemId | Guid? | |
| NeedsRecount | bool | + RecountReason, RecountRequestedOnUtc |
- Methods (internal): `CreateForKnownAsset(...)`, `CreateFoundAtStation(...)`, `AttachReconciledAsset(...)`, `MarkForRecount(...)`, `ApplyRecount(...)`

### ReturnedPropertyReceipt (aggregate) + ReturnedPropertyReceiptItem (child)
- RRSP / RRP return workflow. `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` · no soft-delete · private ctor

**ReturnedPropertyReceipt**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| ReceiptNo | string? | null while Pending |
| ReceiptType | ReturnedPropertyReceiptType | |
| Status | ReturnedPropertyReceiptStatus | Pending/Inspected/Accepted/… |
| Date | DateOnly | |
| AccountabilityId | Guid | |
| AccountabilityDocumentNo | string | snapshot |
| ReturnedBy / AssignedInspector | EmployeeRef (VO) | owned |
| InspectedBy / ReceivedBy | EmployeeRef? (VO) | owned |
| Remarks / InspectionRemarks / RejectionReason / CancellationReason | string? | |
| InspectedOnUtc / AcceptedOnUtc / ResolvedOnUtc | DateTimeOffset? | |
| Items | IReadOnlyCollection\<ReturnedPropertyReceiptItem\> | |
- Methods: `Create(...)`, `AddItem(...)`, `ReassignInspector(...)`, `Inspect(...)`, `Accept(receiptNo, receivedBy)`, `Reject(reason)`, `Cancel(reason?)`

**ReturnedPropertyReceiptItem** (child; `IHasTenant`; private ctor)

| Property | Type | Notes |
|----------|------|-------|
| Id / TenantId / ReceiptId / AccountabilityLineId / AssetRegistryId | | |
| ItemNo | int | |
| Snapshot | AssetSnapshot (VO) | owned |
| InspectedCondition | AssetCondition? | null until inspected |
- Methods (internal): `Create(...)`, `SetInspectedCondition(...)`

### PropertyRepair
- RPRI (NFA Exhibit 6). `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` · no soft-delete · private ctor · **flat, no children**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| AssetRegistryId | Guid | |
| RpriNo | string | |
| Status | RepairStatus | Requested…Accepted (enum in file) |
| NatureOfWork | string | + PartsToReplace?, RequestedBy, RequestedOn |
| InspectorId / InspectorName | Guid?/string? | |
| EngineNo / ChassisNo | string? | + OdometerReading? |
| NatureOfLastRepair / DateOfLastRepair | string?/DateOnly? | |
| PreInspectionFindings / PreInspectedBy / NotedBy / PreInspectedOn | string?/…/DateOnly? | |
| RepairShop / JobOrderNo / InvoiceNo / InvoiceDate / AmountPerJO | string?/…/decimal? | post-repair |
| PostInspectionFindings / PostInspectedBy / PostInspectedOn | | |
| PrNo / PoJoNo / BurNo / DvNo | string? | procurement/finance refs |
| AcceptedBy / AcceptedOn | string?/DateOnly? | |
- Enum `RepairStatus`: Requested, PreInspected, Repaired, PostInspected, Accepted
- Methods: `Request(...)`, `RecordPreRepairInspection(...)`, `RecordPostRepairInspection(...)`, `Accept(...)`, `SetCreatedBy/SetLastModifiedBy` (internal)

### Location
- Base: `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity`, `ISoftDeletable` (public settable) · private ctor

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| Code | string | |
| Name | string | |
| Type | LocationType | |
| ParentLocationId | Guid? | self-reference |
| Description | string? | |
- Enum `LocationType`: Warehouse, Office, Storage, Department, Other
- Methods: `Create(...)`, `Update(...)`

### SignedDocument (AssetRegister)
- Base: `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` · private ctor
- Uploaded wet-signed scan (RRSP/RRP). One copy per (tenant, document).

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| DocumentType | AssetRegisterDocumentType | |
| DocumentId | Guid | polymorphic FK |
| StorageKey / Sha256 / FileName / ContentType | string | |
| FileSizeBytes | long | |
| UploadedById | Guid? | + UploadedByName? |
| UploadedOnUtc | DateTimeOffset | |
- Methods: `Create(...)`, `Replace(...)`
- **Note:** identical shape recurs in ProcurementAcquisition and BudgetDisbursement (only the `DocumentType` enum differs) — a de-dup candidate.

### AssetRegister domain services (interfaces, not entities)
`Domain/Services/`: `IAccountabilityNumberGenerator`, `IInventoryTransferNumberGenerator`, `IIncidentNumberGenerator`, `IIssuanceReportNumberGenerator`, `IUnserviceableReportNumberGenerator`, `IReceivingReportNumberGenerator`, `ICurrentReplacementCostCalculator`, `ICountFreezeGuard`; static policy `ReplacementCostPolicy` (record `PriceObservation`). Not persisted.

### AssetRegister domain events
`AssetRegisterDomainEvent` (abstract record; `IDomainEvent`, `INotification`) + concrete: `AssetRegisteredEvent`, `AssetIssuedEvent`, `AssetReturnedEvent`, `AssetTransferredEvent`, `AssetTransferredOutEvent`, `AssetFoundAtStationEvent`, `AssetReportedMissingFromCountEvent`, `AssetLostEvent`, `AssetRecoveredEvent`, `AssetUnserviceableEvent`, `AssetDisposedEvent`, `AccountabilityCancelledEvent`, `AccountabilityAcceptedEvent`, `IssuanceReportPostedEvent`, `PhysicalCountSessionClosedEvent`, `PhysicalCountFrozenEvent`, `PhysicalCountRecountRequestedEvent`, `IncidentReportFiledEvent`, `UnserviceableReportSubmittedEvent`. In-process only.

---

## 4. ProcurementPlanning

PPMP + APP with version chains. Aggregates use `IAuditableEntity` + `ISoftDeletable` but are **not** `IHasTenant` (office-scoped via `OfficeCode`).

### Ppmp (aggregate) + PpmpItem (child) + PpmpItemData (record)
- `AggregateRoot<Guid>`, `IAuditableEntity`, `ISoftDeletable` (public settable) · private ctor

**Ppmp**

| Property | Type | Notes |
|----------|------|-------|
| PpmpNumber | string | |
| FiscalYear | int | |
| Phase | PpmpPhase | Indicative/Final/Updated |
| OfficeCode | string | |
| EndUserUnit | string | |
| Status | PpmpStatus | Draft…Superseded |
| VersionNumber | int | |
| IsCurrentVersion | bool | |
| VersionChainId | Guid | |
| PreviousVersionId | Guid? | |
| AmendmentReason | string? | + AmendedAt?, AmendedById? |
| PreparedById | Guid | |
| SubmittedAt / ApprovedAt | DateTimeOffset? | + ApprovedById? |
| ReturnReason / ReturnedAt / ReturnedById | string?/…/Guid? | |
| Items | IReadOnlyList\<PpmpItem\> | |
- Methods: `Create(...)`, `Update(...)`, `Submit()`, `Approve(by)`, `Recall()`, `Return(reason, by)`, `MarkConsolidated()`, `UnmarkConsolidated()`, `PromoteToFinal(by)`, `CreateUpdate(reason, by)`, `Supersede()`

**PpmpItem** (child; private ctor)

| Property | Type | Notes |
|----------|------|-------|
| Id / PpmpId | Guid | |
| ItemNo | int | |
| GeneralDescription | string | |
| ProjectType | ProjectType | |
| Quantity | decimal | |
| Unit | string | |
| ModeOfProcurement | string | |
| PreProcurementConference | bool | |
| ProcurementStart / ProcurementEnd / ExpectedDelivery | string | |
| SourceOfFunds | string | |
| EstimatedBudget | decimal | |
| SupportingDocuments / Remarks | string? | |
| FundingSourceCode | string? | UACS link |
- Methods (internal): `Create(...)`, `Clone(...)`

**PpmpItemData** — parameter-object record (handler → domain).

### AnnualProcurementPlan (aggregate) + AppSourcePpmp (child) + AppLineItem (child)
- `AggregateRoot<Guid>`, `IAuditableEntity`, `ISoftDeletable` (private set + `SoftDelete`) · private ctor

**AnnualProcurementPlan**

| Property | Type | Notes |
|----------|------|-------|
| AppNumber | string | |
| FiscalYear | int | |
| Phase | AppPhase | |
| Status | AppStatus | Draft…Superseded |
| VersionNumber / IsCurrentVersion / VersionChainId / PreviousVersionId | | version chain |
| AmendmentReason / AmendedAt / AmendedById | | |
| ConsolidatedById / ConsolidatedOn | Guid?/DateTimeOffset? | |
| ApprovedById / ApprovedOn | | |
| ReturnReason / ReturnedAt / ReturnedById | | |
| SourcePpmps | IReadOnlyList\<AppSourcePpmp\> | |
| LineItems | IReadOnlyList\<AppLineItem\> | |
- Methods: `Create(...)`, `ValidateForConsolidation(...)`, `ConsolidatePpmps(...)`, `Publish()`, `Approve(by)`, `Recall()`, `Return(reason, by)`, `PromoteToFinal(by)`, `CreateUpdate(reason, by)`, `Supersede()`, `SoftDelete(by)`

**AppSourcePpmp** (child; private ctor): Id, AppId, PpmpId, PpmpNumber, OfficeCode, EndUserUnit, Phase, VersionNumber, IncludedOnUtc, IncludedById — snapshots of consolidated PPMPs. Methods (internal): `FromPpmp(...)`, `Clone(...)`.

**AppLineItem** (child; private ctor): Id, AppId, SourcePpmpId, SourcePpmpItemId, SourcePpmpNumber, OfficeCode, EndUserUnit, ItemNo, GeneralDescription, ProjectType, Quantity, Unit, ModeOfProcurement, PreProcurementConference, ProcurementStart, ProcurementEnd, ExpectedDelivery, SourceOfFunds, EstimatedBudget, SupportingDocuments?, Remarks?, ConsolidatedAt — flattened copies of PpmpItem. Methods (internal): `FromPpmpItem(...)`, `Clone(...)`.
- **Normalization note:** `AppLineItem` duplicates almost every `PpmpItem` column by design (snapshot-on-consolidate).

---

## 5. ProcurementAcquisition

PR → Canvass → PO/JO → IAR chain, plus per-fiscal-year number sequences and signed docs. All aggregates `IHasTenant` + `IAuditableEntity` + `ISoftDeletable`. Number sequences use **xmin**.

### PurchaseRequest (aggregate) + PurchaseRequestLineItem (child) + PurchaseRequestLineItemData (record)
- `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity`, `ISoftDeletable` (public settable) · private ctor

**PurchaseRequest**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| PrNumber | string | |
| PrDate | DateOnly | |
| SaiNumber / SaiDate | string?/DateOnly? | |
| AlobsNumber / AlobsDate | string?/DateOnly? | |
| DepartmentId | Guid | |
| ResponsibilityCenterCode | string? | |
| Purpose | string | |
| PrType | PrType | |
| Category | ProcurementCategory | Asset / Supply |
| Justification | string? | |
| Status | PurchaseRequestStatus | Draft…Completed |
| RequestedById / RequestedByName / RequestedByDesignation | Guid?/string/string? | snapshot |
| FundsAvailableCertifiedById / …Name / …Designation / …OnUtc | | Accountant sign |
| ApprovedById / ApprovedByName / ApprovedByDesignation / ApprovedOnUtc | | HoPE |
| ReturnedReason / ReturnedById / ReturnedByName / ReturnedOnUtc | | |
| RejectionReason / CancellationReason | string? | |
| LineItems | IReadOnlyList\<PurchaseRequestLineItem\> | |
- Methods: `Create(...)`, `Update(...)`, `Submit()`, `CertifyFundsAvailable(...)`, `Approve(...)`, `Complete()`, `ReturnForRevision(...)`, `Reject(reason)`, `Cancel(reason?)`

**PurchaseRequestLineItem** (child; private ctor): ItemNo, Quantity, UnitOfIssue, ItemDescription, EstimatedUnitCost, EstimatedTotalCost (computed), StockNumber?, CatalogItemId?, UacsObjectCode?. Methods: `Create(...)`, `Update(...)`, `AssignUacs(...)` (internal).

**PurchaseRequestLineItemData** — parameter-object record struct.

### NumberSequence (shared document-number counter)
- **Framework entity** — `AMIS.Framework.Core.Domain.NumberSequence` (BuildingBlocks/Core). `BaseEntity<Guid>`, `IHasTenant` · xmin · key (TenantId, SequenceKey, Year, Month). Props: TenantId, SequenceKey, Year, Month, LastSerial. Methods: `Create(...)`, `NextSerial()`.
- One `procurement.NumberSequences` table (mapped `multiTenant: true`) replaces the former per-document counters. Rows are discriminated by `SequenceKey`:
  - `PR` — key (TenantId, Year); Month 0 (year-only; the formatted PR number carries the month cosmetically).
  - `PO` — key (TenantId, Year, Month); serial resets monthly.
  - `JO` — key (TenantId, Year, Month); serial resets monthly.
  - `RIV` — key (TenantId, Year); Month 0.
  - `IAR` — key (TenantId, Year); Month 0.
- Allocation is shared: `AMIS.Framework.Persistence.Sequencing.SequenceAllocator` (`ReserveNextSerialAsync` / `AllocateAsync` / `GetOrCreateRowAsync`) increments under xmin optimistic concurrency with bounded retry (5 attempts) on `DbUpdateConcurrencyException` or Postgres 23505. Mapped via `modelBuilder.ConfigureNumberSequences(schema, multiTenant)`. AssetRegister keeps its own `PropertyCodeCounter` + `CounterAllocator` (not migrated).

### PurchaseOrder (aggregate) + PurchaseOrderLineItem (child) + PurchaseOrderLineItemData (record)
- `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity`, `ISoftDeletable` · private ctor

**PurchaseOrder**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| PoNumber | string | |
| PoDate | DateOnly | |
| PurchaseRequestId | Guid | |
| CanvassRequestId | Guid? | |
| SupplierId | Guid | + SupplierName, SupplierAddress, SupplierTin? |
| ModeOfProcurement | ModeOfProcurement | (Contracts enum) |
| PlaceOfDelivery | string | + DateOfDelivery?, DeliveryTerm, PaymentTerm |
| FundCluster | string? | |
| OursBursNumber / OursBursDate | string?/DateOnly? | |
| Status | PurchaseOrderStatus | Draft…Fulfilled/Cancelled |
| CancellationReason | string? | |
| Category | ProcurementCategory | |
| FundsAvailableCertified* | Guid?/string?/… | Accountant sign |
| IssuedById / IssuedByName / IssuedByDesignation / IssuedOnUtc | | approver |
| LineItems | IReadOnlyList\<PurchaseOrderLineItem\> | |
| TotalAmount | decimal | computed |
- Methods: `Create(...)`, `Update(...)`, `Submit()`, `CertifyFundsAvailable(...)`, `Issue(...)`, `RecordDelivery(totalAcceptedQty)`, `Cancel(reason?)`

**PurchaseOrderLineItem** (child; private ctor): ItemNo, StockNumber?, Unit, Description, Quantity, UnitCost, Amount (computed), CatalogItemId?. Methods: `Create(...)`, `Update(...)`.

### PoNumberSequence → merged
- Merged into the shared **NumberSequence** (SequenceKey `PO`, key TenantId+Year+Month).

### CanvassRequest (aggregate) + CanvassRequestLineItem (child) + CanvassAwardSignatory (child) + CanvassRequestLineItemData (record)
- `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity`, `ISoftDeletable` · private ctor

**CanvassRequest**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| RivNumber | string | |
| PurchaseRequestId | Guid | |
| ReturnDeadline | DateOnly | |
| Status | CanvassRequestStatus | Open/Evaluated/Awarded/Cancelled |
| AwardedSupplierId | Guid? | single-supplier convenience |
| LineItems | IReadOnlyList\<CanvassRequestLineItem\> | covered PR lines |
| AwardSignatories | IReadOnlyList\<CanvassAwardSignatory\> | frozen ROPC committee |
| CoveredItemNos | IEnumerable\<int\> | computed |
| Quotations | ICollection\<CanvassQuotation\> | nav |
- Methods: `Create(...)`, `AwardLines(...)`, `Cancel()`, `Evaluate()`

**CanvassRequestLineItem** (child; private ctor): PrItemNo, Description, Unit, Quantity, EstimatedUnitCost, CatalogItemId?, UacsObjectCode?, EstimatedTotalCost (computed), AwardedQuotationId?, AwardedSupplierId?, AwardedUnitPrice?. Methods: `Create(...)`, `AwardTo(...)`.

**CanvassAwardSignatory** (child; private ctor): SortOrder, Name, Role. Method: `Create(...)`.

### CanvassQuotation (aggregate) + CanvassQuotationLineItem (child)
- `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity`, `ISoftDeletable` · private ctor

**CanvassQuotation**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| CanvassRequestId | Guid | |
| SupplierId | Guid | + SupplierName, SupplierAddress, TinNumber? |
| QuotationDate | DateOnly | |
| DeliveryTerms | string? | |
| IsAwarded | bool | |
| LineItems | IReadOnlyList\<CanvassQuotationLineItem\> | |
- Methods: `Create(...)`, `Update(...)`, `MarkAwarded()`, `ClearAwarded()`

**CanvassQuotationLineItem** (child; private ctor): ItemNo, PrItemNo, Description, Unit, Quantity, UnitPrice, Total (computed). Method: `Create(...)`.

### RivNumberSequence → merged
- Merged into the shared **NumberSequence** (SequenceKey `RIV`, key TenantId+Year).

### InspectionAcceptanceReport (aggregate) + InspectionAcceptanceReportLineItem (child)
- IAR. `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity`, `ISoftDeletable` · private ctor

**InspectionAcceptanceReport**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| IarNumber | string | |
| IarDate | DateOnly | |
| PurchaseOrderId | Guid | |
| SupplierId | Guid | + SupplierName |
| InspectedById | Guid | assigned inspector |
| ReceivedById | Guid | property custodian |
| DeliveryReceiptNo / DeliveryDate | string?/DateOnly? | |
| Status | InspectionAcceptanceReportStatus | Draft…Accepted/Cancelled |
| Category | ProcurementCategory | Asset/Supply |
| Remarks | string? | |
| SubmittedForInspectionOnUtc / InspectedOnUtc / AcceptedOnUtc / CancelledOnUtc | DateTimeOffset? | |
| AcceptedById / AcceptedByName / AcceptedByDesignation | | acceptance snapshot |
| LineItems | IReadOnlyList\<…LineItem\> | |
| TotalAmount | decimal | computed |
- Methods: `Create(...)`, `Update(...)`, `SubmitForInspection()`, `ReassignInspector(...)`, `RecordInspection(...)`, `AssignPropertyNo(...)`, `ExpandLineByQuantity(...)`, `Accept(...)`, `Cancel()`

**InspectionAcceptanceReportLineItem** (child; private ctor): ItemNo, Description, TechnicalSpecifications?, Brand?, Model?, SerialNo?, PropertyClassHint?, Unit, Quantity, UnitCost, Amount (computed), InspectionRemarks?, StockPropertyNo?, StockNumber?, CatalogItemId?, UacsObjectCode?, InspectionResult (LineInspectionResult, default Pending), InspectedOnUtc?, InspectedById?. Methods (internal): `Create(...)`, `RecordInspection(...)`, `AssignPropertyNo(...)`, `Renumber(...)`, `CloneAsSingleUnit(...)`, `SetQuantity(...)`.

### IarNumberSequence → merged
- Merged into the shared **NumberSequence** (SequenceKey `IAR`, key TenantId+Year).

### JobOrder (aggregate) + JobOrderLineItem (child) + JobOrderLineItemData (record)
- Works (renovation/repair/fabrication) — PO-like flow with inspection & acceptance on the JO itself. `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity`, `ISoftDeletable` · private ctor

**JobOrder** (selected; mirrors PO header + inspection/acceptance)

| Property | Type | Notes |
|----------|------|-------|
| TenantId / JoNumber / JoDate | | |
| PurchaseRequestId | Guid? | optional source PR |
| JobRequestNo / RequisitioningOffice | string? | |
| SupplierId + SupplierName/Address/Tin? | | |
| ModeOfProcurement | ModeOfProcurement | |
| PlaceOfDelivery / DateOfDelivery? / DeliveryTerm / PaymentTerm | | |
| FundCluster? / OursBursNumber? / OursBursDate? | | |
| Status | JobOrderStatus | Draft…Completed/Cancelled |
| CancellationReason? | | |
| FundsAvailableCertified* | | Accountant |
| IssuedBy* | | approver |
| InspectorId / InspectorName / InspectorDesignation? | | frozen inspector |
| InspectedOnUtc? / InspectionInvoiceNo? / InspectionInvoiceDate? / DateInspected? / InspectionFindings? / FoundInOrder | | |
| AcceptedById? / AcceptedOnUtc? / AcceptanceInvoiceNo? / DateReceived? / IsCompleteDelivery / PartialDeliveryNote? | | |
| LineItems | IReadOnlyList\<JobOrderLineItem\> | |
| TotalAmount | decimal | computed |
- Methods: `Create(...)`, `Update(...)`, `Submit()`, `CertifyFundsAvailable(...)`, `Issue(...)`, `Inspect(...)`, `Accept(...)`, `Cancel(reason?)`

**JobOrderLineItem** (child; private ctor): ItemNo, Unit?, Description, Quantity, UnitCost, Amount (computed). Method: `Create(...)`.

### JoNumberSequence → merged
- Merged into the shared **NumberSequence** (SequenceKey `JO`, key TenantId+Year+Month).

### SignedDocument (ProcurementAcquisition)
- Same shape as AssetRegister `SignedDocument`; `DocumentType` = `ProcurementDocumentType` (PR/PO/AoC). `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity`, `ISoftDeletable`. Methods: `Create(...)`, `Replace(...)`.

---

## 6. BudgetDisbursement

BUR → DV disbursement flow, deductions, per-year number sequences, module settings, signed docs.

### BudgetUtilizationRequest
- `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` · `byte[] Version` · soft-delete getter-only + `SoftDelete` · private ctor

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| BurNumber | string | |
| BurDate | DateOnly | |
| PurchaseOrderId | Guid | + PurchaseOrderNumber |
| DisbursementVoucherId | Guid? | + DisbursementVoucherNumber? (set on Utilize) |
| FundCluster | string | |
| AllotmentClass | string | |
| UacsObjectCode | string | |
| ResponsibilityCenter | string? | |
| Particulars | string | |
| Amount | decimal | |
| Status | BudgetUtilizationRequestStatus | Draft/Obligated/Utilized/Cancelled |
| Remarks | string? | |
- Methods: `Create(...)`, `Obligate()`, `Utilize(dvId, dvNumber)`, `Release()`, `Cancel(remarks)`, `SoftDelete(by)`

### BurNumberSequence → merged
- Merged into the shared **NumberSequence** (see ProcurementAcquisition §5). Budget maps `budgetdisbursement.NumberSequences` with `multiTenant: false` — DV/BUR are global year-only series, so rows store `TenantId = ""` and Month 0. SequenceKey `BUR`. On a missing counter row the create handler seeds `LastSerial` from the highest issued BUR number (seed-from-max preserved).

### DisbursementVoucher (aggregate) + DvDeduction (child)
- `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` · `byte[] Version` · soft-delete getter-only + `SoftDelete` · private ctor

**DisbursementVoucher**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| DvNumber | string | |
| DvDate | DateOnly | |
| PurchaseOrderId | Guid | + PurchaseOrderNumber (inherited from BUR) |
| BudgetUtilizationRequestId | Guid | + BurNumber |
| FundCluster | string | |
| Payee | string | + TinNo?, PayeeAddress? |
| Particulars | string | |
| Amount | decimal | gross |
| ModeOfPayment | string | |
| Deductions | IReadOnlyCollection\<DvDeduction\> | owned |
| TotalDeductions | decimal | computed |
| AmountDue | decimal | computed (net) |
| Status | DisbursementVoucherStatus | Draft/ForApproval/Approved/Paid/Returned/Cancelled |
| Remarks | string? | |
| PaidDate | DateOnly? | |
- Methods: `Create(...)`, `Update(...)`, `ReplaceDeductions(...)`, `Approve()`, `Pay(paidDate, remarks?)`, `Return(remarks)`, `Cancel(remarks)`, `SoftDelete(by)`

**DvDeduction** (owned child; private ctor): Id, Name, Type (`DvDeductionType` — Percentage/Fixed), Value. Methods: `Create(name, type, value)`, `ComputeAmount(grossAmount)`.

### DvNumberSequence → merged
- Merged into the shared **NumberSequence** (SequenceKey `DV`, global year-only — `TenantId ""`, Month 0; `multiTenant: false`). Seed-from-max preserved.

### BudgetDisbursementModuleSettings
- Plain class (**no base**), one row per tenant · all props `{ get; set; }`

| Property | Type | Notes |
|----------|------|-------|
| Id | Guid | |
| TenantId | string | |
| WatermarkSignedCopies | bool | default true |
| DvSectionA/B/C Name & Designation | string? | signatory overrides, null = fall back to org profile |
| BurSectionA/B Name & Designation | string? | signatory overrides |
- Method: `CreateDefault(tenantId)`

### SignedDocument (BudgetDisbursement)
- Same shape as the other SignedDocuments; `DocumentType` = `BudgetDisbursementDocumentType`. `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity`, `ISoftDeletable`. Methods: `Create(...)`, `Replace(...)`.

---

## 7. Vehicle

Fleet management. Vehicles are enrolled from a canonical PPE `AssetRegistry` (via `AssetRegistryId`). All `IHasTenant` + `IAuditableEntity` + `ISoftDeletable` (private set) with `internal Set*` audit setters.

### Enums
- `VehicleType`: Other, Sedan, SUV, Van, Truck, Motorcycle, Bus, PickUp, MPV, Hatchback, Crossover, Wagon, Minibus
- `VehicleStatus`: Active, UnderRepair, Retired, Decommissioned

### Vehicle
- `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` (+ soft-delete private set)

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| AssetRegistryId | Guid | required + unique link to PPE asset |
| PropertyNo | string | mirror from asset |
| AcquisitionDate | DateOnly | mirror |
| PlateNumber | string | upper-cased |
| Make / Model | string | |
| Year | int | |
| Type | VehicleType | |
| Status | VehicleStatus | default Active |
| Odometer | int | |
| MotorNumber / ChassisNumber | string? | |
| NumberOfCylinders / EngineDisplacementCC | int? | |
| FuelType / VehicleUse | string? | |
| AcquisitionCost | decimal? | mirror from asset UnitCost |
| Notes | string? | `{ get; set; }` |
- Methods: `Enroll(...)`, `Update(...)`, `UpdateOdometer(reading)`, `MarkUnderRepair()`, `Reactivate()`, `Retire()`, `Decommission()`, `SoftDelete(by)`, internal `SetCreatedBy/SetLastModifiedBy/SetLastModifiedOnUtc`
- Raises `VehicleCreatedEvent` / `VehicleReactivatedEvent` / `VehicleRetiredEvent` / `VehicleDecommissionedEvent`.

### VehicleDailyUsage
- `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` (+ soft-delete private set)

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| VehicleId | Guid | |
| Date | DateOnly | |
| OdometerStart / OdometerEnd | int | |
| DistanceKm | int | derived stored |
| FuelLiters / FuelCost | decimal | |
| KmPerLiter / CostPerKm | decimal | computed |
| Destination / Remarks | string? | |
- Methods: `Create(...)`, `Update(...)`, `SoftDelete(by)`, static `CalculateKmPerLiter/CalculateCostPerKm`, internal setters

### MaintenanceLog
- `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` (+ soft-delete private set)

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| VehicleId | Guid | |
| ScheduleId | Guid? | optional link to schedule |
| MaintenanceType | string | |
| PerformedDate | DateOnly | |
| OdometerAtService | int? | |
| Description | string? | |
| Cost | decimal? | |
| PerformedBy | string? | |
| Notes | string? | `{ get; set; }` |
- Methods: `Create(...)`, `Update(...)`, `SoftDelete(by)`, internal setters

### MaintenanceSchedule
- `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` (+ soft-delete private set)

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| VehicleId | Guid | |
| MaintenanceType | string | |
| Description | string? | |
| IntervalDays / IntervalMileage | int? | |
| DueDate / DueMileage | DateOnly?/int? | |
| LastDoneDate / LastDoneMileage | DateOnly?/int? | |
| IsActive | bool | default true |
- Methods: `Create(...)`, `Update(...)`, `RecordCompletion(doneDate, doneMileage?)`, `Activate()`, `Deactivate()`, `SoftDelete(by)`, internal setters

---

## 8. Identity

ASP.NET Core Identity extensions + groups + sessions + password history. Uses `Guid`/`string` string keys (Identity default). Not `IHasTenant` at the entity level.

### AmisUser
- Base: `IdentityUser` (string Id), `IHasDomainEvents`

| Property | Type | Notes |
|----------|------|-------|
| (inherited) UserName, Email, PasswordHash, … | | from IdentityUser |
| FirstName / LastName | string? | |
| ImageUrl | Uri? | |
| IsActive | bool | |
| ObjectId | string? | external directory id |
| LastPasswordChangeDate | DateTime | |
| PasswordHistories | ICollection\<PasswordHistory\> | nav |
- Methods: `RecordRegistered(...)`, `RecordPasswordChanged(...)`, `Activate(...)`, `Deactivate(...)`, `RecordRolesAssigned(...)` (all raise domain events)

### AmisRole
- Base: `IdentityRole` (string Id). Adds `Description` (string?). Ctor sets NormalizedName.

### AmisRoleClaim
- Base: `IdentityRoleClaim<string>`. Adds `CreatedBy` (string?), `CreatedOn` (DateTimeOffset) — both `{ get; init; }`.

### Group
- Base: `ISoftDeletable` (private set) · private ctor

| Property | Type | Notes |
|----------|------|-------|
| Id | Guid | |
| Name | string | |
| Description | string? | |
| IsDefault | bool | |
| IsSystemGroup | bool | |
| CreatedAt / CreatedBy / ModifiedAt / ModifiedBy | DateTime/string? | (own audit shape, not IAuditableEntity) |
| GroupRoles | ICollection\<GroupRole\> | nav |
| UserGroups | ICollection\<UserGroup\> | nav |
- Methods: `Create(...)`, `Update(...)`, `SetAsDefault(...)`, `SoftDelete(by)`

### GroupRole (join)
- Props: GroupId (Guid), RoleId (string), Group? nav, Role? nav. Method: `Create(groupId, roleId)`.

### UserGroup (join)
- Props: UserId (string), GroupId (Guid), AddedAt (DateTime), AddedBy (string?), User? nav, Group? nav. Method: `Create(userId, groupId, addedBy?)`.

### PasswordHistory
- Props: Id (int, identity), UserId (string), PasswordHash (string), CreatedAt (DateTime), User? nav. Method: `Create(userId, passwordHash)`.

### UserSession
- Base: `IHasDomainEvents` · private ctor

| Property | Type | Notes |
|----------|------|-------|
| Id | Guid | |
| UserId | string | |
| RefreshTokenHash | string | |
| IpAddress / UserAgent | string | |
| DeviceType / Browser / BrowserVersion / OperatingSystem / OsVersion | string? | |
| CreatedAt / LastActivityAt / ExpiresAt | DateTime | |
| IsRevoked | bool | + RevokedAt?, RevokedBy?, RevokedReason? |
| User | AmisUser? | nav |
- Methods: `Create(...)`, `UpdateActivity()`, `UpdateRefreshToken(...)`, `Revoke(...)` (raises `SessionRevokedEvent`)

### Identity domain events
`Domain/Events/`: `UserRegisteredEvent`, `PasswordChangedEvent`, `UserActivatedEvent`, `UserDeactivatedEvent`, `UserRoleAssignedEvent`, `SessionRevokedEvent`.

---

## 9. Multitenancy

Finbuckle tenant store + provisioning + theming + platform settings.

### AppTenantInfo (tenant store — lives in `BuildingBlocks/Shared`)
- Base: Finbuckle `TenantInfo`, `IAppTenantInfo`. Table `Tenants`.

| Property | Type | Notes |
|----------|------|-------|
| Id | string | (from TenantInfo) |
| Identifier | string | |
| Name | string? | |
| ConnectionString | string | |
| AdminEmail | string | |
| IsActive | bool | |
| ValidUpto | DateTime | subscription validity |
| Issuer | string? | |
- Methods: `AddValidity(months)`, `SetValidity(validTill)`, `Activate()`, `Deactivate()`

### TenantProvisioning (aggregate-ish) + TenantProvisioningStep (child)
- Plain classes (no framework base), in `Provisioning/`.

**TenantProvisioning**

| Property | Type | Notes |
|----------|------|-------|
| Id | Guid | |
| TenantId | string | |
| CorrelationId | string | |
| Status | TenantProvisioningStatus | Pending/Running/Completed/Failed |
| CurrentStep | string? | |
| Error | string? | |
| JobId | string? | |
| CreatedUtc / StartedUtc? / CompletedUtc? | DateTime | |
| Steps | ICollection\<TenantProvisioningStep\> | |
- Methods: `SetJobId(...)`, `MarkRunning(step)`, `MarkCompleted()`, `MarkFailed(step, error)`

**TenantProvisioningStep** (child): Id (Guid), ProvisioningId (Guid), Step (`TenantProvisioningStepName`), Status (`TenantProvisioningStatus`), Error?, StartedUtc?, CompletedUtc?, Provisioning? nav. Methods: `MarkRunning()`, `MarkCompleted()`, `MarkFailed(error)`.

### TenantTheme
- Base: `BaseEntity<Guid>`, `IHasTenant`, `IAuditableEntity` (private set) · private ctor · all palette props `{ get; set; }`
- Light + dark palettes (Primary/Secondary/Tertiary/Background/Surface/Error/Warning/Success/Info × light+dark), brand assets (LogoUrl, LogoDarkUrl, FaviconUrl), typography (FontFamily, HeadingFontFamily, FontSizeBase, LineHeightBase), layout (BorderRadius, DefaultElevation), IsDefault.
- Methods: `Create(tenantId, createdBy?)`, `Update(modifiedBy?)`, `ResetToDefaults()`

### PlatformSettings
- Base: `BaseEntity<Guid>`, `IAuditableEntity` (private set) · **singleton** (fixed `SingletonId`) · not tenant-scoped

| Property | Type | Notes |
|----------|------|-------|
| MaxSessionsPerUser | int? | |
| IdleTimeoutMinutes | int? | |
| AbsoluteTimeoutDays | int | default 7 |
| MaxUsersPerTenant | int? | |
| StorageLimitMb | long? | |
| ApiRateLimitPerMinute | int? | |
- Methods: `CreateDefault(createdBy?)`, `Update(modifiedBy?)`, `ResetToDefaults()`

---

## 10. Chat

Channels, membership, messages, mentions, reactions. Uses `Guid.CreateVersion7()` ids. Tenant is denormalized (display only), not a query filter.

### ChatChannel (aggregate) + ChannelMember (child)
- `AggregateRoot<Guid>`, `ISoftDeletable` (private set) · private ctor

**ChatChannel**

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string? | null for global channel |
| Type | ChannelType | Named/Direct/GroupDm |
| Scope | ChannelScope | Office/Global |
| Name | string? | |
| Topic | string? | |
| DirectKey | string? | sorted user-id pair for 1:1 DM |
| CreatedBy | string | |
| CreatedOnUtc | DateTimeOffset | |
| LastMessageId | Guid? | + LastMessageAtUtc? |
| Members | IReadOnlyCollection\<ChannelMember\> | |
- Methods: `CreateChannel(...)`, `CreateDirect(...)`, `CreateGroupDm(...)`, `CreateGlobal(...)`, `AddMember(...)`, `RemoveMember(...)`, `Rename(...)`, `TouchLastMessage(...)`, `Archive(by)`, `Restore()`, static `BuildDirectKey(a, b)`

**ChannelMember** (child; `BaseEntity<Guid>`; private ctor): ChannelId, UserId, Role (`ChannelMemberRole`), TenantId?, JoinedOnUtc, LastReadMessageId?, LastReadOnUtc?. Methods (internal `Create`), `MarkRead(messageId)`.

### Message (aggregate) + MessageMention (child) + MessageReaction (child)
- `BaseEntity<Guid>` · private ctor · soft-delete via `DeletedAtUtc` tombstone (**not** `ISoftDeletable`)

**Message**

| Property | Type | Notes |
|----------|------|-------|
| ChannelId | Guid | |
| SenderId | string | + SenderName? (denormalized) |
| Content | string | |
| ParentMessageId | Guid? | threading |
| ReplyCount | int | |
| IsPinned | bool | + PinnedOnUtc?, PinnedBy? |
| TenantId | string? | denormalized |
| CreatedOnUtc / EditedOnUtc? / DeletedAtUtc? | DateTimeOffset | |
| DeletedBy | string? | |
| IsDeleted | bool | computed (DeletedAtUtc != null) |
| Mentions | IReadOnlyCollection\<MessageMention\> | |
| Reactions | IReadOnlyCollection\<MessageReaction\> | |
- Methods: `Create(...)`, `Edit(content)`, `SoftDelete(by)`, `Pin(by)`, `Unpin()`, `IncrementReplyCount()`, `AddMention(userId)`, `AddReaction(userId, emoji)`, `RemoveReaction(userId, emoji)`

**MessageMention** (child; `BaseEntity<Guid>`): MessageId, MentionedUserId, CreatedOnUtc. Method: internal `Create`.

**MessageReaction** (child; `BaseEntity<Guid>`): MessageId, UserId, Emoji, CreatedOnUtc; unique per (message, user, emoji). Method: internal `Create`.

---

## 11. Notifications

### Notification
- `AggregateRoot<Guid>`, `IHasTenant`, `IAuditableEntity` (settable audit) · **not** soft-deletable (dismiss = hard delete) · private ctor · Finbuckle `.IsMultiTenant()`

| Property | Type | Notes |
|----------|------|-------|
| TenantId | string | |
| RecipientUserId | string | Identity user id |
| Type | NotificationType | |
| Title | string | |
| Body | string | |
| Link | string? | relative SPA route |
| MetadataJson | string? | |
| Source | string | producing module |
| CorrelationId | string | de-dup key; unique with (RecipientUserId, Type) |
| IsRead | bool | + ReadOnUtc? |
- Methods: `Create(...)`, `MarkRead()`

---

## 12. Auditing

Legacy core module (uses `Core/`, `Infrastructure/`, `Persistence/` instead of `Domain/`). The runtime `Audit` fluent API + `AuditEnvelope` (contract) produce **one persisted entity**:

### AuditRecord (in `Persistence/`)
- Plain class, all props `{ get; set; }`

| Property | Type | Notes |
|----------|------|-------|
| Id | Guid | |
| OccurredAtUtc / ReceivedAtUtc | DateTime | |
| EventType | int | AuditEventType |
| Severity | byte | AuditSeverity |
| TenantId / UserId / UserName | string? | |
| TraceId / SpanId / CorrelationId / RequestId / Source | string? | |
| Tags | long | AuditTag flags |
| PayloadJson | string | serialized event payload |

> `Audit` (static fluent builder), `SecurityAudit`, `AuditEnvelope`, and the `*Payload` records are infrastructure/contracts, not persisted entities.

---

## 13. Shared value objects

Owned EF types defined in `AssetRegister.Contracts.v1.ValueObjects`, embedded into many AssetRegister entities:

### AssetSnapshot (owned type)
Frozen subset of `AssetRegistry` captured at issue/count/incident/disposal. Private ctor + `Create(...)`.

| Property | Type |
|----------|------|
| PropertyNo | string |
| Description | string |
| AssetType | AssetType |
| PropertyClass | string? |
| CategoryCode | string? |
| UnitCost | decimal |
| Unit | string |
| EstimatedUsefulLifeYears | int |
| AcquisitionDate | DateOnly |
| UacsObjectCode | string? |
| SerialNo / Brand / Model | string? |
| NetBookValue | decimal |

### EmployeeRef (owned type)
Signatory snapshot. Props: EmployeeId (Guid), PrintedName (string), Designation (string?). `Create(...)`.

### PropertyNumber (value object, record)
Props: Value (string, upper-cased, ≤32 chars). `Create/Parse/TryParse`, `ToString`.

---

## Refactoring / (de)normalization observations

A few cross-cutting patterns worth a decision before you start moving columns:

1. **`SignedDocument` is triplicated** (AssetRegister, ProcurementAcquisition, BudgetDisbursement) with identical structure — only the `DocumentType` enum differs. Candidate for a shared owned/base type or a single cross-module table with a discriminator.
2. **Number-sequence entities** — ✅ **DONE for ProcurementAcquisition + BudgetDisbursement.** The seven monotonic counters (`PrNumberSequence`, `PoNumberSequence`, `JoNumberSequence`, `RivNumberSequence`, `IarNumberSequence`, `BurNumberSequence`, `DvNumberSequence`) are unified into the shared `AMIS.Framework.Core.Domain.NumberSequence` (one `NumberSequences` table per module, discriminated by `SequenceKey`), with allocation consolidated in `AMIS.Framework.Persistence.Sequencing.SequenceAllocator`. See the **NumberSequence** entry in §5. Still separate on purpose: AssetRegister's `PropertyCodeCounter` + `CounterAllocator` (already generalized, left as the reference implementation), and `PPERRFormSeries` / `PPEIRFormSeries` — those are **not** counters but physical pre-numbered accountable-form batches (fixed serial range, one-active lifecycle) and were deliberately excluded.
3. **Signatory triplets** — `OrganizationProfile` inlines 6 × (Id, Name, Designation); many documents inline "Name + Designation" pairs and `EmployeeRef` (Id, PrintedName, Designation). Decide between the owned-value-object approach (`EmployeeRef`) and the inlined-columns approach and apply consistently.
4. **Snapshot-on-write duplication** is intentional and pervasive (`AssetSnapshot` on every accountability/issuance/incident/return/count line; `AppLineItem` copying `PpmpItem`; PR→PO→IAR line copies). These are deliberate denormalizations for faithful reprints — don't normalize these away without preserving the freeze semantics.
5. **Free-text FKs in Expendable `Product`** — `CategoryId` / `SupplierId` are `string?` and `UnitOfMeasure` is free text, unlike the Guid-keyed MasterData entities. Inconsistent with the rest of the system.
6. **Tenant handling is not uniform** — some entities are Finbuckle `.IsMultiTenant()`, some carry `TenantId` only for display (Chat, Notification denormalized), ProcurementPlanning is office-scoped (`OfficeCode`, no `TenantId`), and Budget number sequences are global (year-only key). Confirm the intended isolation per entity before touching `TenantId` columns.
7. **Concurrency tokens are mixed** — `byte[] Version` (MasterData, BUR, DV), Postgres `xmin` (EmployeeProfile, Expendable aggregates, ProcAcq sequences, AssetRegistry, ProductInventory), and none at all on several. Standardize if desired.
