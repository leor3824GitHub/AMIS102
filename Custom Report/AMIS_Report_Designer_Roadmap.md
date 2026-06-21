# AMIS Report Designer — Phased Roadmap

> A pragmatic, value-first sequencing for building a dynamic/WYSIWYG report
> designer in AMIS. **Companion to the technical spec
> [`AMIS_Report_Designer_Plan.md`](AMIS_Report_Designer_Plan.md)** — that file is the
> "what & how"; this file is the "when, how much, and where to stop."

**Strategy:** renderer first → curated admin templates → structured editor → full
WYSIWYG. Risk and cost climb with each phase while *value per hour drops*. The
**cut-lines** mark where you can legitimately stop with usable value shipped.

**Estimate basis:** one experienced full-stack .NET 10 / Blazor / QuestPDF
developer, ~32 productive hours per week. Built to AMIS conventions (Mediator —
not MediatR; `ICommand`/`IQuery` + `ValueTask`; modular monolith vertical slices;
Finbuckle `IMustHaveTenant`; `BaseDbContext`; endpoints with `.RequirePermission()`).

---

## Phase 0 — Foundations & De-risking Spike
**Goal:** Prove the two riskiest things before committing to the full build.
**Hours: ~24–32**

- Stand up `Modules.ReportDesigner` skeleton (Contracts + impl, `ReportDesignerDbContext : BaseDbContext`, `IModule` wiring, one permission constant).
- **Spike the data-binding resolver**: nested paths (`Asset.Description`), a collection, date + ₱ currency formatting. Highest technical risk — prove it cheaply.
- **Spike multi-page QuestPDF**: a hardcoded AST → PDF with a table that repeats its header across 2+ pages and prints a totals footer.
- Confirm the AST storage shape (`jsonb` column per the plan, rule G1) by round-tripping a sample AST.

**Ships:** nothing user-facing. **Gate:** if binding or pagination looks ugly here, re-scope before spending real money.

> ⛔ **No cut-line — this is the entry fee.**

---

## Phase 1 — Runtime Rendering Engine + AST (the backbone)
**Goal:** A saved JSON template renders to a correct PDF, no recompile.
**Hours: ~70–95**  |  cumulative ~95–125

- Domain AST to AMIS standards: `BaseEntity`, `IMustHaveTenant`, `IAuditable`, factory methods, value objects, no public-setter bags.
- Hardened recursive QuestPDF interpreter (all node types, margins/padding/align/borders, page breaks, repeating headers, footers/totals).
- Real binding resolver from the Phase 0 spike (nested, collections, formatting, null-safe).
- Persistence + migration; one `GenerateReport` query slice (Mediator `IQuery`/`ValueTask`, validator, endpoint, `.RequirePermission()`).
- Engine unit tests + arch tests.

**Ships:** a developer can hand-author a template row and the system prints a correct, multi-page PDF for real AMIS data.

> ✅ **CUT-LINE 1 — "Configurable reports, dev-authored."** Replaces hardcoded report code with data-driven templates. Real value for engineering, none for end-users.

---

## Phase 2 — Admin-Defined Data Sources + Template Management
**Goal:** Admins compose safe, tenant-scoped data sources and create/manage templates against them.
**Hours: ~80–110**  |  cumulative ~175–235

- **First reportable source**: publish one module-backed `IReportableSource` end-to-end — start with `Assets` (built on `AssetRegister.Contracts`). Tenant-scoped, read-only rows projected to dictionaries (no reflection over live entities). Adding more sources later is one class + one DI line each.
- **Data Source Designer** (plan Phase 1b): an admin composes a `ReportDataSource` at runtime (pick fields/relations/filters); the bindable field catalog is **derived** from that saved spec — closes the reflection/injection hole and makes binding pickable, not typed.
- Full template CRUD slices (create/update/duplicate/delete/list) + validators + permissions.
- Basic template-management UI in Blazor (list, duplicate, delete, permission-gated via `UserProfileState.Permissions`).
- **Live Preview loop** (PDF round-trips into a viewer) — built here because every later phase reuses it.
- A raw/JSON or form-based property editor (no canvas yet).

**Ships:** an admin/power-user can define their own data sources, build and manage real templates against them, preview them, and roll them out — without a developer.

> ✅ **CUT-LINE 2 — "Admin-configurable templates." Highest-ROI stopping point.** Most "we need a report designer" requirements are actually satisfied here. Ship this, watch real usage for 4–6 weeks, and let demand decide whether Phases 3–4 are worth it. **Strongly recommend stopping and re-evaluating at this gate.**

---

## Phase 3 — Structured Visual Editor (tree + properties panel)
**Goal:** Visual editing for power-users — *without* the cost of free-form drag-drop.
**Hours: ~90–120**  |  cumulative ~265–355

- Recursive canvas component that **renders** the template visually with a selection model.
- Element toolbox/palette (add node into selected container).
- Per-node-type properties panel with **binding pickers** sourced from the admin-defined data source's *derived* catalog (Phase 2).
- Tree manipulation via explicit controls: add, delete, **move up/down, indent/outdent** (reparent without drag).
- Canvas↔PDF fidelity pass #1 (structurally faithful, "preview to confirm").

**Ships:** non-developers can visually assemble and edit templates. ~80% of WYSIWYG value.

> ✅ **CUT-LINE 3 — "Structured visual designer."** Genuinely self-service for moderately savvy users. Remaining gap vs. full WYSIWYG is *ergonomics*, not capability.

---

## Phase 4 — Full WYSIWYG Drag-and-Drop Canvas
**Goal:** True drag-to-place, drag-to-reorder, drag-to-reparent.
**Hours: ~80–130**  |  cumulative ~345–485

- Canvas drag-and-drop: drag from palette onto drop targets, reorder siblings, reparent into containers, with valid-drop-target rules and visual affordances.
- Drag interaction edge cases (nested containers, invalid drops, touch/trackpad).
- Fidelity pass #2.

**Ships:** the "drag boxes onto a page" experience the original plan promised.

> ⚠️ **Most expensive and most bug-prone phase for the *least* incremental capability** — it makes Phase 3 nicer, not more powerful. Only fund it if usage proves end-users won't accept the structured editor.

---

## Phase 5 — Polish & Productionization
**Goal:** Make it feel like a product.
**Hours: ~70–110**  |  cumulative ~415–595

- Undo/redo, copy/paste nodes.
- Template versioning/history.
- Advanced nodes (images/logos, conditional visibility, sub-totals/grouping, signatory blocks for gov forms).
- Accessibility, performance on large templates, hardening, broader fidelity tuning.

> ✅ **CUT-LINE 4 — "Polished product."**

---

## Summary

| Stop at | Cumulative hrs | ~Weeks (1 dev) | Who it serves |
|---|---|---|---|
| **Cut-line 1** (renderer) | ~95–125 | 3–4 | Engineering only |
| **Cut-line 2** (admin templates) | **~175–235** | **6–8** | **Admins/power-users — best ROI** |
| **Cut-line 3** (structured editor) | ~265–355 | 8–11 | Non-technical users (80% of WYSIWYG) |
| **Cut-line 4** (full WYSIWYG + polish) | ~415–595 | 13–19 | Full self-service product |

**Recommendation:** drive hard to **Cut-line 2**, then pause and let real usage
decide. Phases 3–4 are real work for diminishing returns; the data collected
after Cut-line 2 reveals whether end-users actually need to *author* reports or
just *use* a handful of good ones.

## Conventions

This roadmap covers only *sequencing and effort*. All code-level conventions —
Mediator (not MediatR) with `ICommand`/`IQuery` + `ValueTask` + validators; modular-
monolith vertical slices (`Modules.ReportDesigner` with `Domain/`, `Data/`,
`Features/v1/...`); Finbuckle `IMustHaveTenant`; rich domain with factory methods and
`private set`; `KeyNotFoundException`; and admin-defined data sources with a *derived*
binding catalog — are specified in
[`AMIS_Report_Designer_Plan.md`](AMIS_Report_Designer_Plan.md). See its conventions
table and detailed rules A–I; the plan is the source of truth for *how*.

## Plan-to-Roadmap Mapping

The plan uses "Parts" (internal build order); this roadmap uses "Phases" (delivery
sequence). They are not 1:1. See the cross-reference table at the bottom of
[`AMIS_Report_Designer_Plan.md`](AMIS_Report_Designer_Plan.md).

## Phase 0 Spike Targets

The two concrete things the spike must answer before spending real build hours:

| Risk | Spike target | Pass/fail signal |
|---|---|---|
| Token binding at scale | 500-row `Assets` table, nested `Custodian.FullName`, ₱ currency, date | < 500 ms render, correct formatting |
| QuestPDF pagination | Single template, 3-page output, repeating table header, page-number footer | Header appears on every page; page n/N correct |
| jsonb round-trip | Serialize + deserialize a 50-node AST; assert byte-equal | No field loss, stable across `ReportJson.Options` |

Fail on any of these → re-scope before Phase 1 investment.
