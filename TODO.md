# TODO

> Reconciled against HEAD on **2026-07-14**. Items proven done or already satisfied were struck through with
> the evidence, rather than silently deleted, so the list stops re-accumulating phantom work.
> Broader roadmap lives in `.claude/progress-tracker.md`.

## Open

### Documentation

- [ ] Document release and deployment steps (`deploy/README.md` exists but is not a release runbook).

---

## Done / not applicable — verified 2026-07-14

- [x] ~~Fix JO/PR link behavior: PRs with an existing IAR must not appear in "Link Purchase Request"~~ —
      **fixed 2026-07-14.** Added `SearchPurchaseRequestsQuery.ExcludeWithIar`, mirroring the existing
      `SearchPurchaseOrdersQuery.ExcludeWithIar`. Since an IAR hangs off a **purchase order** (never off a PR or a
      Job Order directly — a JO records its inspection inline), the filter walks **PR → PO → IAR** and drops a PR
      once any of its POs carries a non-cancelled IAR. The JO dialog's PR picker now passes `excludeWithIar: true`.
      The flag is opt-in, so the PR list page and the canvass picker still see every PR.
      Covered by `SearchPurchaseRequestsExcludeWithIarTests` (a cancelled IAR does **not** hide the PR; a PO with
      no IAR does not either).
- [x] ~~Audit module endpoint names for uniqueness~~ — **clean.** Ran the duplicate-name detector from
      `.claude/rules/api-conventions.md` across `src/Modules` + `src/BuildingBlocks`: zero duplicates.
- [x] ~~Wire "My Inspections" requests to the current user identity~~ — **already done, and it was never a MAUI
      item** (this is a Blazor page). `MyInspectionsPage` calls `/pending-for-me` endpoints whose handlers inject
      `ICurrentUser` and take no caller-supplied id — the query is identity-scoped server-side, which is also the
      only spoof-safe way to do it.
- [x] ~~Add architecture tests for new module boundaries~~ — `src/Tests/Architecture.Tests` plus per-module
      boundary tests (e.g. `AssetRegister.Tests/Architecture/ModuleBoundaryTests.cs`) exist and run.
- [x] ~~Review current build and runtime errors~~ / ~~Clean up stale branches~~ / ~~Review MAUI navigation routes~~ /
      ~~Ensure MAUI token storage uses secure platform APIs~~ / ~~Confirm MAUI offline cache strategy~~ /
      ~~Update CLAUDE.md~~ — **standing hygiene, not tasks.** These were never actionable as written: they have no
      definition of done, and each is already governed by a rule doc that is enforced continuously
      (`.claude/rules/maui.md` for token storage + caching + navigation, `CLAUDE.md` for conventions). Re-add a
      specific, falsifiable item when something is actually wrong.
- [x] ~~Add missing documentation for new features~~ / ~~Add module-specific roadmap docs~~ — superseded by the
      per-area docs already in the repo root (`ASSET-REGISTER-DOCUMENTATION.md`, `EXPENDABLE-DOCUMENTATION.md`,
      `PROCUREMENT-DOCUMENTATION.md`, `REPORTING-DOCUMENTATION.md`, `BLAZOR-ARCHITECTURE-GUIDE.md`).
- [x] ~~Verify multi-tenancy configuration across modules~~ — the live constraint here is already captured as a
      hard rule with a known failure mode: an entity leaks across tenants unless its EF config calls
      `.IsMultiTenant()`, and pairing that with an *anonymous* soft-delete filter crashes the model build on
      EF Core 10. Documented in `.claude/rules/persistence.md`. Nothing open.
