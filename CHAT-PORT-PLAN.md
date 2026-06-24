# Port the Chat module from the FSH starter kit into AMIS102

> Implementation plan. Status: drafted, not yet started. Scope decisions recorded below.

## Context

The upstream fork [`leor3824GitHub/dotnet-starter-kit`](https://github.com/leor3824GitHub/dotnet-starter-kit)
(branch **`develop`**, not `main`) ships a Slack-style **Chat** feature: 1:1 DMs, group DMs, named
channels, threads (single-level), reactions, @mentions, pins, and full-text search, with real-time
delivery over SignalR. AMIS102 shares the same FSH lineage, so the framework primitives line up
almost 1:1 — but three things differ and drive most of the real work (see below).

The upstream chat is **3 layers**:

1. `src/Modules/Chat` — the bounded context (CQRS vertical slices + DDD aggregates), schema `chat`.
2. `src/BuildingBlocks/Web/Realtime` — a **single app-wide SignalR hub** (`AppHub`) + presence
   tracker + module-supplied adapter interfaces. The Chat module never references the hub class;
   it pushes through `IHubContext<AppHub>` to well-known groups.
3. `clients/dashboard` (React SPA) — the chat UI.

A separate upstream `Notifications` module consumes the chat mention integration-event.

### What ports cleanly

The AMIS framework already has every primitive the upstream chat depends on, under `AMIS.Framework.*`
instead of `FSH.Framework.*`:

| Upstream needs | AMIS has | Location |
| --- | --- | --- |
| `IModule` (ConfigureServices / MapEndpoints) | ✅ identical | [IModule.cs](src/BuildingBlocks/Web/Modules/IModule.cs) |
| `AggregateRoot<TId>` / `DomainEvent.Create(...)` | ✅ identical | [AggregateRoot.cs](src/BuildingBlocks/Core/Domain/AggregateRoot.cs), [DomainEvent.cs](src/BuildingBlocks/Core/Domain/DomainEvent.cs) |
| `ISoftDeletable` (`IsDeleted/DeletedOnUtc/DeletedBy`) | ✅ identical | [ISoftDeletable.cs](src/BuildingBlocks/Core/Domain/ISoftDeletable.cs) |
| `ICurrentUser.GetUserId()/GetTenant()` | ✅ identical | [ICurrentUser.cs](src/BuildingBlocks/Core/Context/ICurrentUser.cs) |
| `IEventBus` / `IIntegrationEvent` (same `Id/OccurredOnUtc/TenantId/CorrelationId/Source`) | ✅ identical | [IEventBus.cs](src/BuildingBlocks/Eventing.Abstractions/IEventBus.cs), [IIntegrationEvent.cs](src/BuildingBlocks/Eventing.Abstractions/IIntegrationEvent.cs) |
| `AddHeroDbContext<T>` / `BaseDbContext` | ✅ | [Persistence](src/BuildingBlocks/Persistence/) |
| Mention resolution over user directory | ✅ `IUserService.GetListAsync(ct)` → `UserDto { Id, UserName, IsActive }` | [IUserService.cs](src/Modules/Identity/Modules.Identity.Contracts/Services/IUserService.cs) |
| JWT-in-query for WebSocket handshake | ✅ already implemented (needs one path tweak — see Phase 0) | [ConfigureJwtBearerOptions.cs](src/Modules/Identity/Modules.Identity/Authorization/Jwt/ConfigureJwtBearerOptions.cs#L71) |

So the module and contracts are largely a **mechanical namespace rename** plus the AMIS-specific
adaptations called out under "Gotchas."

### What does NOT port — read this first

| Concern | Upstream (FSH) | AMIS102 | Consequence |
| --- | --- | --- | --- |
| **Client UI** | React SPA (`@microsoft/signalr`, `realtime-context.tsx`) | **Blazor Server + MudBlazor + cookie auth** ([AMIS.Blazor](src/Host/AMIS.Blazor/)), plus MAUI | UI is a **rewrite**, not a port. The SignalR client runs **server-side inside the Blazor circuit**. |
| **Attachments** | dedicated `Files` module + `IFileAccessPolicy` | **No Files module** — only a `Storage` building block ([IStorageService](src/BuildingBlocks/Storage/Services/IStorageService.cs)) | **Deferred to v2** (decision below). Drop `MessageAttachment` from v1. |
| **Tenant isolation** | added late via a `ChatTenantIsolation` migration | AMIS convention = `.IsMultiTenant()` **+ named** soft-delete query filter from day one ([persistence.md](.claude/rules/persistence.md)) | Bake tenant isolation into the EF config up front. |
| **Mention notifications** | separate persistent `Notifications` module + bell inbox | none today | **v1 skips persistence** — push the mention straight to the user's SignalR group (decision below). |

## Architecture (target state in AMIS102)

```
Blazor Server circuit (AMIS.Blazor)            AMIS.Api host
  MudBlazor chat page                                  AppHub  @ /api/v1/realtime/hub   [Authorize]
  ChatHubClient (server-side SignalR client)  ──WS──►   groups: user:{id} tenant:{id} channel:{id}
   • AccessTokenProvider = existing JWT flow                 ▲
   • C# events: ChatMessageCreated, PresenceChanged…         │ IHubContext<AppHub>
   • invokes: JoinChannel(id), Typing(id)                    │
  ApiClient (REST) ──────────────────────────────────►  Modules/Chat  (CQRS slices)
                                                           SendMessage → save+outbox → COMMIT → broadcast
                                                          BuildingBlocks/Web/Realtime  (AppHub, PresenceTracker,
                                                           IChannelMembershipChecker, IUserChannelLookup)
                                                          Chat adapters satisfy those interfaces
```

The hub stays decoupled from Chat: it depends only on `IChannelMembershipChecker` / `IUserChannelLookup`,
which the Chat module implements (queries the membership table).

## Namespace / type mapping

| Upstream | AMIS102 |
| --- | --- |
| `FSH.Framework.Web.Realtime` | `AMIS.Framework.Web.Realtime` |
| `FSH.Framework.Core.Domain` / `.Context` / `.Exceptions` | `AMIS.Framework.Core.Domain` / `.Context` / `.Exceptions` |
| `FSH.Framework.Eventing.Abstractions` | `AMIS.Framework.Eventing.Abstractions` |
| `FSH.Modules.Chat[.Contracts]` | `AMIS.Modules.Chat[.Contracts]` |
| `FshPermission` + `PermissionConstants.Register` | `AmisPermission` + `PermissionConstants.Register` (see [ExpendableModule.cs](src/Modules/Expendable/Modules.Expendable/ExpendableModule.cs)) |
| `FSH.Modules.Identity.Contracts.Services.IUserService` | `AMIS.Modules.Identity.Contracts.Services.IUserService` |
| `AddHeroDbContext` / `AddHeroPlatform` / `UseHeroPlatform` | same names in AMIS |

---

## Build phases

### Phase 0 — Realtime infrastructure in BuildingBlocks/Web ⚠️ PROTECTED

> Touches BuildingBlocks → requires explicit approval per [buildingblocks-protection.md](.claude/rules/buildingblocks-protection.md).
> **✅ APPROVED (2026-06-24, leor3824):** explicit authorization granted to add/edit BuildingBlocks for
> this phase. Scope of the approval: the **new `src/BuildingBlocks/Web/Realtime/` folder**, the CPM
> package additions to `Directory.Packages.props` + `Web.csproj`, and the one-line JWT-over-WebSocket
> path tweak in the Identity module (not BuildingBlocks). Keep edits to existing BB files minimal —
> prefer `Program.cs` wiring over touching `AddHeroPlatform`/`UseHeroPlatform`. Make changes
> backward-compatible and run the full `dotnet test src/AMIS.Framework.slnx` suite after.
> Justification: the hub is genuinely cross-cutting (chat **and** any future presence/notifications
> consumer). It cannot live in the Chat module without other modules taking a dependency on Chat.

New folder `src/BuildingBlocks/Web/Realtime/`:

- **`AppHub.cs`** — `[Authorize] sealed class AppHub : Hub`. Groups `user:{id}`, `tenant:{id}`,
  `channel:{id}`. `OnConnectedAsync` joins user+tenant groups and pre-joins every channel the user
  already belongs to (via `IUserChannelLookup`); fires `PresenceChanged` to the tenant group on the
  first connection. Hub methods `JoinChannel(Guid)` (membership-gated, idempotent — for channels
  created after connect) and `Typing(Guid)` (throttled 3s/user/channel via `IDistributedCache`).
  **Reads identity from `Context.User`, NOT `ICurrentUser`** — `ICurrentUser` flows through
  `IHttpContextAccessor`, whose negotiate `HttpContext` is not pinned to subsequent hub invocations
  and returns nulls.
- **`IChannelMembershipChecker` / `IUserChannelLookup`** — adapter interfaces, implemented by Chat.
  ⚠️ `AppHub` (in BB) takes a **hard runtime dependency** on these, but only the Chat module registers
  the implementations. Realtime is therefore unbootable unless Chat is loaded — BB can't reference the
  module, so document this coupling (and fail fast with a clear message if the adapters are missing).
- **`IPresenceTracker` + `PresenceTracker`** — in-memory `ConcurrentDictionary<userId,count>`,
  per-host (single-replica only; documented as such).
- **`HeroRealtimeExtensions.cs`** — `AddHeroRealtime(config)` registers SignalR + the presence
  tracker, adding the StackExchange.Redis backplane when `CachingOptions:Redis` is set (channel
  prefix `amis-signalr`). `MapHeroRealtime()` maps the hub at `/api/v1/realtime/hub` + a
  `GET /api/v1/realtime/presence` snapshot endpoint. Give the presence endpoint a module-prefixed,
  globally-unique `.WithName("Realtime_GetPresence")` per [api-conventions.md](.claude/rules/api-conventions.md)
  (`MapHub` itself needs no name). The `Typing` throttle uses `IDistributedCache` — verify it resolves
  in dev with no Redis (`AddHeroCaching` should register an in-memory `IDistributedCache` fallback;
  confirm before relying on it).
- **Packages (CPM):** add `Microsoft.AspNetCore.SignalR.StackExchangeRedis` to
  `Directory.Packages.props`; reference it + `Microsoft.AspNetCore.SignalR` from `Web.csproj`.
  ⚠️ Watch the NU1015 CPM trap noted in memory — keep `Directory.Packages.props` well-formed.

<<<<<<< HEAD
**Host wiring** ([Playground.Api/Program.cs](src/Playground/Playground.Api/Program.cs)): call
`builder.AddHeroRealtime(...)` **before** `builder.Build()`, and `app.MapHeroRealtime()` **after**
`app.UseHeroPlatform(...)`. Ordering matters: `UseRouting` / `UseAuthentication` / `UseAuthorization`
all live inside `UseHeroPlatform` ([Extensions.cs:141-164](src/BuildingBlocks/Web/Extensions.cs#L141-L164)),
and the hub's `[Authorize]` needs them in front of it. Prefer `Program.cs` wiring over editing
`AddHeroPlatform`/`UseHeroPlatform` to keep the protected BB surface to just the new `Realtime/` folder.
=======
**Host wiring** ([AMIS.Api/Program.cs](src/Host/AMIS.Api/Program.cs)): call
`AddHeroRealtime` during service config and `MapHeroRealtime` after build (either inside
`AddHeroPlatform`/`UseHeroPlatform` in the Web BB, or directly in `Program.cs`).
>>>>>>> ac38fbedb80961dc03e9acb70ef2c656938fb8a7

**JWT-over-WebSocket tweak** — AMIS already lifts `access_token` from the query string in
[ConfigureJwtBearerOptions.cs:71](src/Modules/Identity/Modules.Identity/Authorization/Jwt/ConfigureJwtBearerOptions.cs#L71),
but it is gated to paths starting with `/notifications`. Extend that condition to also match the hub
path (`/api/v1/realtime/hub`) so the WebSocket/SSE handshake authenticates:

```csharp
var path = context.HttpContext.Request.Path;
if (!string.IsNullOrEmpty(accessToken) &&
    (path.StartsWithSegments("/notifications", StringComparison.OrdinalIgnoreCase) ||
     path.StartsWithSegments("/api/v1/realtime/hub", StringComparison.OrdinalIgnoreCase)))
{
    context.Token = accessToken;
}
```

### Phase 1 — Chat module backend

Two projects under `src/Modules/Chat/`, mirroring the upstream layout, AMIS-ified.

**`Modules.Chat.Contracts`** (refs: `Mediator.Abstractions`, `Eventing.Abstractions`, `Shared`):
- `Authorization/ChatPermissions.cs` — as `AmisPermission[]` (View/Create/ManageAll channels;
  Send/EditOwn/DeleteOwn/DeleteAny messages). No `CreateGlobal` permission in v1 — the single global
  channel is **seeded**, not user-created (add `CreateGlobal` only if/when general global channels land).
- `Events/MentionedInChannelIntegrationEvent.cs` — `: IIntegrationEvent`.
- `v1/Commands/*` , `v1/Queries/*`, `v1/DTOs/*` (`ChannelDto` — include `Scope`, `ChannelMemberDto`,
  `MessageDto`, `MessageReactionDto`, `ChannelType`, `ChannelMemberRole`, **`ChannelScope { Office, Global }`**).
  `ChannelScope` is orthogonal to `ChannelType` (the global channel is `Type=Named, Scope=Global`).
  **Omit** `MessageAttachmentDto` for v1.
- `ChatContractsMarker.cs`.

**`Modules.Chat`** (refs: `Caching`, `Persistence`, `Web`, `Eventing`, `Modules.Chat.Contracts`,
`Modules.Identity.Contracts`):
- `Domain/` — `ChatChannel` (aggregate, `ISoftDeletable`; `CreateChannel`/`CreateDirect`/
  `CreateGroupDm`/**`CreateGlobal`** factories — `CreateGlobal` sets `Scope=Global`, `TenantId=null`,
  `Type=Named`; the others set `Scope=Office` + current `TenantId`; sorted `DirectKey` `"{lo}:{hi}"` for
  find-or-create DM; `Archive`/`Restore` flip the flag, never EF `Remove()`), `ChannelMember`, `Message`
  (`Create`/`Edit`/`SoftDelete`/`Pin`/`Unpin`/`AddReaction`/`RemoveReaction`; soft delete = `DeletedAtUtc`
  tombstone, **not** `ISoftDeletable`), `MessageMention`, `MessageReaction`, `Domain/Events/*`. **Drop**
  `MessageAttachment`.
  - **Global channel = implicit membership.** The seeded `Global` channel has **no `ChannelMember`
    rows** — every authenticated user is a member by virtue of `Scope=Global`. The membership adapters
    special-case it (below) so `OnConnectedAsync` pre-joins it, `ListMyChannels` always includes it, and
    `RequireMember` always passes for it. This avoids provisioning a membership row per user (and on every
    new hire). DMs and `Office` channels keep explicit `ChannelMember` rows as before.
  - **DM find-or-create race:** put a **UNIQUE index on `(TenantId, DirectKey)`** and have
    `FindOrCreateDm` catch the unique-violation and re-SELECT — two users opening the same DM at once
    both miss the lookup and both INSERT, so the loser must fall back to the winner's row rather than
    surface a 500 or create a duplicate DM.
  - **Reactions idempotency:** UNIQUE index on `(MessageId, UserId, Emoji)` so double-taps and
    reconnect replays can't create duplicate reaction rows; `AddReaction` is then naturally idempotent.
  - **Message tombstone has no global query filter** (it's `DeletedAtUtc`, not `ISoftDeletable`), so
    **every** message read slice — list, replies, pinned, search, parent reply-count — must hand-filter
    `WHERE DeletedAtUtc IS NULL`. UI renders a "message deleted" placeholder rather than dropping the row.
- `Data/ChatDbContext.cs` — `: BaseDbContext`, `HasDefaultSchema("chat")`, **`base.OnModelCreating`
  FIRST, then `ApplyConfigurationsFromAssembly`** (the AMIS convention — see
  [BudgetDisbursementDbContext.cs:39-40](src/Modules/BudgetDisbursement/Modules.BudgetDisbursement/Data/BudgetDisbursementDbContext.cs#L39-L40)).
  The ctor mirrors the real `BaseDbContext` signature — **four args**, not just `DbContextOptions`:
  `(IMultiTenantContextAccessor<AppTenantInfo> mtca, DbContextOptions<ChatDbContext> options,
  IOptions<DatabaseOptions> settings, IHostEnvironment env)` → `: base(mtca, options, settings, env)`
  (the rule-doc `: BaseDbContext(options)` snippet is illustrative only). `ChatDbContextFactory`
  (`IDesignTimeDbContextFactory<ChatDbContext>`) must construct it with these four args. `Data/Configurations/*`,
  `ChatDbInitializer`, `ChatDbContextFactory`.
  - **`ChatDbInitializer.SeedAsync` seeds the one `Global` channel** using a **fixed well-known `Guid`**
    (e.g. a constant in `ChatModuleConstants.GlobalChannelId`) so it's idempotent: `FindAsync(id)` → if
    null, insert via `ChatChannel.CreateGlobal(...)` with `TenantId = null`. ⚠️ If `SeedAsync` runs
    per-tenant in this framework, the fixed-id existence check makes re-entry a no-op — it must **not**
    create one global channel per office.
- `Features/v1/` — vertical slices (command/handler/validator/endpoint each):
  - **Channels:** CreateChannel, FindOrCreateDm, ListMyChannels, DiscoverChannels, GetChannelById,
    UpdateChannel, ArchiveChannel, RestoreChannel, AddChannelMembers, RemoveChannelMember,
    MarkChannelRead.
  - **Messages:** SendMessage, EditMessage, DeleteMessage, PinMessage, UnpinMessage,
    ListChannelMessages, ListMessageReplies, GetPinnedMessages.
    - **`ListChannelMessages` uses keyset/cursor pagination, NOT offset.** Key on the time-sortable
      `Guid.CreateVersion7()` message id ("messages before id X, take N"). Offset paging double-shows
      or skips rows once a live feed shifts the window — and `MudVirtualize` infinite-scroll depends on
      a stable cursor. Hard to change after the response contract ships, so bake it in now.
  - **Reactions:** AddReaction, RemoveReaction.
  - **Search:** SearchMessages — **v1 uses `EF.Functions.ILike` over `Message.Content`**, matching every
    other AMIS search handler ([SearchProductsQueryHandler.cs:29-32](src/Modules/Expendable/Modules.Expendable/Features/v1/Products/SearchProducts/SearchProductsQueryHandler.cs#L29-L32)).
    ⚠️ Do **not** port the upstream Postgres tsvector full-text search in v1: nothing in AMIS uses FTS,
    and it forces a Npgsql-specific migration (`tsvector` generated column + GIN index via
    `HasGeneratedTsVectorColumn`/`HasMethod("GIN")` or raw SQL) plus `EF.Functions.ToTsVector().Matches`.
    Defer ranked FTS to v2 alongside attachments.
  - `Features/v1/Internal/ChannelAuthorization.cs` — `RequireMember` throws **NotFound (404)** so
    non-members can't probe channel existence; `RequireAdmin` throws `ForbiddenException`.
- `Services/` — `ChannelMembershipChecker : IChannelMembershipChecker`,
  `UserChannelLookup : IUserChannelLookup` (the hub adapters), `MentionParser`,
  `IMentionResolver` + `MentionResolver` (over `IUserService.GetListAsync`).
  Both adapters **special-case the global channel**: `ChannelMembershipChecker.IsMember` returns `true`
  for any authenticated user when the channel's `Scope == Global`; `UserChannelLookup` always appends the
  global channel id to a user's channel list (so the hub pre-joins `channel:{globalId}` for everyone).
  Membership queries for `Office` channels must still **scope tenant explicitly from `Context.User`** (the
  tenant filter is not reliably ambient inside the hub — see #4).
- `ChatModule.cs` — `PermissionConstants.Register(ChatPermissions.All)`;
  `AddHeroDbContext<ChatDbContext>`; register `IDbInitializer`, validators, the two hub adapters,
  `IMentionResolver`; health check; map endpoints under `api/v{version}/chat` with
  `.RequireAuthorization()`. **Module Order after any future Notifications module.**

`SendMessageCommandHandler` flow (the spine of the feature): verify membership → validate parent
(single-level threads only) → parse `@username` → resolve to ids (drop self/unresolved) → create
`Message` → `TouchLastMessage` + bump parent reply count → **enqueue** one
`MentionedInChannelIntegrationEvent` per distinct mentioned user **into the Outbox** → `SaveChanges`
(message + outbox rows in one transaction) → **COMMIT** → **only then** broadcast `ChatMessageCreated`
to `channel:{id}` via `IHubContext<AppHub>` and best-effort push `ChatMentioned` to each `user:{id}`.

⚠️ **Ordering is correctness, not style:**
- **Broadcast only after the transaction commits.** Pushing `ChatMessageCreated` before commit lets a
  client refetch and not see the row (or see a phantom after a rollback). Raise a domain event and fire
  the SignalR push in an after-commit step, never mid-`SaveChanges`.
- **Publish the mention integration event via the existing Outbox** (Expendable already wires
  `OutboxMessageConfiguration` — reuse that pattern) so it's transactional with the message insert; a
  future Notifications consumer then can't miss or double-count mentions. The live `ChatMentioned`
  SignalR push stays best-effort (fire-and-forget after commit).

**Endpoint naming:** per [api-conventions.md](.claude/rules/api-conventions.md) + the
endpoint-names memory, `.WithName(...)` must be globally unique — prefix every name with the module,
e.g. `"Chat_SendMessage"`. Do **not** use `nameof(SendMessageCommand)` (the command lives in the
shared Contracts assembly).

**Host registration** ([Program.cs](src/Host/AMIS.Api/Program.cs)): add representative
Chat types to the Mediator `Assemblies` list, add `typeof(ChatModule).Assembly` to `moduleAssemblies`,
add both `.csproj` to [AMIS.Framework.slnx](src/AMIS.Framework.slnx) and as refs in
[AMIS.Api.csproj](src/Host/AMIS.Api/AMIS.Api.csproj).

### Phase 2 — Migrations

Generate `InitialChat` for `ChatDbContext` into `Migrations.PostgreSQL/Chat/` with its snapshot.
⚠️ The migrations-discovery memory ("context found via its snapshot") applies to *existing* contexts.
`ChatDbContext` is **brand new — it has no snapshot yet**, so the **first** `migrations add` discovers
it through its `IDesignTimeDbContextFactory<ChatDbContext>` (`ChatDbContextFactory`). That factory must
build the context with the real four-arg `BaseDbContext` ctor (see Phase 1). After the first migration
exists: **generate, never hand-edit**, and don't delete the snapshot.

### Phase 3 — Mentions without a Notifications module (v1)

The upstream `Notifications` module (persistent bell inbox) is **out of scope for v1**. Instead,
`SendMessage` pushes the mention straight to the mentioned user's `user:{id}` SignalR group
(`hub.Clients.Group($"user:{id}").SendAsync("ChatMentioned", …)`). The
`MentionedInChannelIntegrationEvent` is still published so a future persistent Notifications module
can subscribe without changing Chat. Add that module later if a durable inbox is wanted.

### Phase 4 — Blazor UI (the bulk of the work)

In [AMIS.Blazor](src/Host/AMIS.Blazor/):

- **`ChatHubClient`** — a circuit-scoped service wrapping `Microsoft.AspNetCore.SignalR.Client`.
  `HubConnectionBuilder().WithUrl(apiBase + "/api/v1/realtime/hub", o => o.AccessTokenProvider = …)`,
<<<<<<< HEAD
  feeding the **existing JWT** the REST `ApiClient` already uses. ⚠️ `AccessTokenProvider` is a
  `Func<Task<string?>>`, so it **cannot** call `AuthorizationHeaderHandler` (that's an `internal sealed
  DelegatingHandler` in the HttpClient pipeline, not callable here). Read the token from the same source
  the handler does: `() => Task.FromResult(ICircuitTokenCache.AccessToken ?? <access_token claim on
  HttpContext.User>)` — see
  [AuthorizationHeaderHandler.cs:186-213](src/Playground/Playground.Blazor/Services/Api/AuthorizationHeaderHandler.cs#L186-L213)
  and [CircuitTokenCache.cs](src/Playground/Playground.Blazor/Services/Api/CircuitTokenCache.cs). Both
  types are `internal` to `Playground.Blazor`, so `ChatHubClient` (same assembly) can use them directly.
  Exposes C# events (`ChatMessageCreated`, `PresenceChanged`, `ChatTypingStarted`, `ChatMentioned`)
  and `JoinChannel`/`Typing` invokes. `IAsyncDisposable`, tied to the circuit lifetime; reconnect
  with backoff. **On every (re)connect, re-read the latest token from `ICircuitTokenCache`** so a
  refresh that happened on the REST path is picked up (see token-expiry risk below).
  `InvokeAsync(StateHasChanged)` to marshal hub callbacks onto the render thread.
- **ApiClient methods** for the chat REST endpoints. ⚠️ [ApiClient/Generated.cs](src/Playground/Playground.Blazor/ApiClient/Generated.cs)
  is **NSwag `<auto-generated>`** — do **not** hand-edit it. Either (a) regenerate it from the API's
  OpenAPI doc so the chat endpoints appear automatically (preferred — matches how the rest of the app
  is built), or (b) hand-write a `ChatClient.cs` partial alongside the other
  [ApiClient/*Client.cs](src/Playground/Playground.Blazor/ApiClient/) extension clients (e.g.
  `MasterDataClientExtensions.cs`). Pick one.
=======
  feeding the **existing JWT** the REST `ApiClient` already uses (decision below — reuse
  [AuthorizationHeaderHandler.cs](src/Host/AMIS.Blazor/Services/Api/AuthorizationHeaderHandler.cs)
  / [TokenRefreshService.cs](src/Host/AMIS.Blazor/Services/Api/TokenRefreshService.cs)).
  Exposes C# events (`ChatMessageCreated`, `PresenceChanged`, `ChatTypingStarted`, `ChatMentioned`)
  and `JoinChannel`/`Typing` invokes. `IAsyncDisposable`, tied to the circuit lifetime; reconnect
  with backoff. `InvokeAsync(StateHasChanged)` to marshal hub callbacks onto the render thread.
- **ApiClient methods** for the chat REST endpoints (follow the existing
  [ApiClient/Generated.cs](src/Host/AMIS.Blazor/ApiClient/Generated.cs) pattern).
>>>>>>> ac38fbedb80961dc03e9acb70ef2c656938fb8a7
- **MudBlazor components** under `Components/Pages/Chat/`: channel rail, message list
  (`MudVirtualize`), composer, typing indicator, mention picker, pinned panel, search. Follow
  [blazor.md](.claude/rules/blazor.md): prefer `AMISTextField`/`AMISButton`/`AMISSelect`; gate
  action buttons on `UserProfileState.Permissions.Contains(...)`; **save every `.razor`/`.cs` as
  UTF-8** (peso/em-dash corruption memory).
- **NavMenu** entry gated by `ChatPermissions.Channels.View`.

### Phase 5 — MAUI client (later, out of scope)

If mobile chat is wanted, the [maui.md](.claude/rules/maui.md) SignalR-client + Shell-navigation
patterns apply. Not part of this plan.

---

## AMIS-specific gotchas (don't lose these in the rename)

1. **Multi-tenancy is explicit — and chat uses a *custom* filter, NOT `.IsMultiTenant()`, to support
   the global channel.** Because v1 ships one NFA-wide `Global` channel (scope decision below), a channel
   has no single owning office, so Finbuckle's `.IsMultiTenant()` filter (`TenantId == currentTenant`)
   would hide it from everyone — and `.IsMultiTenant()` can't express "my tenant **OR** global." So:
   - **`ChatChannel`:** **nullable** `TenantId` (office channels = the office id; the global channel =
     `null`). Do **not** call `.IsMultiTenant()`. Hand-roll one combined named query filter that reads the
     current tenant captured from the injected `IMultiTenantContextAccessor<AppTenantInfo>` in the ctor:
     `builder.HasQueryFilter("OfficeOrGlobal", c => (c.TenantId == _currentTenantId || c.TenantId == null) && !c.IsDeleted);`.
     `TenantId` is assigned by the domain factory (`CreateChannel`/`CreateDirect`/`CreateGroupDm` →
     current tenant; `CreateGlobal` → `null`), since there's no Finbuckle auto-stamp without `.IsMultiTenant()`.
   - **`ChannelMember` / `Message` / `MessageMention` / `MessageReaction`:** **not** tenant-filtered at all
     — they are reachable only through a channel the caller is a member of, so **channel membership is the
     authorization boundary** in both modes. (They may carry a denormalized `TenantId` for display/reporting,
     but it is never a query filter.) This is what makes the same `Message` table serve both office and
     global channels.
   > EF Core 10 named/anonymous-filter note still applies *if* you ever add `.IsMultiTenant()` to a chat
   > entity (it registers a named filter, so any companion soft-delete filter must also be named). In this
   > design no chat entity uses it, so the single combined filter above is sufficient. See
   > [persistence.md](.claude/rules/persistence.md).
2. **Child-collection INSERTs.** `MessageConfiguration` sets `Property(x => x.Id).ValueGeneratedNever()`
   and the domain assigns `Guid.CreateVersion7()` in factories. Without this EF treats nav-collection
   children (mentions/reactions) as `Modified` → 0-row UPDATE instead of INSERT.
3. **`base.OnModelCreating` runs FIRST** in `ChatDbContext`, *then* `ApplyConfigurationsFromAssembly`.
   This is the verified AMIS convention ([BudgetDisbursementDbContext.cs:39-40](src/Modules/BudgetDisbursement/Modules.BudgetDisbursement/Data/BudgetDisbursementDbContext.cs#L39-L40),
   [ExpendableDbContext.cs:56-57](src/Modules/Expendable/Modules.Expendable/Data/ExpendableDbContext.cs#L56-L57)).
   ⚠️ The upstream FSH `BaseDbContext` wants base **last**; AMIS's does not — do **not** carry the
   "base last" pattern over. AMIS applies `.IsMultiTenant()`/named soft-delete filters per-entity in each
   `IEntityTypeConfiguration`, so config order relative to `base` does not need the FSH inversion.
4. **`Context.User`, not `ICurrentUser`, inside the hub** (returns null otherwise).
5. **Endpoint `WithName` global uniqueness** — module-prefixed, never `nameof(ContractCommand)`.
6. **UTF-8 saves** for all Blazor files.

## Scope decisions (settled)

- **Attachments:** ❌ deferred to v2. v1 is text + mentions + reactions + pins. Drop the
  `MessageAttachment` entity, DTO, and file-access plumbing entirely.
- **Hub auth (Blazor):** ✅ reuse the existing JWT token flow; no new auth surface. The
  `AccessTokenProvider` reads the token from `ICircuitTokenCache` (fallback: `access_token` claim) —
  the same source `AuthorizationHeaderHandler` uses — **not** the handler itself (it's a
  `DelegatingHandler`, not invokable by SignalR). Refresh is still driven by `TokenRefreshService` on
  the REST path; the hub picks up refreshed tokens on its next (re)connect.
- **Notifications:** ❌ no separate module in v1 — push mentions over SignalR (Phase 3); publish the
  integration event for a future durable inbox.
- **Tenant scope of channels:** ✅ **office-isolated by default + one seeded NFA-wide `Global` channel**
  (chosen 2026-06-24). Per-office channels/DMs stay private to that office (tenant-scoped); a single
  built-in `Global` channel (scope `Global`, `TenantId = null`) is visible to **every** authenticated
  user across all offices, with **implicit membership** (no `ChannelMember` rows — everyone is a member
  by virtue of the scope). This is a **net-new capability** — neither upstream FSH nor a vanilla port has
  cross-tenant chat. ❌ *Not* in v1: admin-created arbitrary global channels (general capability), and
  cross-office DMs — both deferred. The `ChannelScope` enum + nullable `TenantId` foundation laid here
  makes adding them later non-breaking.

## Recommended v1 cut

DMs + group DMs + named channels + send/edit/delete + reactions + @mentions + single-level threads +
pins + search (ILIKE) + typing + presence. No attachments, no persistent notifications, no ranked FTS,
Blazor-only client. This is the smallest slice that is genuinely "chat" and exercises the full realtime
path end-to-end.

## Sequencing & validation

**Build a thin vertical slice end-to-end first, then fan out.** The current phase order is layer-by-layer
(all of Phase 0, then all of Phase 1…), but Phase 0 is PROTECTED-BuildingBlocks work that can't be
validated until the Blazor client exists in Phase 4. Reduce risk by first shipping a **walking skeleton**:
hub + `SendMessage` + `ListChannelMessages` + one minimal Blazor page — proving handshake auth, the
circuit hub-client, token flow, and the commit→broadcast path while the surface area is small. Only then
build out the remaining slices (edit/delete, reactions, pins, search, mentions, presence UI).

**Per-phase "done when":**

| Phase | Done when |
| --- | --- |
| 0 — Realtime BB | Hub handshake authenticates with a query-string JWT; `OnConnectedAsync` joins `user:`/`tenant:`/`channel:` groups (verify with `wscat` or an integration test); `GET /api/v1/realtime/presence` returns a snapshot; `dotnet build` 0 warnings. |
| 1 — Chat module | `dotnet build src/AMIS.Framework.slnx` + **`Architecture.Tests` green** (they assert Chat doesn't reach into other modules' internals — referencing `Modules.Identity.Contracts` is allowed; reaching into `Modules.Identity` internals fails the suite). `WithName` uniqueness check (api-conventions grep) clean. |
| 2 — Migrations | `InitialChat` generated via the factory; `database update` applies; schema `chat` present with the `(TenantId, DirectKey)` and `(MessageId, UserId, Emoji)` unique indexes. |
| 3 — Mentions | `@user` in a message delivers a live `ChatMentioned` to that user's circuit; one outbox row enqueued per distinct mention. |
| 4 — Blazor UI | Two browsers in the same channel see each other's messages, typing, presence, reactions, and pins in real time; nav entry hidden without `ChatPermissions.Channels.View`. **Cross-office check:** two users in *different* offices (tenants) both see and can post to the seeded `Global` channel, but **cannot** see each other's office-only channels. |

**Spine unit test (matches the repo's `Generic.Tests` convention):** one `SendMessageCommandHandler`
test — member sends with `@self @stranger @realuser` → message persisted, only `realuser` resolved, one
mention outbox row enqueued, self and unresolved dropped. This guards the most complex slice.

**Ship-dark:** the nav entry and endpoints are already permission-gated (`ChatPermissions.*`), so Chat
can merge before the UI is polished without exposing it — assign the permissions only to a pilot role.

**Tenant safety of `channel:{id}` groups:** group names are global strings, so `JoinChannel` must reject
a channel id belonging to another tenant. This is covered implicitly if `ChannelMember` rows are
tenant-scoped (the membership check fails for a foreign channel) — confirm that holds for the
`IUserChannelLookup` pre-join in `OnConnectedAsync` too.

## Open questions / risks

- **Presence at scale:** `PresenceTracker` is per-host. Multi-replica presence needs a Redis-backed
  store; the SignalR Redis backplane handles *message* fan-out but not the presence *count*. Fine for
  single-replica; flag before HA.
- **Hub token expiry on a long-lived connection:** SignalR calls `AccessTokenProvider` only on
  (re)connect, **not per message**, while `TokenRefreshService` only fires on a REST **401**. A circuit
  that's active on the hub but idle on REST can hold a JWT past expiry with nothing to refresh it. The
  API will drop the connection when the token's lifetime ends; mitigation is to re-read the latest token
  from `ICircuitTokenCache` on each reconnect (and optionally cycle the hub connection when the REST path
  refreshes). Confirm the reconnect actually re-invokes `AccessTokenProvider` on this SignalR version.
- **Mention resolution cost:** `MentionResolver` pulls the full user list and filters in memory
  (upstream's documented trade-off). Acceptable at this scale; revisit with a targeted `IUserService`
  lookup for large tenants. Note `UserDto.Id`/`UserName` are **nullable** ([UserDto.cs](src/Modules/Identity/Modules.Identity.Contracts/DTOs/UserDto.cs))
  — null-filter before matching `@username`. And `MentionedInChannelIntegrationEvent` must supply
  **non-null** `CorrelationId` and `Source` ([IIntegrationEvent.cs:20-25](src/BuildingBlocks/Eventing.Abstractions/IIntegrationEvent.cs#L20-L25)).
- **Blazor Server circuit churn:** one server-side hub connection per circuit. Confirm disposal on
  circuit teardown and reconnect on circuit re-establish so connections don't leak.
- **`AddHeroPlatform`/`UseHeroPlatform` edits** also live in BuildingBlocks if we wire realtime there
  rather than directly in `Program.cs` — prefer `Program.cs` wiring to keep the protected surface
  minimal.
