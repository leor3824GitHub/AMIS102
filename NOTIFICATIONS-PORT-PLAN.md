# Notifications Module — Port & Implementation Plan

> Implementation guide for porting the upstream **Notifications** module into AMIS102 and adding an
> **inspector-request notification** for Job Order inspection.
> Companion to `CHAT-PORT-PLAN.md` and `UPSTREAM-FEATURE-DIFF.md`. Authored 2026-06-27.

---

## 1. Goal

Build a per-user **bell-icon notification inbox** driven by cross-module integration events plus live
SignalR push, and use it to **notify the assigned inspector when a Job Order is issued and ready for
inspection**.

Two deliverables in one module:

1. **Generic Notifications module** — durable inbox + live push, reusable by any module.
2. **Inspector-request notification** — the first concrete producer: ProcurementAcquisition notifies the
   JO's assigned inspector the moment the JO is Issued.

**UX scope decision (2026-06-27): the bell is workflow-only.** It surfaces actionable items
(inspection requests, approvals, …) — NOT chat @mentions, which already appear in Chat's own unread
indicator. A non-zero badge therefore means "something is waiting on you", a signal users can trust.
This also keeps the Notifications module **fully decoupled from Chat** (zero cross-module reference). If
mentions are ever wanted in the bell, have Chat publish the generic `NotificationRequestedIntegrationEvent`
itself — no adapter inside Notifications.

---

## 2. Feasibility — confirmed ✅

| Requirement | Already in place | Reference |
| --- | --- | --- |
| The inspector is a known person, assigned per JO | `JobOrder.InspectorId` / `InspectorName` (frozen at creation) | `Domain/JobOrders/JobOrder.cs:95-97` |
| A clear "becomes actionable" trigger | JO `Issued` → `Inspected`; only an Issued JO can be inspected | `Domain/JobOrders/JobOrder.cs:313-335`, `IssueJobOrderCommandHandler.cs:25` |
| Employee id → login (identity) account mapping | `EmployeeReferenceDto.IdentityUserId` via `GetEmployeeReferenceByIdQuery` | `MasterData.Contracts/v1/References/EmployeeReferenceContracts.cs:9,88` |
| Live push to a single user | `AppHub` auto-joins every connection to `user:{id}` group on connect | `BuildingBlocks/Web/Realtime/AppHub.cs:62-92` |
| Durable-inbox seam | Chat already publishes `MentionedInChannelIntegrationEvent` "for a future durable Notifications module" | `Chat/.../SendMessage/SendMessageCommandHandler.cs:163` |
| Cross-module event delivery | `IEventBus` + `IIntegrationEventHandler<T>`, in-memory dispatch resolves handlers from DI | `Eventing.Abstractions/*`, `Eventing/InMemory/InMemoryEventBus.cs` |

### The one real caveat

`InspectorId` is an **employee id**. Notifications target an **identity user id** (`user:{guid}` group).
A directory-only employee with no login account (`IdentityUserId == null`) has nothing to push to.
**Handling:** resolve `IdentityUserId`; if null, log a warning and skip the live + durable notification.
Optional v2: email fallback via the `Mailing` building block.

---

## 3. Design decisions (defaults — change here if needed)

1. **Trigger = JO _Issued_**, not JO creation. Inspection is only valid on an Issued JO, so that is when
   the request becomes actionable.
2. **Generic event** `NotificationRequestedIntegrationEvent` that any module can publish — so future
   producers (PO approval, IAR acceptance, funds certification) reuse the same path with zero changes to
   the Notifications module.
2b. **Workflow-only bell — chat @mentions are NOT routed in.** The mention adapter and the Chat.Contracts
   reference were dropped; mentions stay in Chat's own unread indicator. Keeps the badge meaningful
   ("action needed") and the module Chat-free. Reversible: Chat can publish the generic event later.
3. **Resolve inspector → identity user at the source** (the procurement handler), so the Notifications
   module stays domain-agnostic and never references ProcurementAcquisition.
4. **Reuse the existing `AppHub` connection** in Blazor (extend `ChatHubClient`) — no second SignalR pipe.
5. **Realtime event name kept local** (`NotificationCreated` const in the module + Blazor client),
   mirroring how `ChatHubClient` already holds local copies — **so `BuildingBlocks/Web/Realtime` is not
   touched** (avoids the protected-building-block approval gate).
6. **No soft-delete** on the `Notification` entity — dismiss = hard delete. Avoids the
   `ef-softdelete-filter-conflict` pitfall on a multi-tenant entity.
7. **Module order 750** (before Chat's 850), per the upstream ordering.

---

## 4. Architecture

```
ProcurementAcquisition (IssueJobOrderCommandHandler)
        │  resolve InspectorId → IdentityUserId
        │  publish NotificationRequestedIntegrationEvent  ──┐ IEventBus
        ▼                                                   ▼
                                            Notifications module
                                            └── NotificationRequestedConsumer
                                                    │
                                                    ▼
                                              NotificationWriter
                                                ├── persist Notification row (denormalized) ──→ notifications schema (DB)
                                                └── IHubContext<AppHub>.Clients
                                                      .Group("user:{id}").SendAsync("NotificationCreated", dto)
                                                                            │
                                                                            ▼
                                              AppHub → Blazor ChatHubClient → INotificationState → bell UI
```

**Workflow-only: exactly one consumer.** Chat `@mentions` are NOT routed in (no `MentionedInChannelConsumer`,
no Chat reference) — they surface in Chat's own unread indicator. See §2 / §3.2b. Any future producer reuses
this same single path by publishing `NotificationRequestedIntegrationEvent`.

The inbox row is **denormalized** (Title / Body / Link / MetadataJson copied in) so rendering the bell
never calls back into the source module.

---

## 5. Module layout (mirrors `Modules/Chat`)

```
src/Modules/Notifications/
├── Modules.Notifications.Contracts/
│   ├── Modules.Notifications.Contracts.csproj      # refs: Mediator.Abstractions, Eventing.Abstractions, Shared
│   ├── NotificationsContractsMarker.cs
│   ├── Permissions/
│   │   └── NotificationPermissions.cs
│   ├── Events/
│   │   └── NotificationRequestedIntegrationEvent.cs
│   └── v1/
│       ├── Enums/NotificationType.cs
│       └── DTOs/NotificationDto.cs
└── Modules.Notifications/
    ├── Modules.Notifications.csproj                # refs: Caching, Persistence, Web, Eventing, .Contracts
    │                                               #       (NO Chat reference — workflow-only bell)
    ├── NotificationsModule.cs
    ├── NotificationsModuleConstants.cs
    ├── AssemblyInfo.cs                             # [assembly: AmisModule(typeof(NotificationsModule), 750)]
    ├── Data/
    │   ├── NotificationsDbContext.cs
    │   ├── NotificationsDbContextFactory.cs
    │   ├── NotificationsDbInitializer.cs
    │   └── Configurations/NotificationConfiguration.cs
    ├── Domain/
    │   └── Notification.cs
    ├── Services/
    │   ├── INotificationWriter.cs
    │   └── NotificationWriter.cs
    ├── Integration/
    │   └── NotificationRequestedConsumer.cs   # generic; no Chat-specific adapter (workflow-only bell)
    └── Features/v1/
        ├── ListMyNotifications/   (Query + Handler + Endpoint)
        ├── GetUnreadCount/        (Query + Handler + Endpoint)
        ├── MarkRead/              (Command + Handler + Validator + Endpoint)
        ├── MarkAllRead/           (Command + Handler + Endpoint)
        └── DismissNotification/   (Command + Handler + Validator + Endpoint)
```

---

## 6. Key types (sketches)

### 6.1 `NotificationType` (Contracts)

```csharp
namespace AMIS.Modules.Notifications.Contracts.v1.Enums;

public enum NotificationType
{
    General = 0,
    ChatMention = 1,
    InspectionRequested = 2,
    // add new producers here
}
```

### 6.2 Generic producer event (Contracts)

```csharp
using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.Notifications.Contracts.v1.Enums;

namespace AMIS.Modules.Notifications.Contracts.Events;

/// <summary>
/// Published by any module that wants to drop a row into a user's notification inbox. The Notifications
/// module owns the only consumer; producers stay decoupled from how the bell is rendered/stored.
/// </summary>
public sealed record NotificationRequestedIntegrationEvent(
    string RecipientUserId,      // identity user id (Guid string) — resolve at the producer
    NotificationType Type,
    string Title,
    string Body,
    string? Link,                // relative SPA route, e.g. /procurement/job-orders/{id}
    string Source,               // producing module name
    string? MetadataJson,
    string? TenantId,
    string CorrelationId) : IIntegrationEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
```

> As built: the positional `Source` parameter satisfies `IIntegrationEvent.Source` directly, so no explicit
> interface member is needed (only `Id` + `OccurredOnUtc` are added). Cf. `Eventing.Abstractions/IIntegrationEvent.cs`.

### 6.3 `Notification` aggregate (Domain)

Denormalized, tenant-scoped, no soft-delete:

```csharp
public sealed class Notification : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public string RecipientUserId { get; private set; } = default!;
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public string? Link { get; private set; }
    public string? MetadataJson { get; private set; }
    public string Source { get; private set; } = default!;
    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadOnUtc { get; private set; }
    public string CorrelationId { get; private set; } = default!;  // idempotency

    public static Notification Create(...);   // factory
    public void MarkRead();                   // sets IsRead + ReadOnUtc, idempotent
    // IAuditableEntity members (CreatedOnUtc, etc.)
}
```

**EF config** (`Data/Configurations/NotificationConfiguration.cs`) — follow `persistence.md` for a
multi-tenant entity:

```csharp
builder.ToTable("Notifications", NotificationsModuleConstants.SchemaName).IsMultiTenant();
builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
builder.HasIndex(x => new { x.RecipientUserId, x.IsRead });
builder.HasIndex(x => new { x.CorrelationId, x.RecipientUserId, x.Type }).IsUnique();  // idempotency
// NOTE: TenantId is intentionally omitted from this unique index. Safe only because the inspector
// producer uses CorrelationId = jo.Id (a global GUID). A future producer with a non-global key
// (e.g. "PO-2026-001") could collide across tenants — add TenantId to the index then.
```

### 6.4 `NotificationWriter` (the single reused path)

```csharp
internal sealed class NotificationWriter(
    NotificationsDbContext db,
    IHubContext<AppHub> hub,
    ILogger<NotificationWriter> logger) : INotificationWriter
{
    public const string NotificationCreatedEvent = "NotificationCreated";   // local const (see decision #5)

    public async Task WriteAsync(NotificationRequestedIntegrationEvent e, CancellationToken ct)
    {
        if (!Guid.TryParse(e.RecipientUserId, out var userGuid)) return;     // no targetable user

        // Idempotency: unique index guards redelivery; pre-check keeps the happy path clean.
        var exists = await db.Notifications.AnyAsync(
            n => n.CorrelationId == e.CorrelationId && n.RecipientUserId == e.RecipientUserId && n.Type == e.Type, ct);
        if (exists) return;

        var n = Notification.Create(e.TenantId ?? db.TenantInfo?.Identifier ?? "", e.RecipientUserId,
                                    e.Type, e.Title, e.Body, e.Link, e.Source, e.MetadataJson, e.CorrelationId);
        db.Notifications.Add(n);
        await db.SaveChangesAsync(ct);

        // After commit: best-effort live push, must never fail the request (cf. SendMessageCommandHandler).
        try
        {
            await hub.Clients.Group(AppHub.UserGroup(userGuid))
                .SendAsync(NotificationCreatedEvent, n.ToDto(), ct);
        }
        catch (Exception ex) { logger.LogWarning(ex, "Live push failed for notification {Id}.", n.Id); }
    }
}
```

### 6.5 Consumer (Integration)

A single generic consumer — the workflow-only bell takes no Chat-specific adapter.

```csharp
internal sealed class NotificationRequestedConsumer(INotificationWriter writer)
    : IIntegrationEventHandler<NotificationRequestedIntegrationEvent>
{
    public Task HandleAsync(NotificationRequestedIntegrationEvent e, CancellationToken ct = default)
        => writer.WriteAsync(e, ct);
}
```

### 6.6 Module registration (`NotificationsModule.cs`)

Mirror `ChatModule.ConfigureServices` / `MapEndpoints`:

```csharp
public void ConfigureServices(IHostApplicationBuilder builder)
{
    var services = builder.Services;
    PermissionConstants.Register(NotificationsModuleConstants.Permissions);   // Notifications.View (IsBasic)

    services.AddHeroDbContext<NotificationsDbContext>();
    services.AddScoped<IDbInitializer, NotificationsDbInitializer>();
    services.AddScoped<INotificationWriter, NotificationWriter>();

    // Explicit registration (matches AssetRegisterModule.cs:119 style)
    services.AddScoped<IIntegrationEventHandler<NotificationRequestedIntegrationEvent>, NotificationRequestedConsumer>();
}

public void MapEndpoints(IEndpointRouteBuilder endpoints)
{
    var apiVersionSet = endpoints.NewApiVersionSet().HasApiVersion(new ApiVersion(1)).ReportApiVersions().Build();
    var group = endpoints.MapGroup("api/v{version:apiVersion}/notifications")
        .WithTags("Notifications").WithApiVersionSet(apiVersionSet).RequireAuthorization();

    ListMyNotificationsEndpoint.Map(group);
    GetUnreadCountEndpoint.Map(group);
    MarkReadEndpoint.Map(group);
    MarkAllReadEndpoint.Map(group);
    DismissNotificationEndpoint.Map(group);
}
```

> Endpoint `WithName(...)` values must be **module-prefixed** and globally unique
> (`Notifications_ListMyNotifications`, …) per `api-conventions.md`.

---

## 7. The inspector hook (ProcurementAcquisition)

1. `Modules.ProcurementAcquisition.csproj` → add `<ProjectReference>` to
   `Modules.Notifications.Contracts` (allowed: modules may reference other modules' **Contracts**).
2. `IssueJobOrderCommandHandler` — inject `IEventBus` + `IMediator` (already has `IMediator`), and after
   `SaveChangesAsync`, best-effort publish:

```csharp
// after dbContext.SaveChangesAsync(...)
try
{
    var inspector = await mediator.Send(new GetEmployeeReferenceByIdQuery(jo.InspectorId), cancellationToken);
    if (!string.IsNullOrWhiteSpace(inspector?.IdentityUserId))
    {
        await eventBus.PublishAsync(new NotificationRequestedIntegrationEvent(
            RecipientUserId: inspector.IdentityUserId!,
            Type: NotificationType.InspectionRequested,
            Title: "Inspection requested",
            Body: $"Job Order {jo.JoNumber} is issued and ready for your inspection.",
            Link: $"/procurement/job-orders?inspect={jo.Id}",  // deep-links into the JO list page's inspect flow
            Source: "ProcurementAcquisition",
            MetadataJson: null,
            TenantId: jo.TenantId,
            CorrelationId: jo.Id.ToString()), cancellationToken);
    }
    else
    {
        logger.LogWarning("JO {JoNumber} inspector {InspectorId} has no linked login; inspection notice skipped.",
            jo.JoNumber, jo.InspectorId);
    }
}
catch (Exception ex) { logger.LogWarning(ex, "Failed to publish inspection-requested notification for JO {Id}.", jo.Id); }
```

   - **CorrelationId = `jo.Id`** → the unique index makes re-issue / redelivery idempotent (one inspection
     notice per JO).
   - Publish is **after commit** and wrapped — a notification failure must never roll back the Issue.
   - Confirm an `ILogger` is available in the handler (add injection if needed).

---

## 8. Blazor UI

### Placement decision — bell is a top-bar element, NOT a left-nav item

A workflow notification bell is **cross-cutting** (it spans procurement, finance, assets, …), so it belongs
to no nav section. The primary surface is a 🔔 icon + unread badge in the top app bar; the full-history
page is reached from the bell's "See all" link, not the sidebar.

- **Remove** the standalone `Notifications` link from the **Communication** section of
  `NavMenu.razor` (lines ~314-316). That section becomes **Chat only** — which is correct, Chat *is*
  communication. (The current link is also a dead 404 and ungated; both are resolved by removal.)
- The full page stays at `/notifications`, reached from the bell dropdown.

### Steps

| Step | File(s) | Note |
| --- | --- | --- |
| Remove the misfiled nav link | `Components/Layout/NavMenu.razor` | Delete the `/notifications` `MudNavLink` under "Communication" (~line 314). Bell replaces it. |
| Handle the new event on the existing AppHub connection | `Services/Chat/ChatHubClient.cs` | Add `NotificationCreatedEvent` const, `_connection.On<NotificationDto>(...)`, and `event Action<NotificationDto>? NotificationReceived`. Reuses one connection — no second pipe. |
| Scoped session state | `Services/Notifications/NotificationState.cs` | Mirror `IUserProfileState` (scoped, `OnChanged` event). Holds unread count + recent items. |
| Owner / init | `Components/Layout/AMISLayout.razor` | Load initial unread count + recent list once per circuit; subscribe to `ChatHubClient.NotificationReceived` → update state; unsubscribe in `Dispose()`. |
| Bell component (top bar) | `Components/Layout/NotificationBell.razor` (new), placed in `AMISLayout`'s `MudAppBar` next to the avatar | `MudBadge` unread count over a `MudMenu` bell; dropdown lists recent items + "Mark all read" + "See all" → `/notifications`. Clicking an item calls `MarkRead` and navigates to `Link`. |
| Full page | `Components/Pages/Notifications/NotificationsPage.razor` (`@page "/notifications"`) | Reached from the bell, not the sidebar. MudList, compact controls per `blazor.md`. |
| Typed API client | `Services/Api/ApiClientRegistration.cs` | Register a Notifications client (follow how existing module clients are generated/registered). |
| Permission gate | page + bell | `Notifications.View` is IsBasic, so all authenticated users qualify; still read `UserProfileState.Permissions` per `blazor.md`. |

UTF-8 save discipline applies to all `.razor`/`.cs` (peso/em-dash glyphs) — see `blazor.md`.

---

## 9. Host wiring (`AMIS.Api`)

1. `Program.cs` — `AddMediator` assemblies list: add `typeof(NotificationsModule)`,
   `typeof(NotificationsContractsMarker)`, and a representative event/command type.
2. `Program.cs` — `moduleAssemblies`: add `typeof(NotificationsModule).Assembly`.
3. `AMIS.Api.csproj` — add `<ProjectReference>` to both Notifications projects.
4. `AMIS.Framework.slnx` — add both projects.
5. `AssemblyInfo.cs` — `[assembly: AmisModule(typeof(NotificationsModule), 750)]`
   (**required** — without it the module is dormant despite a green build; see the
   *module-activation-attribute* memory).

---

## 10. Database migration

```powershell
dotnet ef migrations add InitialNotifications `
    --project src/Host/Migrations.PostgreSQL `
    --context NotificationsDbContext `
    --output-dir Migrations/Notifications
```

- Verify the migration **adds** the `notifications` schema/table and does not touch other contexts.
- Watch the multi-tenant + (no) soft-delete query-filter rule from `persistence.md` — since there is **no**
  soft-delete here, no named/anonymous filter conflict arises. If a soft-delete flag is added later, it
  **must** use the named-filter form alongside `.IsMultiTenant()`.
- Dev data is disposable (per the *development-phase-priorities* memory) — a destructive recreate is fine
  if the migration is awkward.

---

## 11. Tests (`src/Tests`)

- ✅ `NotificationWriterTests` (`src/Tests/Generic.Tests/Notifications/`) — 4 cases: persists a denormalized
  row + pushes once; idempotent on duplicate CorrelationId; skips an unparseable recipient; List/MarkRead are
  scoped to the calling user.
- ❌ ~~`MentionedInChannelConsumerTests`~~ — **dropped.** The workflow-only decision (§2 / §3.2b) removed the
  Chat adapter entirely; there is no mention consumer to test.
- ✅ `IssueJobOrderNotificationTests` (`src/Tests/ProcurementAcquisition.Tests/Integration/`) — 3 cases:
  publishes `NotificationRequestedIntegrationEvent` (asserting recipient/type/source/correlation/link/tenant)
  when the inspector has a linked login; **skips** publishing when `IdentityUserId` is null; never throws and
  still leaves the JO `Issued` when the event bus fails. Covers `IssueJobOrderCommandHandler.NotifyInspectorAsync`.
- Architecture tests already enforce module-boundary rules — confirm Notifications only references other
  modules' `.Contracts`.

---

## 12. Build & verify

```powershell
dotnet build src/AMIS.Framework.slnx    # 0 warnings required
dotnet test  src/AMIS.Framework.slnx    # all green
# Endpoint-name uniqueness check (api-conventions.md):
#   grep duplicate WithName(...) across src/Modules + src/BuildingBlocks
```

Manual smoke (two browser sessions / users):
1. User B is the assigned inspector on a JO and is logged in.
2. User A issues the JO.
3. B's bell increments live (SignalR) and the inbox row deep-links to the JO inspection screen.
4. Reload B — the row persists (durable). Mark read → badge clears. Re-issue does not duplicate.
5. (Negative check) @mention B in Chat → bell does NOT increment; the mention shows in Chat only.

---

## 13. Phase checklist

- [x] **P1** Notifications.Contracts (marker, permissions, enum, DTO, generic event)
- [x] **P1** Notifications module (Domain, Data, DbContext/Factory/Initializer, config)
- [x] **P1** `NotificationWriter` + generic consumer (workflow-only; no Chat adapter)
- [x] **P1** Feature slices (List, UnreadCount, MarkRead, MarkAllRead, Dismiss) + endpoints
- [x] **P1** Module + AssemblyInfo (order 750) + host wiring + slnx
- [x] **P2** ProcurementAcquisition → publish inspection-requested event on Issue
- [x] **P3** Blazor: hub client event, `INotificationState`, bell, `/notifications` page, API client, nav cleanup
- [x] **P4** Tests: `NotificationWriterTests` (4 cases) + `IssueJobOrderNotificationTests` (3 cases, producer
  hook) + full solution build (0 errors, 0 new warnings)
- [x] **P4** EF migration `Migrations.PostgreSQL/Notifications/…_InitialNotifications` generated (schema + unique idempotency index). Applied automatically on app startup by `NotificationsDbInitializer`.

> First-migration gotcha (recorded): EF's design-time scan only finds contexts that already have a migration
> (via the migration's `[DbContext]` attribute) in the migrations assembly; its factory scan does **not** reach
> referenced module assemblies, and the API-as-startup path is blocked while the app is running (DLL lock). To
> bootstrap, a throwaway `IDesignTimeDbContextFactory<NotificationsDbContext>` was added **in the
> Migrations.PostgreSQL project**, the migration generated, then the throwaway removed (the new snapshot makes
> the context self-discoverable thereafter).

> Note: `ListMyNotificationsQueryHandler` orders/pages **client-side** (mirrors `ListMyChannelsQueryHandler`)
> because SQLite — used in tests — cannot `ORDER BY` a `DateTimeOffset`. Postgres sorts it in-DB equally well.

---

## 14. Out of scope (v1)

- Email / push (mobile) fallback for unlinked inspectors — note as v2 via `Mailing`.
- Notification preferences / per-type mute.
- MAUI bell (the MAUI client can consume the same `AppHub` later — see `maui.md`).
- Grouping/threading of notifications.

---

## 15. Reference index (read before coding each part)

| Topic | Path |
| --- | --- |
| Module to mirror | `src/Modules/Chat/Modules.Chat/ChatModule.cs` |
| Realtime hub + groups | `src/BuildingBlocks/Web/Realtime/AppHub.cs` |
| Post-commit best-effort push pattern | `src/Modules/Chat/.../SendMessage/SendMessageCommandHandler.cs` |
| Integration consumer + DI registration | `src/Modules/AssetRegister/.../Integration/AssetIARAcceptedEventConsumer.cs`, `AssetRegisterModule.cs:119` |
| Event bus contracts/dispatch | `src/BuildingBlocks/Eventing.Abstractions/*`, `Eventing/InMemory/InMemoryEventBus.cs` |
| Employee → identity mapping | `src/Modules/MasterData/.../References/EmployeeReferenceContracts.cs` |
| Inspector + Issue trigger | `src/Modules/ProcurementAcquisition/.../Domain/JobOrders/JobOrder.cs`, `.../IssueJobOrder/IssueJobOrderCommandHandler.cs` |
| Blazor hub client to extend | `src/Host/AMIS.Blazor/Services/Chat/ChatHubClient.cs` |
| Blazor state pattern | `.claude/rules/blazor.md` (IUserProfileState) |
| Module conventions | `.claude/rules/modules.md` |
| API conventions (names!) | `.claude/rules/api-conventions.md` |
| Persistence / multi-tenant filters | `.claude/rules/persistence.md` |
```
