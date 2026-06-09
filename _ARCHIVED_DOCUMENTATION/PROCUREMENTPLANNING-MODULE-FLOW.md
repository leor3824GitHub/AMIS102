# ProcurementPlanning Module — Detailed Flow

## Architecture Overview

```
Contracts (enums, DTOs, commands, queries)
   └─ consumed by Domain, Features, and Blazor client
Domain (Ppmp, AnnualProcurementPlan)
   └─ pure business logic, no EF/HTTP
Features/v1/{Slice}/
   ├─ Endpoint  → HTTP route + permission guard
   ├─ Handler   → orchestrates domain + repo
   └─ Validator → FluentValidation
Data/
   ├─ ProcurementPlanningDbContext
   └─ Configurations/{PpmpConfiguration, AnnualProcurementPlanConfiguration}
```

---

## Core Enums

### PPMP (department-level plan)

| `PpmpPhase`  | Meaning                    |
| ------------ | -------------------------- |
| `Indicative` | Early budget estimate      |
| `Final`      | Confirmed procurement plan |
| `Updated`    | Post-approval revision     |

| `PpmpStatus`   | Transitions               |
| -------------- | ------------------------- |
| `Draft`        | Starting state, editable  |
| `Submitted`    | Sent for approval         |
| `Approved`     | Cleared by approver       |
| `Consolidated` | Merged into an APP        |
| `Returned`     | Sent back for revision    |
| `Superseded`   | Replaced by a new version |

### APP (agency-wide plan)

| `AppPhase`   | Meaning                            |
| ------------ | ---------------------------------- |
| `Indicative` | Consolidated from Indicative PPMPs |
| `Final`      | Consolidated from Final PPMPs      |
| `Updated`    | Post-approval revision             |

| `AppStatus`  | Transitions                 |
| ------------ | --------------------------- |
| `Draft`      | Editable, accepting PPMPs   |
| `Published`  | Submitted for head approval |
| `Approved`   | Signed off                  |
| `Returned`   | Sent back for revision      |
| `Superseded` | Replaced by a new version   |

---

## PPMP Lifecycle

```
                ┌──────────────────────────────────────────────┐
  POST /ppmps   │  CreatePpmpCommand (Phase, OfficeCode, items)│
                └──────────────────┬───────────────────────────┘
                                   │ generates PpmpNumber
                                   ▼
                           [Status: Draft, VersionNumber: 1, IsCurrentVersion: true]
                                   │
           ┌───────────────────────┤ (editable — PUT /ppmps/{id})
           │                       │
           │                       ▼
           │               [Status: Returned]  ◄──── ReturnPpmpCommand (from approver)
           │                       │                 guard: Submitted only
           │                       │                 ◄── RecallPpmpCommand
           │                       │                     guard: Submitted → back to Draft
           └───────────────────────┤
                                   │ SubmitPpmpCommand
                                   │ guard: Draft|Returned, ≥1 item
                                   ▼
                           [Status: Submitted]
                                   │
                                   │ ApprovePpmpCommand
                                   │ guard: Submitted only
                                   ▼
                           [Status: Approved]
                                   │
                     ┌─────────────┼──────────────────────────────┐
                     │             │                              │
          ConsolidatePpmps      PromoteToFinalPpmpCommand      CreateUpdatePpmpCommand
          (by BAC Sec, into     guard: Approved+Indicative      guard: Approved|Consolidated
           an APP)               → new Ppmp row,                + Final|Updated phase
           guard: Approved        Phase=Final, Status=Draft,     → new Ppmp row,
           Phase must match APP   VersionNumber=1, items cloned  Phase=Updated, V+1,
                     │            Supersede() on old row         items cloned
                     ▼                                           Supersede() on old row
                [Status: Consolidated]
```

**Versioning:** Every `PromoteToFinal` / `CreateUpdate` inserts a **new row** sharing the same `VersionChainId`. The previous row's `IsCurrentVersion` → `false`, `Status` → `Superseded`.

---

## APP Lifecycle

```
 POST /annual-procurement-plans
  CreateAnnualProcurementPlanCommand(FiscalYear, Phase)
         │
         ▼
  [Status: Draft, empty — no items yet]
         │
         │ ConsolidatePpmpsCommand(AppId, PpmpIds[])
         │ guard: Draft|Returned; PPMPs must be Approved;
         │ phase must match (Indicative↔Indicative, Final↔Final, Updated↔Final|Updated)
         │ → copies PPMP items into AppLineItems + marks PPMPs as Consolidated
         ▼
  [Status: Draft, populated with line items]
         │
         │ PublishAnnualProcurementPlanCommand
         │ guard: Draft|Returned, ≥1 line item
         ▼
  [Status: Published]
         │
         ├── RecallAppCommand       → back to Draft (guard: Published)
         ├── ReturnAppCommand       → Status: Returned (guard: Published)
         │                            (re-consolidate, then re-publish)
         │
         │ ApproveAppCommand
         │ guard: Published only
         ▼
  [Status: Approved]
         │
         ├── PromoteToFinalAppCommand     ── guard: Approved + Indicative phase
         │    → new APP row, Phase=Final, Status=Draft, VersionNumber=1
         │    → EMPTY — BAC Sec consolidates Final PPMPs fresh
         │    → Supersede() on Indicative APP
         │
         └── CreateUpdateAppCommand       ── guard: Published|Approved + Final|Updated phase
              → new APP row, Phase=Updated, VersionNumber=prev+1
              → clones SourcePpmps + LineItems from prior APP
              → Supersede() on prior APP
```

---

## PPMP → APP Consolidation (Detail)

```
ConsolidatePpmpsCommand(AppId, [ppmpId1, ppmpId2, ...])
        │
        ├─ Validates APP phase vs PPMP phases (phase-matched rule)
        │     Indicative APP  → only Indicative PPMPs
        │     Final APP       → only Final PPMPs
        │     Updated APP     → Final or Updated PPMPs
        │
        ├─ For each PPMP:
        │     ├─ Adds AppSourcePpmp record (audit trail)
        │     ├─ Copies each PpmpItem → AppLineItem (with SourcePpmpId, SourcePpmpItemId)
        │     └─ PPMP.MarkConsolidated() → Status = Consolidated
        │
        └─ Re-consolidation is idempotent — existing entries for same PPMP are removed then re-added
```

---

## Version Chain (per office, per fiscal year)

```
Indicative v1 (Approved, Superseded)
        │ PromoteToFinal
        ▼
Final v1 (Draft → Approved, Superseded)
        │ CreateUpdate("budget revision")
        ▼
Updated v1 (Draft → Approved, Superseded)
        │ CreateUpdate("urgent item")
        ▼
Updated v2 (Draft → ...)

All rows: same VersionChainId, PreviousVersionId chain, only one IsCurrentVersion=true at a time
```

---

## API Surface (Permissions → Endpoints)

| Permission             | Endpoint                                                      | Method           |
| ---------------------- | ------------------------------------------------------------- | ---------------- |
| `Ppmps.Create`         | `POST /api/v1/ppmps`                                          | Create           |
| `Ppmps.Update`         | `PUT /api/v1/ppmps/{id}`                                      | Update           |
| `Ppmps.Submit`         | `POST /api/v1/ppmps/{id}/submit`                              | Submit           |
| `Ppmps.Approve`        | `POST /api/v1/ppmps/{id}/approve`                             | Approve          |
| `Ppmps.Return`         | `POST /api/v1/ppmps/{id}/return`                              | Return/Recall    |
| `Ppmps.PromoteToFinal` | `POST /api/v1/ppmps/{id}/promote-to-final`                    | PromoteToFinal   |
| `Ppmps.CreateUpdate`   | `POST /api/v1/ppmps/{id}/create-update`                       | CreateUpdate     |
| `Ppmps.View`           | `GET /api/v1/ppmps`, `GET /api/v1/ppmps/{id}`                 | Search/Get       |
| `Apps.Create`          | `POST /api/v1/annual-procurement-plans`                       | Create           |
| `Apps.Consolidate`     | `POST /api/v1/annual-procurement-plans/{id}/consolidate`      | ConsolidatePpmps |
| `Apps.Publish`         | `POST /api/v1/annual-procurement-plans/{id}/publish`          | Publish          |
| `Apps.Approve`         | `POST /api/v1/annual-procurement-plans/{id}/approve`          | Approve          |
| `Apps.Return`          | `POST /api/v1/annual-procurement-plans/{id}/return`           | Return/Recall    |
| `Apps.PromoteToFinal`  | `POST /api/v1/annual-procurement-plans/{id}/promote-to-final` | PromoteToFinal   |
| `Apps.CreateUpdate`    | `POST /api/v1/annual-procurement-plans/{id}/create-update`    | CreateUpdate     |
| `Apps.View`            | `GET /api/v1/annual-procurement-plans`, `GET /{id}`           | Search/Get       |

---

## Key Domain Invariants (enforced in domain, not handlers)

- You can only **Update** a `Draft` or `Returned` PPMP
- You can only **Submit** if there is at least one item
- You can only **Approve** a `Submitted` PPMP
- You can only **ConsolidatePpmps** into a `Draft`/`Returned` APP, and phases must match
- You can only **Publish** an APP that has at least one line item
- You can only **PromoteToFinal** an `Approved + Indicative` entity
- You can only **CreateUpdate** from `Final` or `Updated` phase (never directly from Indicative)
- The old version is **Superseded** (caller responsibility in handlers) when a new version is created
- Only **one** `IsCurrentVersion=true` row exists per `VersionChainId` at any time
