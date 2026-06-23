# Upstream feature diff — what `dotnet-starter-kit` has that AMIS102 doesn't

> Comparison notes for later review. Source: [`leor3824GitHub/dotnet-starter-kit`](https://github.com/leor3824GitHub/dotnet-starter-kit)
> branch **`develop`** (not `main`). Captured 2026-06-23.

## Framing — the two repos diverged in opposite directions

The upstream is a **generic SaaS starter template**, so it kept generic infrastructure and sample
business modules. AMIS102 **replaced the sample business modules with a real government domain
suite** — AssetRegister, AssetManagement, ProcurementAcquisition, ProcurementPlanning,
BudgetDisbursement/Finance, Vehicle, MasterData, Expendable — plus three reporting engines
(Fast/Rdlc/QuestPdf), a `Blazor.UI` building block, and a MAUI client. **None of that exists upstream.**

So "what does upstream have that AMIS lacks" is almost entirely **cross-cutting infrastructure**, not
business features. The comparison is a two-way street: AMIS is far more domain-built-out; upstream is
more SaaS-infrastructure built-out.

Shared baseline (both have): Identity, Multitenancy, Auditing, and the FSH framework building blocks
(Core, Persistence, Caching, Eventing, Jobs, Mailing, Storage, Shared, Web).

## Modules in upstream, absent in AMIS

| Module | What it is | Relevance to AMIS |
| --- | --- | --- |
| **Notifications** | Per-user bell-icon inbox driven by cross-module integration events + live SignalR push. Inbox rows are denormalized (Title/Body/Link/MetadataJson copied in) so rendering never calls back into the source module. Order 750 (before Chat 800). | **High** — genuinely useful, AMIS has nothing like it. Natural companion to Chat (consumes the `MentionedInChannelIntegrationEvent`). |
| **Files** | Full presigned-URL lifecycle (`RequestUploadUrl → FinalizeUpload → serve → delete/restore`), `IFileAccessPolicy` per owner type, quota debit on finalize, scan status, orphaned/deleted purge jobs. Order 350 (before consumers). | **Medium** — AMIS already does uploads ad-hoc via the `Storage` building block (signed-doc storage, asset photos). The Files module is a more disciplined version, but working patterns already exist. |
| **Webhooks** | Tenant-scoped outbound webhook subscriptions, HMAC-signed delivery (`X-Webhook-Signature: sha256=…`), Hangfire retries (`{30,120,600,3600}s`), open-generic fan-out (`WebhookFanoutHandler<TEvent>`) over **every** `IIntegrationEvent`. Order 400. | **Medium** — useful if AMIS needs to push events to external government systems; otherwise skip. |
| **Tickets** | Support/helpdesk ticket lifecycle + comments. State machine `Open → InProgress → Resolved → Closed`; illegal transitions throw `CustomException` 409. Order 700. | **Low/Medium** — generic helpdesk feature; only if in-app support tickets are wanted. |
| **Billing** | Plans, subscriptions, invoices (+ line items), usage metering, monthly invoice job (`5 0 1 * *`). **Manual payment marking — no payment provider.** `BillingDbContext` is a plain `DbContext` (not `BaseDbContext`); `BillingPlan` is a global, non-tenant-scoped catalogue. Order 500. | **Low** — SaaS subscription billing. A government internal system has no paying tenants → probably irrelevant. |
| **Catalog** | Sample product catalog (images, soft-delete, filtered unique indexes, search). | **None** — the template's demo module. AMIS's Expendable / AssetRegister are richer real equivalents. |
| **Chat** | Slack-style messaging (DMs, channels, threads, reactions, mentions, pins, search) over SignalR. | See **CHAT-PORT-PLAN.md** — already analysed + port plan written. |

## Building blocks / cross-cutting in upstream, absent in AMIS

| Piece | What it is | Relevance |
| --- | --- | --- |
| **`BuildingBlocks/Web/Realtime`** | Single app-wide SignalR `AppHub` (groups `user:{id}`/`tenant:{id}`/`channel:{id}`), `PresenceTracker`, module-supplied adapter interfaces (`IChannelMembershipChecker`/`IUserChannelLookup`), optional Redis backplane. | **High** — foundation for Chat *and* Notifications. (Covered by CHAT-PORT-PLAN.md Phase 0.) |
| **`BuildingBlocks/Web/Sse`** | Server-Sent Events channel with two-step token handshake (`POST /sse/token` single-use 30s TTL → `GET /sse/stream` anonymous, consumes token), 15s heartbeat, bounded buffer (cap 100, DropOldest). | **Low/Medium** — alternative one-way push for clients that can't use WebSockets. SignalR already covers most needs. |
| **`BuildingBlocks/Quota`** | Per-resource usage quota service (`IQuotaService`, in-memory + Redis impls), `QuotaEnforcementMiddleware`, `QuotaPlanResolver`, `QuotaOptions`. | **Medium** — **partially overlaps** AMIS's existing Platform Settings (session + quota management). Upstream's version is per-resource metering tied to Billing. |

## Bottom line — what's actually worth porting

- **Notifications** — the clear win. Pairs directly with Chat; AMIS has zero in-app notification
  capability today. Would reuse the same `AppHub` as Chat.
- **Realtime (`AppHub`)** — coming anyway via the Chat plan; Notifications rides on it.
- **Files / Webhooks / Quota** — situational. Port only on a concrete need:
  external-system integration → Webhooks; stricter upload governance → Files; tenant metering
  beyond Platform Settings → Quota.
- **Billing / Catalog / Tickets** — SaaS-template baggage that doesn't fit a government asset
  system. Skip.

## Cross-reference

- Chat analysis + port plan: **CHAT-PORT-PLAN.md**
