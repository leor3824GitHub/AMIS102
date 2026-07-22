# Inter-tenant transfer of asset/property (PPEIR → PPERR)

> Status: **Shipped.** Scope delivered is **PPE only** (PPEIR → PPERR); Semi-Expendable (SMIR → SMRR)
> reuses the same aggregates and plumbing and is still to do — the handler rejects a destination on an
> SMIR with a 422 pointing at the manual handshake.
>
> Delivered: the `AssetTransferOffer` aggregate and line, migration `20260718110710_InterTenantAssetTransfers`,
> `AssetTransferProjector` + `AssetTransferProjectionJob`, five feature slices under `Features/v1/Transfers/`,
> the accumulated-depreciation carry-over, and the Blazor "Transfer Offers" pages.
>
> **Follow-up shipped separately (see "Deriving the destination from the recipient" below):** the
> destination agency is no longer a hand-picked dropdown — it is derived from the recipient employee's
> office via a new tenant↔office link.
>
> Also records two **incidental findings** (dead outbox tables; `AddEventingForDbContext` is not
> composable) that predate this feature and remain open — see the last section.

## Context

Today, moving an asset from Agency A to Agency B (two tenants in AMIS) is a **manual, unlinked, two-document handshake**:

1. Sending tenant posts a **PPEIR/SMIR** (`PropertyIssuanceReport`, `IssuanceNature.TransferCO/RO/PO`) → `AssetRegistry.MarkTransferredOut()` sets `LifecycleState.TransferredOut` and clears custodian/location.
2. Receiving tenant separately **re-keys** a **PPERR/SMRR** (`ReceivingReport`, `ReceiptType.Transfer`), typing `SourceAgencyName`, `SourcePropertyNo`, `SourceDocumentRef`, `OriginalAcquisitionDate` by hand from a paper/PDF copy.

Nothing links the two documents. Everything is re-typed (transcription errors), and **accumulated depreciation is silently reset to zero** on the receiving side — a COA GAM correctness bug, not just an inconvenience.

Goal: make the transfer a linked, auditable, low-transcription handshake across the tenant boundary, without weakening tenant isolation.

---

## The hard constraint that determines the whole design

**Data cannot "travel" via a shared row. There is no such thing as a cross-tenant record in this architecture.** Three independent confirmations:

1. `src/BuildingBlocks/Persistence/Context/BaseDbContext.cs:44-56` — `OnConfiguring` swaps the **connection string per tenant** from `AppTenantInfo.ConnectionString`. Tenants may live in physically separate databases. Any design assuming one shared table is silently coupled to the current "shared DB" deployment and breaks the day a tenant is given its own connection string.
2. `src/BuildingBlocks/Persistence/Context/BaseDbContext.cs:63-68` — `SaveChangesAsync` forces `TenantNotSetMode.Overwrite`. Finbuckle stamps the ambient tenant onto every `IHasTenant` entity on save. You **cannot** set `TenantId = tenantB` and save while the request is authenticated as tenant A — Finbuckle rejects the mismatch.
3. `src/Modules/AssetRegister/Modules.AssetRegister/Provisioning/DepreciationRecurringJob.cs:11-18` — the doc-comment states it outright: *"saving across tenants in a single context is rejected by Finbuckle, so we must loop tenant-by-tenant."*

### The sanctioned mechanism

`DepreciationRecurringJob.cs:36-44` is the **canonical, already-proven pattern** in this repo for touching another tenant's data. Reuse it verbatim — do not invent anything:

```csharp
await using var scope = scopeFactory.CreateAsyncScope();
scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
    .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
var db = scope.ServiceProvider.GetRequiredService<AssetRegisterDbContext>();
```

Tenant lookup: `ITenantService.GetAllTenantInfosAsync` (`Modules.Multitenancy.Contracts/ITenantService.cs:18`) — its XML doc explicitly says *"Use for background/cross-tenant work."*

**Consequence:** an inter-tenant transfer is **two writes in two tenant scopes**, linked by a correlation id — never one write. Query filters stay fully intact on both sides; there is no filter bypass and no leak surface.

---

## Recommended approach: a two-phase Transfer Offer handshake

Do **not** auto-create the receiving PPERR/SMRR. That is wrong both technically and in COA terms — the receiving agency must issue its own property numbers from its own pre-numbered accountable form series (`PPERRFormSeries`), supply its own `ReceivedBy`/`NotedBy` signatories, apply its **own** capitalization threshold (`GetActiveCapitalizationThresholdQuery`, which decides PPE vs Semi-Expendable and can differ per agency), and retain the right to reject.

So the sender **offers**; the receiver **accepts** and posts their own document.

```
TENANT A (sender)                          TENANT B (receiver)
─────────────────                          ───────────────────
Post PPEIR/SMIR
 └ IssuanceNature.Transfer*
 └ DestinationTenantId = B      ──────►    Inbox: "Incoming Transfers"
 └ asset → TransferredOut                   └ review payload
 └ AssetTransferOffer (Sent)                └ Reject ──► offer=Rejected, A notified
                                            └ Accept ──► prefills Create PPERR/SMRR
                                                          (own form series, own
                                                           property nos, own signatories)
                                                          └ existing handler runs UNCHANGED
                                            ◄──────      offer → Accepted (+ receipt ref)
```

An offer is **never** auto-accepted, and the sender's asset sits in `TransferredOut` regardless — the sender's books are correct the moment they post, which matches how the paper process already works.

### Why not the alternatives

- **Shared non-tenant "exchange" table** — breaks under per-tenant connection strings; also puts two agencies' data in one un-filtered table, which is the exact isolation guarantee this system sells.
- **Auto-create the PPERR in tenant B** — sender cannot know B's property numbers, form series, signatories, or capitalization threshold; and it forges an accountable document on B's books without B's supply officer acting.
- **Export/import a file** — no link, no audit trail, and re-introduces the transcription errors we're removing.

---

## Data model

One new aggregate, **written into both tenants' own AssetRegister schema** (one row each, same `CorrelationId`). Place in `Domain/Transfers/`:

**`AssetTransferOffer`** : `AggregateRoot<Guid>, IHasTenant, IAuditableEntity`
- `CorrelationId` (Guid) — the join key across the two tenants
- `Direction` — `Outbound` | `Inbound` (which side this row represents)
- `FromTenantId`, `FromAgencyName`, `ToTenantId`, `ToAgencyName`
- `SourceIssuanceReportId`, `SourceIssuanceReportNo`, `IssuanceReportType`
- `Status` — `Sent | Accepted | Rejected | Cancelled`
- `ProjectedUtc?` — null until the projector has copied it into the destination tenant
- `ReceivingReportId?`, `ReceivingReportNo?` (back-ref filled on accept)
- `RejectedReason?`, `RespondedUtc?`
- `_lines` → **`AssetTransferOfferLine`**: `SourcePropertyNo`, `Description`, `SerialNo`, `Brand`, `Model`, `UnitCost`, `OriginalAcquisitionDate`, **`AccumulatedDepreciation`**, **`NetBookValue`**, `CatalogUacsCode?`

EF config: `.ToTable(...).IsMultiTenant()` + **named** soft-delete filter — `builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted)`. An anonymous filter alongside `.IsMultiTenant()` throws at model build on EF Core 10 (see `.claude/rules/persistence.md`). Plus a **unique index on `CorrelationId`** — this is what makes re-projection idempotent.

Both rows are ordinary tenant-scoped rows. Each agency sees only its own copy. That is the point.

---

## Closing the depreciation-continuity gap

This is a correctness fix worth doing regardless of the rest.

`AssetRegistry.Register(...)` (`Domain/Assets/AssetRegistry.cs:123`) hard-sets `AccumulatedDepreciation = 0`. The receiving side currently inherits only the *timeline* (`OriginalAcquisitionDate` → `DepreciationStartDate`, `AssetRegistry.cs:100-102`), so a 4-year-old asset arrives at Agency B at **full original cost**, overstating B's carrying value and understating cumulative depreciation.

The value already exists on the sending side: `PropertyIssuanceReportLine.AccumulatedDepreciation` / `BookValue`, populated by Accounting via `UpdateIssuanceReportDepreciationCommand`, and `AssetSnapshot.NetBookValue`.

Changes:
1. `ReceivingReportItem` — add `AccumulatedDepreciation` (nullable decimal).
2. `AssetRegistry.Register(...)` — add optional `accumulatedDepreciation = 0m` parameter; seed the field instead of hard-zeroing.
3. `CreateReceivingReportCommandHandler.cs:68-78` — pass it through alongside the existing `OriginalAcquisitionDate` continuity logic.
4. **Gate the offer on it:** an offer for a PPEIR line whose `AccumulatedDepreciation` is still null must be blocked — Accounting has to run `UpdateIssuanceReportDepreciation` first. Otherwise we've automated the transfer of a wrong number.

---

## Reliability: the offer row *is* the outbox

The dual write spans two DbContexts in two DI scopes, so it is **two transactions** — even on a shared database. A half-succeeded write strands an asset: `TransferredOut` at A, invisible at B. This needs to be at-least-once.

**Do not use the shared eventing outbox.** Investigation found a live landmine:

- `AddEventingForDbContext<T>()` is called **exactly once in the whole solution** — `IdentityModule.cs:101`, with `IdentityDbContext`. It registers `IOutboxStore`/`IInboxStore` as plain `AddScoped`, so both resolve **globally** to Identity's context.
- Side effect already in the codebase: Chat's and Expendable's `OutboxMessages`/`InboxMessages` tables are **dead** — never read or written. All modules' event idempotency lands in Identity's schema by accident.
- Calling `AddEventingForDbContext<AssetRegisterDbContext>()` would **override those registrations**, repointing Identity's inbox at AssetRegister depending on module registration order. Silent, order-dependent breakage.

Also note `InMemoryEventBus` creates a scope but **does not set tenant context** (`InMemory/InMemoryEventBus.cs:42`), so a consumer runs as the *sender* — it cannot deliver into tenant B anyway. And the dispatcher is off by default (`UseHostedServiceDispatcher=false`, per `GenerateTokenCommandHandler.cs:171`).

**Instead, make the offer row its own outbox** — no new tables, no global DI changes:

1. Sender's PPEIR post writes the issuance report + the outbound `AssetTransferOffer` (`Status=Sent`, `ProjectedUtc=null`) in **one `SaveChanges`**, one tenant, one transaction. Atomic.
2. A Hangfire recurring job scans each tenant for `Status=Sent AND ProjectedUtc IS NULL` and projects the inbound copy into `ToTenantId` inside a `MultiTenantContextSetter` scope, then stamps `ProjectedUtc`.
3. Idempotency is free: the **unique index on `CorrelationId`** in the receiving tenant makes re-projection a no-op. No inbox store needed.

This reuses `DepreciationRecurringJob`'s exact shape (loop tenants, scope-switch, isolate per-tenant failures) — a pattern already registered and proven in this module — and is correct under both shared-DB and DB-per-tenant.

---

## Implementation steps

Scope for this pass: **PPE only** (PPEIR → PPERR). Semi-Expendable (SMIR → SMRR) reuses the same aggregates and plumbing, so extending later is mostly validation + UI.

1. **Depreciation carry-over** (standalone, independently valuable — ship first, on its own) — `AssetRegistry.Register`, `ReceivingReportItem`, `CreateReceivingReportCommandHandler`. Migration.
2. **Domain** — `AssetTransferOffer` + line, enums, EF configs (named soft-delete filter, unique index on `CorrelationId`), DbSet. Migration.
3. **Cross-tenant projector** — a service encapsulating the `IMultiTenantContextSetter` scope switch, modelled on `DepreciationRecurringJob.cs:36-44`, plus the recurring job that drives it. This is the *only* place that crosses the boundary; keep it small and heavily tested.
4. **Sender slice** — extend `CreateIssuanceReportCommand` with optional `DestinationTenantId`; on `Transfer*` nature, create the outbound offer in the same `SaveChanges` as the issuance report. Validate destination tenant exists and `IsActive` via `ITenantService`. Block if any line's `AccumulatedDepreciation` is still null.
5. **Receiver slices** — `SearchIncomingTransferOffers`, `GetTransferOffer`, `AcceptTransferOffer` (links offer → the receiving report the user posts), `RejectTransferOffer` (the projector carries the response back to A on its next pass).
6. **Notifications** — `NotificationRequestedIntegrationEvent` to B on offer arrival, back to A on accept/reject. ⚠️ `Notification` is `.IsMultiTenant()`, so setting `TenantId` on the *event* is not enough — `NotificationWriter` must run under the recipient tenant's ambient context. Route it through the same projector scope.
7. **Blazor UI** — "Incoming Transfers" list + detail with Accept/Reject; Accept navigates to the existing Create PPERR form **prefilled** from the offer (receiver still picks form series, property numbers, signatories). Destination-agency picker on the PPEIR create form.
8. **Permissions** — new `AssetRegisterPermissions.Transfers.*`. ⚠️ Must be added to `PermissionConstants._all` or `RequirePermission` returns 403 for *everyone*, admins included.

No extra sender-side approval gate: the PPEIR is already an approved accountable document with an `ApprovedBy` signatory resolved from the organization profile, so posting it is sufficient authorization.

---

## Deriving the destination from the recipient

The first cut asked for the destination agency **twice**: once as the "Issued To" employee, and again as a
separate "Destination Agency" dropdown built from the tenant registry. The two were unrelated — nothing
stopped a user naming a recipient at Agency X while sending the offer, and the assets' book values, to
Agency Y. The recipient is the better source of truth, so the destination is now derived from it.

### Why it could not simply be read off the employee

Three gaps had to be closed first; none of these were true when the feature first shipped:

| Assumption | Reality before this change |
| --- | --- |
| The tenant knows which office it is | `AppTenantInfo` held only `ConnectionString, AdminEmail, IsActive, ValidUpto, Issuer`. Tenant ids are free-form (`root`, `sdsbo`, `adsbo`). |
| Office location codes are unique | `OfficeConfiguration` puts the unique index on `Code`. `LocationCode` is nullable with no index at all. |
| An employee resolves to an agency | `EmployeeProfile.OfficeCode` (DTO: `OwnerOfficeCode`) is nullable — null means "shared, belongs to every agency" — and is create-only. |
| `AnnexECode` is a reliable agency key | Tenant-scoped free text, nullable, not unique across tenants, hand-copied from some `Office.RegProvCode` with no FK. |

Note `Office` and `EmployeeProfile` are **deliberately shared** across tenants (not `.IsMultiTenant()`), which
is exactly why the recipient picker can already see employees at other agencies.

### The link

`AppTenantInfo` gained `OfficeId` (soft reference — MasterData is a different DbContext, so no FK) plus an
`OfficeCode` display snapshot, with a **unique filtered index on `OfficeId`**: one office represents exactly
one agency, or resolution would be ambiguous. A `Guid` key rather than a code string keeps this immune to any
later re-keying of `LocationCode`/`RegProvCode`.

Routing therefore reads: **`EmployeeProfile.OfficeId` → the tenant whose `AppTenantInfo.OfficeId` matches.**
`OfficeId` is a required FK on the employee, so it is deterministic. The nullable `OwnerOfficeCode` stamp is
deliberately **not** used — its null case means "shared", which is unroutable by definition.

`TransferDestinationResolver` (`Data/Services/`) owns that chain and is used by both the query slice
(`ResolveTransferDestination`) and the create-issuance guard, so UI and server can never disagree.

### Why it is confirmed rather than silent

The PPEIR form shows the derived agency as a banner with a "Send the linked offer" checkbox, not as an
invisible side effect. Posting a transfer moves assets onto another agency's books and cannot be undone, so
a mis-picked name must be visible before saving — the same reasoning that keeps an offer from being
auto-accepted on the receiving side. A collapsed "Choose a different agency" override remains for the case
derivation cannot cover: the destination agency is on AMIS, but the named recipient is not, so there is no
employee row to derive from.

### Two guards added server-side

- **Recipient/destination agreement** — when `IssuedTo.EmployeeId` is set, its agency must equal
  `DestinationTenantId`, else 422. A hand-typed recipient (`Guid.Empty`) has no agency and is left alone.
- **`Transfers.Offer` enforced in the handler** — the endpoint can only demand one permission
  (`Issuance.Create`), and the UI's check is client-side, so without this a caller with issuance rights alone
  could post a destination straight to the API.

### Operational note

Tenants created before this change have `OfficeId = null` and cannot be auto-derived destinations. The
Tenants page shows a "Not linked" chip and a link action for exactly this reason; new tenants can pick their
office at creation.

---

## Security review points (non-negotiable)

- The projector is the single cross-tenant seam. Everything else runs under normal filters. Keep it that way.
- The offer payload is a **deliberate, minimal disclosure** to tenant B — only the lines being transferred. Never project the sending tenant's catalog, custodians, or unrelated assets.
- Validate `ToTenantId` against `ITenantService` (exists + `IsActive`) before writing. Never accept a tenant id from the client without that check.
- Receiving tenant must never be able to mutate the sender's document — only respond to the offer.

---

## Verification

- **Unit** — `AssetRegistry.Register` seeds accumulated depreciation; offer state machine rejects illegal transitions (accept-after-reject, double-accept).
- **Integration** (follow `src/Tests/AssetRegister.Tests/Integration/`, which already exercises tenant scopes — e.g. `DepreciationPostingServiceTests`, `AutoQueueUnserviceableOnAcceptTests`):
  - Post PPEIR in tenant A with destination B → assert inbound offer visible in B's scope, invisible in a third tenant C.
  - Accept in B → PPERR created on B's own series; asset registered with **carried-over** accumulated depreciation and original acquisition date; offer `Accepted` on **both** sides.
  - Reject → A's offer flips to `Rejected`; no asset created in B.
  - Run the projector twice over the same offer → the `CorrelationId` unique index makes the second pass a no-op (no duplicate inbound offer).
  - **Isolation regression:** assert a tenant-C context sees zero offers and zero transferred assets.
- **Property Card continuity** — `GetPropertyCardQueryHandler` computes the ledger on demand from source documents. Confirm the asset shows `TransferredOut` on A's card and `Acquired` on B's card, and that both reference the shared correlation.
- **End-to-end** — run with Aspire (`dotnet run --project src/Host/AMIS.AppHost`), two provisioned tenants, drive the full offer → accept path through the Blazor UI.
- `dotnet build src/AMIS.Framework.slnx` (0 warnings) and `dotnet test src/AMIS.Framework.slnx` before commit.

---

## Decisions taken

1. **Scope** — PPE only (PPEIR → PPERR) first; Semi-Expendable follows on the same plumbing.
2. **Deployment** — uniformly shared DB today. The design does not depend on that: the projector and the offer-as-outbox work identically if a tenant is later given its own connection string. Deliberately avoided the one shortcut that *would* couple us to shared-DB (sharing a `DbTransaction` across the two contexts).
3. **Trust model** — posting the PPEIR is sufficient authorization; no second approval gate.

## Incidental findings worth tracking separately

Both were surfaced while researching this feature. Neither is caused by it, and neither should be silently folded into this work:

- **Dead outbox tables.** Chat and Expendable declare `OutboxMessages`/`InboxMessages` DbSets and have the tables migrated, but `AddEventingForDbContext` is only ever called for `IdentityDbContext` — so those tables are never read or written, and all modules' event idempotency lands in Identity's schema. Worth a decision: wire them properly, or drop them.
- **`AddEventingForDbContext` is not composable.** It registers `IOutboxStore`/`IInboxStore` via plain `AddScoped`, so a second call anywhere silently overrides the first in load order. If outbox usage ever spreads beyond Identity, this needs a keyed/per-context resolution strategy first. This is BuildingBlocks code — protected; needs explicit approval before changing.
