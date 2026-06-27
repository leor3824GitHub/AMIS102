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

It also lights up the **@mention bell** for free, by consuming the `MentionedInChannelIntegrationEvent`
that Chat already publishes.

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
        │  publish NotificationRequestedIntegrationEvent   ──┐
Chat (SendMessageCommandHandler)                             │  IEventBus
        │  publish MentionedInChannelIntegrationEvent  ──────┤
        ▼                                                    ▼
Notifications module
        ├── NotificationRequestedConsumer  ──┐
        ├── MentionedInChannelConsumer     ──┤→ NotificationWriter
        │                                     │     ├── persist Notification row (denormalized)
        │                                     │     └── IHubContext<AppHub>.Clients
        │                                     │          .Group("user:{id}").SendAsync("NotificationCreated", dto)
        ▼                                     ▼
   notifications schema (DB)            AppHub  → Blazor ChatHubClient → INotificationState → bell UI
```

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
    ├── Modules.Notifications.csproj                # refs: Caching, Persistence, Web, .Contracts,
    │                                               #       Eventing, Chat.Contracts  (for the mention consumer)
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
    │   ├── NotificationRequestedConsumer.cs
    │   └── MentionedInChannelConsumer.cs
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
    string IIntegrationEvent.Source => Source;   // verify exact member shape vs MentionedInChannelIntegrationEvent
}
```

> ⚠️ Match the **exact** `IIntegrationEvent` member surface (`Id`, `OccurredOnUtc`, `Source`, and any
> `TenantId`) against `Chat.Contracts/Events/MentionedInChannelIntegrationEvent.cs` and
> `Eventing.Abstractions/IIntegrationEvent.cs` when implementing — the sketch above is indicative.

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

### 6.5 Consumers (Integration)

```csharp
internal sealed class NotificationRequestedConsumer(INotificationWriter writer)
    : IIntegrationEventHandler<NotificationRequestedIntegrationEvent>
{
    public Task HandleAsync(NotificationRequestedIntegrationEvent e, CancellationToken ct = default)
        => writer.WriteAsync(e, ct);
}

internal sealed class MentionedInChannelConsumer(INotificationWriter writer)
    : IIntegrationEventHandler<MentionedInChannelIntegrationEvent>
{
    public Task HandleAsync(MentionedInChannelIntegrationEvent e, CancellationToken ct = default)
        => writer.WriteAsync(new NotificationRequestedIntegrationEvent(
            RecipientUserId: e.MentionedUserId,
            Type: NotificationType.ChatMention,
            Title: "You were mentioned",
            Body: e.ContentPreview ?? "You were mentioned in a channel.",
            Link: $"/chat?channel={e.ChannelId}&message={e.MessageId}",
            Source: "Chat",
            MetadataJson: null,
            TenantId: e.TenantId,
            CorrelationId: e.CorrelationId), ct);
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
    services.AddScoped<IIntegrationEventHandler<MentionedInChannelIntegrationEvent>, MentionedInChannelConsumer>();
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
            Link: $"/procurement/job-orders/{jo.Id}",
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

| Step | File(s) | Note |
| --- | --- | --- |
| Handle the new event on the existing AppHub connection | `Services/Chat/ChatHubClient.cs` | Add `NotificationCreatedEvent` const, `_connection.On<NotificationDto>(...)`, and `event Action<NotificationDto>? NotificationReceived`. Reuses one connection — no second pipe. |
| Scoped session state | `Services/Notifications/NotificationState.cs` | Mirror `IUserProfileState` (scoped, `OnChanged` event). Holds unread count + recent items. |
| Owner / init | `Components/Layout/AMISLayout.razor` | Load initial unread count + recent list once per circuit; subscribe to `ChatHubClient.NotificationReceived` → update state; unsubscribe in `Dispose()`. |
| Bell component | `Components/Layout/` (new `NotificationBell.razor`) | Badge with unread count + dropdown of recent items; clicking an item calls `MarkRead` and navigates to `Link`. |
| Full page | `Components/Pages/Notifications/NotificationsPage.razor` (`@page "/notifications"`) | Wire the existing dead nav link (`NavMenu.razor:314`). Use `CollectionView`-equivalent MudList, compact controls per `blazor.md`. |
| Typed API client | `Services/Api/ApiClientRegistration.cs` | Register a Notifications client (follow how existing module clients are generated/registered). |
| Permission gate | page + bell | `Notifications.View` is IsBasic, so all authenticated users qualify; still read `UserProfileState.Permissions` for the page guard per `blazor.md`. |

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

- `NotificationWriterTests` — writes a row, pushes once, is idempotent on duplicate CorrelationId.
- `MentionedInChannelConsumerTests` — maps a mention event to a `ChatMention` notification.
- `IssueJobOrder` handler test — publishes `NotificationRequestedIntegrationEvent` when the inspector has a
  linked login; **skips** publishing (and logs) when `IdentityUserId` is null; never throws on publish
  failure.
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
5. @mention B in Chat → bell increments via the same path.

---

## 13. Phase checklist

- [ ] **P1** Notifications.Contracts (marker, permissions, enum, DTO, generic event)
- [ ] **P1** Notifications module (Domain, Data, DbContext/Factory/Initializer, config, migration)
- [ ] **P1** `NotificationWriter` + two consumers
- [ ] **P1** Feature slices (List, UnreadCount, MarkRead, MarkAllRead, Dismiss) + endpoints
- [ ] **P1** Module + AssemblyInfo (order 750) + host wiring + slnx
- [ ] **P2** ProcurementAcquisition → publish inspection-requested event on Issue
- [ ] **P3** Blazor: hub client event, `INotificationState`, bell, `/notifications` page, API client
- [ ] **P4** Tests + build (0 warnings) + smoke test

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
