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
| **Client UI** | React SPA (`@microsoft/signalr`, `realtime-context.tsx`) | **Blazor Server + MudBlazor + cookie auth** ([Playground.Blazor](src/Playground/Playground.Blazor/)), plus MAUI | UI is a **rewrite**, not a port. The SignalR client runs **server-side inside the Blazor circuit**. |
| **Attachments** | dedicated `Files` module + `IFileAccessPolicy` | **No Files module** — only a `Storage` building block ([IStorageService](src/BuildingBlocks/Storage/Services/IStorageService.cs)) | **Deferred to v2** (decision below). Drop `MessageAttachment` from v1. |
| **Tenant isolation** | added late via a `ChatTenantIsolation` migration | AMIS convention = `.IsMultiTenant()` **+ named** soft-delete query filter from day one ([persistence.md](.claude/rules/persistence.md)) | Bake tenant isolation into the EF config up front. |
| **Mention notifications** | separate persistent `Notifications` module + bell inbox | none today | **v1 skips persistence** — push the mention straight to the user's SignalR group (decision below). |

## Architecture (target state in AMIS102)

```
Blazor Server circuit (Playground.Blazor)            Playground.Api host
  MudBlazor chat page                                  AppHub  @ /api/v1/realtime/hub   [Authorize]
  ChatHubClient (server-side SignalR client)  ──WS──►   groups: user:{id} tenant:{id} channel:{id}
   • AccessTokenProvider = existing JWT flow                 ▲
   • C# events: ChatMessageCreated, PresenceChanged…         │ IHubContext<AppHub>
   • invokes: JoinChannel(id), Typing(id)                    │
  ApiClient (REST) ──────────────────────────────────►  Modules/Chat  (CQRS slices)
                                                           SendMessage → save → broadcast → publish mention
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
- **`IPresenceTracker` + `PresenceTracker`** — in-memory `ConcurrentDictionary<userId,count>`,
  per-host (single-replica only; documented as such).
- **`HeroRealtimeExtensions.cs`** — `AddHeroRealtime(config)` registers SignalR + the presence
  tracker, adding the StackExchange.Redis backplane when `CachingOptions:Redis` is set (channel
  prefix `amis-signalr`). `MapHeroRealtime()` maps the hub at `/api/v1/realtime/hub` + a
  `GET /api/v1/realtime/presence` snapshot endpoint.
- **Packages (CPM):** add `Microsoft.AspNetCore.SignalR.StackExchangeRedis` to
  `Directory.Packages.props`; reference it + `Microsoft.AspNetCore.SignalR` from `Web.csproj`.
  ⚠️ Watch the NU1015 CPM trap noted in memory — keep `Directory.Packages.props` well-formed.

**Host wiring** ([Playground.Api/Program.cs](src/Playground/Playground.Api/Program.cs)): call
`AddHeroRealtime` during service config and `MapHeroRealtime` after build (either inside
`AddHeroPlatform`/`UseHeroPlatform` in the Web BB, or directly in `Program.cs`).

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
  Send/EditOwn/DeleteOwn/DeleteAny messages).
- `Events/MentionedInChannelIntegrationEvent.cs` — `: IIntegrationEvent`.
- `v1/Commands/*` , `v1/Queries/*`, `v1/DTOs/*` (`ChannelDto`, `ChannelMemberDto`, `MessageDto`,
  `MessageReactionDto`, `ChannelType`, `ChannelMemberRole`). **Omit** `MessageAttachmentDto` for v1.
- `ChatContractsMarker.cs`.

**`Modules.Chat`** (refs: `Caching`, `Persistence`, `Web`, `Eventing`, `Modules.Chat.Contracts`,
`Modules.Identity.Contracts`):
- `Domain/` — `ChatChannel` (aggregate, `ISoftDeletable`; `CreateChannel`/`CreateDirect`/
  `CreateGroupDm` factories; sorted `DirectKey` `"{lo}:{hi}"` for find-or-create DM; `Archive`/
  `Restore` flip the flag, never EF `Remove()`), `ChannelMember`, `Message` (`Create`/`Edit`/
  `SoftDelete`/`Pin`/`Unpin`/`AddReaction`/`RemoveReaction`; soft delete = `DeletedAtUtc` tombstone,
  **not** `ISoftDeletable`), `MessageMention`, `MessageReaction`, `Domain/Events/*`. **Drop**
  `MessageAttachment`.
- `Data/ChatDbContext.cs` — `: BaseDbContext`, `HasDefaultSchema("chat")`, **`base.OnModelCreating`
  LAST**. `Data/Configurations/*`, `ChatDbInitializer`, `ChatDbContextFactory`.
- `Features/v1/` — vertical slices (command/handler/validator/endpoint each):
  - **Channels:** CreateChannel, FindOrCreateDm, ListMyChannels, DiscoverChannels, GetChannelById,
    UpdateChannel, ArchiveChannel, RestoreChannel, AddChannelMembers, RemoveChannelMember,
    MarkChannelRead.
  - **Messages:** SendMessage, EditMessage, DeleteMessage, PinMessage, UnpinMessage,
    ListChannelMessages, ListMessageReplies, GetPinnedMessages.
  - **Reactions:** AddReaction, RemoveReaction.
  - **Search:** SearchMessages (Postgres full-text).
  - `Features/v1/Internal/ChannelAuthorization.cs` — `RequireMember` throws **NotFound (404)** so
    non-members can't probe channel existence; `RequireAdmin` throws `ForbiddenException`.
- `Services/` — `ChannelMembershipChecker : IChannelMembershipChecker`,
  `UserChannelLookup : IUserChannelLookup` (the hub adapters), `MentionParser`,
  `IMentionResolver` + `MentionResolver` (over `IUserService.GetListAsync`).
- `ChatModule.cs` — `PermissionConstants.Register(ChatPermissions.All)`;
  `AddHeroDbContext<ChatDbContext>`; register `IDbInitializer`, validators, the two hub adapters,
  `IMentionResolver`; health check; map endpoints under `api/v{version}/chat` with
  `.RequireAuthorization()`. **Module Order after any future Notifications module.**

`SendMessageCommandHandler` flow (the spine of the feature): verify membership → validate parent
(single-level threads only) → parse `@username` → resolve to ids (drop self/unresolved) → create
`Message` → `TouchLastMessage` + bump parent reply count → save → broadcast `ChatMessageCreated` to
`channel:{id}` via `IHubContext<AppHub>` → publish one `MentionedInChannelIntegrationEvent` per
distinct mentioned user.

**Endpoint naming:** per [api-conventions.md](.claude/rules/api-conventions.md) + the
endpoint-names memory, `.WithName(...)` must be globally unique — prefix every name with the module,
e.g. `"Chat_SendMessage"`. Do **not** use `nameof(SendMessageCommand)` (the command lives in the
shared Contracts assembly).

**Host registration** ([Program.cs](src/Playground/Playground.Api/Program.cs)): add representative
Chat types to the Mediator `Assemblies` list, add `typeof(ChatModule).Assembly` to `moduleAssemblies`,
add both `.csproj` to [AMIS.Framework.slnx](src/AMIS.Framework.slnx) and as refs in
[Playground.Api.csproj](src/Playground/Playground.Api/Playground.Api.csproj).

### Phase 2 — Migrations

Generate `InitialChat` for `ChatDbContext` into `Migrations.PostgreSQL/Chat/` with its snapshot.
⚠️ Per the migrations-discovery memory: the context is discovered through its snapshot — **generate,
never hand-edit**, and don't delete the snapshot.

### Phase 3 — Mentions without a Notifications module (v1)

The upstream `Notifications` module (persistent bell inbox) is **out of scope for v1**. Instead,
`SendMessage` pushes the mention straight to the mentioned user's `user:{id}` SignalR group
(`hub.Clients.Group($"user:{id}").SendAsync("ChatMentioned", …)`). The
`MentionedInChannelIntegrationEvent` is still published so a future persistent Notifications module
can subscribe without changing Chat. Add that module later if a durable inbox is wanted.

### Phase 4 — Blazor UI (the bulk of the work)

In [Playground.Blazor](src/Playground/Playground.Blazor/):

- **`ChatHubClient`** — a circuit-scoped service wrapping `Microsoft.AspNetCore.SignalR.Client`.
  `HubConnectionBuilder().WithUrl(apiBase + "/api/v1/realtime/hub", o => o.AccessTokenProvider = …)`,
  feeding the **existing JWT** the REST `ApiClient` already uses (decision below — reuse
  [AuthorizationHeaderHandler.cs](src/Playground/Playground.Blazor/Services/Api/AuthorizationHeaderHandler.cs)
  / [TokenRefreshService.cs](src/Playground/Playground.Blazor/Services/Api/TokenRefreshService.cs)).
  Exposes C# events (`ChatMessageCreated`, `PresenceChanged`, `ChatTypingStarted`, `ChatMentioned`)
  and `JoinChannel`/`Typing` invokes. `IAsyncDisposable`, tied to the circuit lifetime; reconnect
  with backoff. `InvokeAsync(StateHasChanged)` to marshal hub callbacks onto the render thread.
- **ApiClient methods** for the chat REST endpoints (follow the existing
  [ApiClient/Generated.cs](src/Playground/Playground.Blazor/ApiClient/Generated.cs) pattern).
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

1. **Multi-tenancy is explicit.** On `ChatChannelConfiguration`: `builder.ToTable("Channels", "chat")
   .IsMultiTenant();` and — because `.IsMultiTenant()` registers a *named* query filter — the
   soft-delete filter must also be **named**: `builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);`.
   An anonymous filter alongside `.IsMultiTenant()` throws at model build (EF Core 10). See
   [persistence.md](.claude/rules/persistence.md). Decide per entity whether `Message`/`ChannelMember`
   are also `.IsMultiTenant()` or inherit isolation through the channel FK.
2. **Child-collection INSERTs.** `MessageConfiguration` sets `Property(x => x.Id).ValueGeneratedNever()`
   and the domain assigns `Guid.CreateVersion7()` in factories. Without this EF treats nav-collection
   children (mentions/reactions) as `Modified` → 0-row UPDATE instead of INSERT.
3. **`base.OnModelCreating` runs LAST** in `ChatDbContext` so tenant auto-apply sees the configured
   child types.
4. **`Context.User`, not `ICurrentUser`, inside the hub** (returns null otherwise).
5. **Endpoint `WithName` global uniqueness** — module-prefixed, never `nameof(ContractCommand)`.
6. **UTF-8 saves** for all Blazor files.

## Scope decisions (settled)

- **Attachments:** ❌ deferred to v2. v1 is text + mentions + reactions + pins. Drop the
  `MessageAttachment` entity, DTO, and file-access plumbing entirely.
- **Hub auth (Blazor):** ✅ reuse the existing JWT token flow (`AuthorizationHeaderHandler` /
  `TokenRefreshService`) as the `AccessTokenProvider`; no new auth surface.
- **Notifications:** ❌ no separate module in v1 — push mentions over SignalR (Phase 3); publish the
  integration event for a future durable inbox.

## Recommended v1 cut

DMs + group DMs + named channels + send/edit/delete + reactions + @mentions + single-level threads +
pins + search + typing + presence. No attachments, no persistent notifications, Blazor-only client.
This is the smallest slice that is genuinely "chat" and exercises the full realtime path end-to-end.

## Open questions / risks

- **Presence at scale:** `PresenceTracker` is per-host. Multi-replica presence needs a Redis-backed
  store; the SignalR Redis backplane handles *message* fan-out but not the presence *count*. Fine for
  single-replica; flag before HA.
- **Mention resolution cost:** `MentionResolver` pulls the full user list and filters in memory
  (upstream's documented trade-off). Acceptable at this scale; revisit with a targeted `IUserService`
  lookup for large tenants.
- **Blazor Server circuit churn:** one server-side hub connection per circuit. Confirm disposal on
  circuit teardown and reconnect on circuit re-establish so connections don't leak.
- **`AddHeroPlatform`/`UseHeroPlatform` edits** also live in BuildingBlocks if we wire realtime there
  rather than directly in `Program.cs` — prefer `Program.cs` wiring to keep the protected surface
  minimal.
