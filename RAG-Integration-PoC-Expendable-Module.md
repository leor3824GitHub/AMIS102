# RAG Integration PoC — Assistant Module (Expendable + AssetRegister)

> Status: **Planned — not yet implemented.** Approved design for a proof-of-concept; follow the phases in order.
> Date planned: 2026-07-08 · **Revised 2026-07-14** — the assistant is promoted out of Expendable into its own
> `Modules.Assistant`; **AssetRegister** and **MasterData** join as tool-contributing modules. Per-tool permission
> enforcement is now **mandatory**, not a follow-up.
> **Second-pass verified 2026-07-14** (Fable 5 adversarial review): every codebase claim checked against source
> (file:line), tool tables corrected to real queries/permissions, install-time package questions resolved.
>
> *(Filename kept for link stability; the scope is no longer Expendable-only.)*
> UI design: [RAG-PoC-UI-Design.md](RAG-PoC-UI-Design.md) — it replaces this plan's Phase 6.

## Context

The Expendable module's search is entirely `ILike` substring matching today, and there is zero AI/vector code anywhere in the solution. This PoC adds three capabilities:

1. **Cross-module Q&A assistant** — natural-language questions answered from live data in **Expendable** (stock levels, issuances, products) **and AssetRegister** (assets, custodians, book value), with **MasterData** resolving employee names to ids — from one conversation.
2. **Semantic product search** — embedding-based similarity search over Expendable products (find items by meaning, not just substring).
3. **Per-tool authorization** — the assistant only ever exposes the tools the *calling user* is permitted to use. A tool's gate mirrors the endpoint serving the same query, with **one intentional exception**: tools that a loop could turn into an enumeration primitive are gated at the enumeration tier (see Risks #8).

**Why AssetRegister is the second module:** it shares custodians and employees with Expendable, so *"what has been issued to Juan Dela Cruz?"* spans supply requests **and** property accountability. One assistant answering across both is something two separate assistants structurally cannot do. That cross-module answer is the point of the PoC.

**Data policy: Hybrid** — embeddings computed **locally in-process** (bulk product data never leaves the server); generation via a **pluggable LLM provider** behind the standard `Microsoft.Extensions.AI` `IChatClient` abstraction — **default: Anthropic Claude** (`claude-opus-4-8`, official `Anthropic` C# SDK adapter), switchable by configuration to OpenAI/Azure OpenAI or a fully local Ollama model. Whichever provider is configured only ever sees the question plus the retrieved tool results.

**Architecture insight:** for structured business data, the retrieval half of "RAG" is best done as a **tool-use loop over the modules' existing Mediator query handlers**. Mediator is one in-process bus, and every module's queries are public records in its `.Contracts` project — so a tool dispatches into the owning module's own handler, and **Finbuckle tenant scoping holds for free**. Embedding-based semantic search serves both as a standalone endpoint and as one of the assistant's tools.

## Key decisions

| Decision | Choice | Why |
|---|---|---|
| **Assistant placement** | **New `Modules.Assistant` (+ `.Contracts`)** — not inside Expendable | Forced by the second module: for AssetRegister to contribute tools it must implement an interface, and if that interface lived in `Modules.Expendable.Contracts` then **AssetRegister would depend on Expendable** — a bad coupling between unrelated bounded contexts. A neutral module is the only legal home. Cheap: the assistant is **stateless — no DbContext, no DbInitializer, no migration**. |
| **Tool contribution** | Modules *push* tools via `IAssistantToolProvider` (in `Modules.Assistant.Contracts`); the assistant collects `IEnumerable<IAssistantToolProvider>` from DI | Dependency arrow points **modules → Assistant.Contracts**; the assistant references no module. Adding a module's tools requires **zero edits to the assistant** — mirrors how modules already contribute permissions and endpoints. Bonus: the tool delegate lives inside the module, so it may use `internal` types. |
| **Per-tool authorization** | Each `AssistantTool` declares a `RequiredPermission`; the tool list is **filtered per caller before the model sees it**, then re-checked inside the delegate | **Mandatory, not optional.** Permissions are enforced at the *endpoint* (`.RequirePermission()`), never in handlers — so an unfiltered tool loop is a permission-bypass bus. The sharpest live example: `get_stock_levels` mirrors an endpoint gated on the **non-basic** `Inventory.ViewReports` (`GetWarehouseStockLevelsEndpoint.cs:20`) — unfiltered, the assistant would hand warehouse-wide stock to every basic user the REST API denies. (AssetRegister's `Assets.View`/`Accountability.View` split also exists, but both are seeded `IsBasic` (`AssetRegisterModule.cs:29,33`) — exercising that split needs an admin-built role.) Mechanism already exists in-repo: `ICurrentUser.GetUserId()` + `IUserService.HasPermissionAsync(...)` (Identity **Contracts**, cache-backed), used exactly this way at `RequestRepairCommandHandler.cs:117`. |
| Local embeddings | `SmartComponents.LocalEmbeddings` (bge-micro-v2 ONNX, 384-dim) behind a module-local `IEmbeddingService` | One-liner API, MIT, no external calls. Fallback if it misbehaves on .NET 10: `Microsoft.ML.OnnxRuntime` + `Microsoft.ML.Tokenizers` + all-MiniLM behind the same interface. |
| Vector storage | **pgvector** — `vector(384)` column on new `expendable.ProductEmbeddings` table via `Pgvector.EntityFrameworkCore`; similarity ranked **in the database** with `CosineDistance` | Native vector store on the Postgres we already run; scales past the in-memory ceiling and keeps the query one LINQ expression. Requires a **user-approved BuildingBlocks modification**: one `UseVector()` line inside the `UseNpgsql` options lambda in `OptionsBuilderExtensions.ConfigureHeroDatabase` (Postgres branch only — MSSQL branch untouched). Also requires a pgvector-enabled Postgres image in the AppHost. |
| Embedding lifecycle | Synchronous upsert in Create/Update/Delete product handlers (best-effort try/catch) + admin `POST /products/embeddings/rebuild` backfill endpoint; `ContentHash` column for idempotency | Local embedding is ms-scale CPU work — Hangfire adds nothing at PoC scale. Rebuild endpoint runs in caller's tenant context (correct tenancy for free). |
| **Semantic search scope** | **Expendable only.** AssetRegister contributes Q&A tools but **no embeddings** in this PoC | Keeps the pgvector blast radius to one table. `IEmbeddingService` therefore stays inside Expendable; it only moves to `Assistant.Contracts` when a second module needs to embed. **Asset semantic search is the designated next follow-up and its design is already settled — over `AssetRegistry` (not `PropertyItemCatalog`), grouped by kind. See Follow-ups.** |
| LLM provider | **Pluggable via `Microsoft.Extensions.AI` `IChatClient`** — default `Anthropic` (`claude-opus-4-8`); `OpenAI`/`AzureOpenAI` and `Ollama` (local) selectable via `Provider` config | Assistant code depends only on `IChatClient` + `AIFunction` tools; each provider is one adapter registration. Anthropic's official C# SDK ships an IChatClient adapter; Ollama gives a zero-cloud option (upgrades Hybrid → fully self-hosted when required). |
| Assistant loop | **`IChatClient` function-invocation loop** (`.UseFunctionInvocation()` middleware runs the tool round-trips), non-streamed; stateless single-turn with client-side history replay (≤10 turns) | The middleware is the provider-agnostic equivalent of a manual tool loop; `MaxToolIterations` maps to its iteration cap, and per-tool try/catch lives inside each `AIFunction` delegate (return error text, never throw). Blazor has no streaming plumbing — non-streamed PoC, SignalR streaming is the follow-up. |
| Permissions | `Permissions.Assistant.Use` (**`IsBasic: false` — opt-in**) / `Permissions.Assistant.Manage` in the **Assistant** module; `Permissions.Expendable.SearchIndex.Rebuild` in **Expendable**; semantic search reuses `Products.View` | Each module owns its own keys. Every one must be added to BOTH the `*Permissions.cs` constants **and** `RegisteredPermissions` in the owning `*Module.cs` — otherwise it 403s for everyone, admins included. **`Assistant.Use` is deliberately NOT basic**: it is the primary on/off switch (grant per role, no redeploy) for a feature that spends money and ships data externally — see [Enablement & kill-switches](#enablement--kill-switches). |

## Enablement & kill-switches

This is **two features with very different risk profiles**, and they get **two independent switches**. Bundling them would be a mistake: a plausible and legitimate deployment state is *"we'll take semantic product search — it never leaves our server — but we're not ready to send data to an external AI vendor."* That must be reachable by flipping a flag, not by deleting code.

| | Semantic search | The assistant |
|---|---|---|
| Compute | Local ONNX, in-process | External LLM API |
| Cost | Free | Per-token, uncapped |
| Data egress | **None** | Question + tool results leave the building |
| Flag | `ExpendableOptions:SemanticSearch:Enabled` (default **true**) | `AssistantOptions:Enabled` (default **false**) |

### Three layers of "off", cheapest first

**1. Permission — `Permissions.Assistant.Use`, declared `IsBasic: false`.** The primary switch, and it costs zero new code: per-role, per-tenant, runtime, no redeploy, driven from the existing Roles admin UI. Nobody has the assistant until an admin grants it; revoking turns it off. **Deliberately not `IsBasic`** — a feature that spends money per question and ships data to an external vendor must be opt-in, not opt-out. (Caveat, per existing behaviour: a newly granted role requires the user to re-login before the permission cache reflects it.)

**2. `AssistantOptions:Enabled` — the deployment kill-switch.** Permissions can't help when the API key is missing, the provider is down, or the bill spikes. When `false`:

- `AssistantModule.ConfigureServices` **does not register the keyed `IChatClient` at all** — so a missing/blank key can never crash startup, and `ChatClientFactory` (which throws on unknown providers) is never invoked. Register `IAssistantService` with a no-op implementation instead.
- The ask endpoint **still maps**, but short-circuits to `503 Service Unavailable` + ProblemDetails ("The assistant is not enabled in this environment."). A 503 with a reason is diagnosable; a 404 sends the next developer hunting for a routing bug.
- Permissions are still registered (so roles don't break when it's re-enabled) — the feature is dark, not absent.

**3. `ExpendableOptions:SemanticSearch:Enabled` — independent.** When `false`: the smart-search toggle is hidden in the UI, the Create/Update/Delete embedding hooks no-op, the rebuild endpoint 503s, and `ExpendableToolProvider` **does not contribute `semantic_product_search`** (the assistant's other tools still work — the tool list is built per request, so a disabled sub-feature simply isn't offered to the model).

### Telling the client — `GET /api/v1/assistant/status`

Blazor cannot guess. Add a tiny endpoint returning `{ "enabled": bool }`, gated on `Assistant.Use` (or `.AllowAnonymous()` for authenticated users — it leaks nothing). `AMISLayout` folds it into its **existing** `Task.WhenAll` bootstrap wave, so it costs no extra latency round-trip, and stores it in a scoped availability state. The FAB, the nav link, and the `/assistant` page all require **permission AND enabled** — a user with `Assistant.Use` in an environment where the provider is switched off sees no dead entry points.

### Irreducible footprint (be honest about this)

Three things **cannot** be flagged away and ship regardless:

1. `UseVector()` in the BuildingBlocks Npgsql branch — benign; a type-mapping plugin, no behaviour change.
2. The `pgvector/pgvector` Postgres image — a strict superset of stock Postgres.
3. The `expendable.ProductEmbeddings` table — created by migration, simply stays empty when semantic search is off.

Everything else — LLM calls, cost, egress, and every UI surface — switches off cleanly.

### Deliberately out of PoC scope

- **Per-tenant API keys / quotas.** The key is host-level; all tenants share it. Per-tenant on/off is *already* solved by layer 1 (don't grant `Assistant.Use` in that tenant), but per-tenant *billing isolation* would need tenant-scoped settings storage. Follow-up.
- **Spend caps.** `MaxToolIterations` and `MaxTokens` bound a single request; nothing bounds aggregate spend. A per-user daily question cap (cache-backed) is the natural first guard. Follow-up.

## Implementation phases

### Phase 0 — Packages, infrastructure & config

- `src/Directory.Packages.props`: add `<PackageVersion>` entries for `Microsoft.Extensions.AI` (+ `Microsoft.Extensions.AI.Abstractions`) — GA, ~10.x; `Anthropic` — **the official package, currently v12.x, which ships the Microsoft.Extensions.AI adapter. Do NOT grab the community `Anthropic.SDK` (v5.x) — a different library**; `SmartComponents.LocalEmbeddings` (still `0.1.0-preview10148`, April 2024 — the smoke test below is mandatory, not ceremony); and `Pgvector.EntityFrameworkCore` **0.3.0** (supports EF Core 9 **and 10** — verified; brings `Pgvector` transitively). Optional-provider adapters (`Microsoft.Extensions.AI.OpenAI`, `OllamaSharp`) are added **only when that provider is enabled** — not in the PoC baseline.
- Package references (version-less):
  - `Modules.Assistant.Contracts.csproj`: `Microsoft.Extensions.AI.Abstractions` (for the `AIFunction` type on `AssistantTool`)
  - `Modules.Assistant.csproj`: `Microsoft.Extensions.AI`, `Anthropic`
  - `Modules.Expendable.csproj`: `Microsoft.Extensions.AI.Abstractions`, `SmartComponents.LocalEmbeddings`, `Pgvector.EntityFrameworkCore`
  - `Modules.AssetRegister.csproj`: `Microsoft.Extensions.AI.Abstractions`
  - `Modules.MasterData.csproj`: `Microsoft.Extensions.AI.Abstractions`
- **Project references** (the new module's are easy to forget — nothing compiles without them):
  - `Modules.Assistant.Contracts.csproj`: `Mediator.Abstractions` (for `ICommand<>`) + the `Microsoft.Extensions.AI.Abstractions` package above
  - `Modules.Assistant.csproj`: `BuildingBlocks/Web` (for `IModule`, `AmisModule` attribute, `RequirePermission`), own `Modules.Assistant.Contracts`, and `Modules.Identity.Contracts` (for `IUserService.HasPermissionAsync` — the per-tool filter)
  - `Modules.Expendable.csproj` / `Modules.AssetRegister.csproj` / `Modules.MasterData.csproj`: add `Modules.Assistant.Contracts` (for `IAssistantToolProvider`)
  - `src/BuildingBlocks/Persistence/Persistence.csproj`: `Pgvector.EntityFrameworkCore` (needed for the `UseVector()` call below)
  - `src/Host/Migrations.PostgreSQL/Migrations.PostgreSQL.csproj`: `Pgvector.EntityFrameworkCore` if not already transitive (generated migrations reference the `Pgvector.Vector` CLR type)
- **AppHost Postgres image** — `src/Host/AMIS.AppHost/AppHost.cs` line ~12: the default Aspire Postgres image does not include pgvector. Change to:

  ```csharp
  var postgres = builder.AddPostgres("postgres", port: 5432)
      .WithImage("pgvector/pgvector").WithImageTag("pg17")   // match the current PG major
      .WithDataVolume("AMIS-postgres-data230")
      ...
  ```

  If the existing data volume was created by a different PG major version, use a new volume name (dev data is disposable per project convention).
- **BuildingBlocks change (user-approved)** — `src/BuildingBlocks/Persistence/OptionsBuilderExtensions.cs`, `ConfigureHeroDatabase`, PostgreSQL branch only:

  ```csharp
  case DbProviders.PostgreSQL:
      builder.UseNpgsql(connectionString, e =>
      {
          e.MigrationsAssembly(migrationsAssembly);
          e.EnableRetryOnFailure();
          e.UseVector();          // pgvector type mapping (Pgvector.EntityFrameworkCore)
      });
      break;
  ```

  MSSQL branch untouched — semantic search is Postgres-only (acceptable: all deployments run POSTGRESQL). Run the full test suite after this change since it affects every module's DbContext.
- `src/Host/AMIS.Api/appsettings.json` + `.Development.json`: new section (SendGrid ApiKey pattern):

  ```json
  "AssistantOptions": {
    "Enabled": false,                  // ← SHIPPED DEFAULT. Off unless a deployment opts in.
    "Provider": "Anthropic",           // Anthropic | OpenAI | AzureOpenAI | Ollama
    "ApiKey": "",
    "Model": "claude-opus-4-8",
    "Endpoint": null,                  // Azure OpenAI resource / Ollama URL; null for Anthropic & OpenAI
    "MaxTokens": 4096,
    "MaxToolIterations": 8
  },
  "ExpendableOptions": {
    "SemanticSearch": { "Enabled": true }   // local + free + no egress — safe to default on
  }
  ```

  > `MaxToolIterations` raised from 6 → 8: cross-module questions chain more tool calls (e.g. look up the employee, then their supply requests, then their property accountability).
  >
  > The two flags are **independent by design** — see [Enablement & kill-switches](#enablement--kill-switches) below. `Enabled: false` is the shipped default so a fresh clone, CI, or any environment without an API key stays completely inert: no `IChatClient` is constructed, so an empty key can never fail startup, and nothing ever calls `api.anthropic.com` by accident.

- **Smoke test first**: scratch console `new LocalEmbedder().Embed("test")` → 384 floats, before wiring anything.

### Phase 1 — Embedding storage (Expendable)

- `Domain/Products/ProductEmbedding.cs` — plain entity: `Id`, `TenantId` (IHasTenant), `ProductId`, `Pgvector.Vector Embedding`, `ContentHash`, `ModelId`, `GeneratedOnUtc`.
  - **No `byte[] Version`/`IsRowVersion()`** (Npgsql insert breaker), no concurrency token, **no ISoftDeletable** (hard-delete with product; avoids EF10 named-filter conflict).
- `Data/Configurations/ProductEmbeddingConfiguration.cs` — `.ToTable("ProductEmbeddings", SchemaName).IsMultiTenant()`, unique index `(TenantId, ProductId)`, and:

  ```csharp
  builder.Property(x => x.Embedding).HasColumnType("vector(384)").IsRequired();
  ```

- `Data/ExpendableDbContext.cs`: add `DbSet<ProductEmbedding>`; in `OnModelCreating` add `modelBuilder.HasPostgresExtension("vector");` so the generated migration emits `CREATE EXTENSION IF NOT EXISTS vector` **before** creating the table (extension creation needs sufficient DB privileges — the dev container superuser is fine). Belt-and-braces: also add `CREATE EXTENSION IF NOT EXISTS vector;` next to the existing pgcrypto block in `ExpendableDbInitializer.MigrateAsync` so per-tenant databases (DB-per-tenant mode) get it too.
- Generate migration `AddProductEmbeddings` into `src/Host/Migrations.PostgreSQL/Expendable/` (assembly `AMIS.Playground.Migrations.PostgreSQL`); verify the column type is `vector(384)` and the extension annotation is present. The new table has no `Version` column, so the existing pgcrypto/Version backfill loop skips it.
- **No vector index at PoC scale** (sequential scan over hundreds–thousands of rows is fine). When volume grows, add an HNSW index in a later migration: `CREATE INDEX ... USING hnsw ("Embedding" vector_cosine_ops)`.

### Phase 2 — Embedding services + semantic search slice (Expendable)

- `Services/Embeddings/IEmbeddingService.cs` + `LocalEmbeddingService.cs` — singleton wrapping `LocalEmbedder` (add lock if thread-safety unverified). **Stays inside Expendable** — it moves to `Assistant.Contracts` only when a second module needs to embed.
- `Services/Embeddings/IProductEmbeddingService.cs` + `ProductEmbeddingService.cs` (scoped):
  - `BuildEmbeddingText(Product)` → `"{Name} | {Article} | {VariantName} | {UnitOfMeasure} | {Description}"`
  - `UpsertForProductAsync` (SHA-256 hash of text; skip when unchanged)
  - `RemoveForProductAsync`
  - `ReconcileAllAsync` (upsert stale/missing, delete orphans; returns counts)
- Contracts: `v1/Products/ProductSemanticSearchContracts.cs` — `SemanticSearchProductsQuery(string Query, int TopK) : IQuery<...>` + hit DTO with `Score`, **and** `RebuildProductEmbeddingsCommand : ICommand<RebuildEmbeddingsResultDto>` (result carries upserted/skipped/removed counts — the rebuild is a command like any other, not a bare endpoint).
- `Features/v1/Products/SemanticSearchProducts/{Handler,Validator,Endpoint}.cs` — embed the query locally, then rank **in the database** with pgvector (Finbuckle tenant filters apply automatically):

  ```csharp
  var qv = new Pgvector.Vector(_embedding.Embed(query.Query));
  var hits = await _db.ProductEmbeddings.AsNoTracking()
      .Join(_db.Products.AsNoTracking().Where(p => p.Status == ProductStatus.Active),
            e => e.ProductId, p => p.Id, (e, p) => new { e, p })
      .OrderBy(x => x.e.Embedding.CosineDistance(qv))   // translated to <=> in SQL
      .Take(query.TopK)
      .Select(x => new { x.p, Distance = x.e.Embedding.CosineDistance(qv) })
      .ToListAsync(ct);
  // Score = 1 - Distance (cosine similarity)
  ```

  `GET /products/semantic-search`, `.WithName("Expendable_SemanticSearchProducts")`, `.RequirePermission(ExpendablePermissions.Products.View)`.
- `Features/v1/Products/RebuildProductEmbeddings/{Handler,Validator,Endpoint}.cs` — `POST /products/embeddings/rebuild`, `.RequirePermission(ExpendablePermissions.SearchIndex.Rebuild)`, `.WithName("Expendable_RebuildProductEmbeddings")`, returns counts. The command takes no user input, but the repo rule is *every command has a validator* — include the trivial one rather than argue the exception.
- Hook Create/Update/DeleteProduct handlers: after save, call upsert/remove in try/catch (log warning, never fail the business op). **All of this is gated on `ExpendableOptions:SemanticSearch:Enabled`** — when false the hooks no-op, and the semantic-search + rebuild endpoints short-circuit to `503` (same treatment as the assistant's kill-switch; see Enablement).

### Phase 3 — The Assistant module

New module at `src/Modules/Assistant/`. **No `Data/` folder, no DbContext, no DbInitializer, no migration** — the assistant persists nothing (the transcript is client-side; embeddings live in the modules that own the data).

#### 3a. `Modules.Assistant.Contracts`

- `AssistantContractsMarker.cs`
- `v1/Assistant/AssistantContracts.cs` — `AskAssistantCommand(string Question, IReadOnlyList<AssistantTurnDto>? History) : ICommand<AssistantAnswerDto>`; answer DTO includes `ToolsUsed`.
- `Tools/IAssistantToolProvider.cs` — **the extension point**:

  ```csharp
  namespace AMIS.Modules.Assistant.Contracts.Tools;

  /// One model-callable tool, plus the permission the caller must hold to even see it.
  public sealed record AssistantTool(
      string Name,
      string Description,
      string RequiredPermission,
      AIFunction Function);

  /// Implemented by any module that wants to expose its data to the assistant.
  /// Registered in that module's own ConfigureServices; collected here via DI.
  public interface IAssistantToolProvider
  {
      IReadOnlyList<AssistantTool> GetTools();
  }
  ```

  > This is why `Modules.Assistant.Contracts` references `Microsoft.Extensions.AI.Abstractions` — `AIFunction` is part of the contract. That is a deliberate, accepted dependency; the alternative (providers return raw delegates + hand-written JSON schema) is more code for no benefit.

#### 3b. `Modules.Assistant` (implementation)

- `Services/AssistantOptions.cs` (`AddOptions<>().BindConfiguration("AssistantOptions")`) — `Provider`, `ApiKey`, `Model`, `Endpoint`, `MaxTokens`, `MaxToolIterations`.
- `Services/ChatClientFactory.cs` — builds the provider-specific `IChatClient` from options and wraps it with the function-invocation middleware (keyed singleton so it never collides with other future AI clients):

  ```csharp
  IChatClient inner = options.Provider switch
  {
      "Anthropic"   => /* official `Anthropic` package: new AnthropicClient { ApiKey = ... }.AsIChatClient(options.Model) — adapter confirmed (v12.x), see Risks #2 */,
      "OpenAI"      => /* Microsoft.Extensions.AI.OpenAI adapter */,
      "AzureOpenAI" => /* same adapter, Endpoint + key */,
      "Ollama"      => /* OllamaSharp OllamaApiClient(Endpoint, Model) — implements IChatClient */,
      _ => throw new InvalidOperationException($"Unknown assistant provider '{options.Provider}'."),
  };
  return new ChatClientBuilder(inner)
      .UseFunctionInvocation(configure: o => o.MaximumIterationsPerRequest = options.MaxToolIterations)
      .Build();
  ```

  PoC baseline implements the `Anthropic` branch only; the others throw a descriptive "provider not installed" error until their adapter package is added (config-plus-one-registration to enable).

- `Services/IAssistantService.cs` + `ChatAssistantService.cs` (scoped). Injects the keyed `IChatClient`, `IEnumerable<IAssistantToolProvider>`, `ICurrentUser`, `IUserService`, and options. **Note:** this is the solution's *first* keyed-DI usage — the scoped consumer must request the client with `[FromKeyedServices("Assistant")]` on the constructor parameter, or DI will fail to resolve at startup.

  **Permission filtering — the load-bearing part:**

  ```csharp
  var userId = currentUser.GetUserId().ToString();
  var tools = new List<AITool>();
  foreach (var provider in _providers)
      foreach (var tool in provider.GetTools())
          if (await userService.HasPermissionAsync(userId, tool.RequiredPermission, ct))
              tools.Add(tool.Function);
  ```

  Filtering happens **before** the tool list reaches the model, so an unauthorized tool is never called *and its existence is never disclosed*. Each provider's delegate re-checks its own permission as defense in depth. The `HasPermissionAsync` loop is cheap — `UserPermissionService` is cache-backed (`ICacheService`).

- Call shape: build `List<ChatMessage>` (system prompt + replayed history + question) → `await chatClient.GetResponseAsync(messages, new ChatOptions { Tools = tools, MaxOutputTokens = options.MaxTokens }, ct)` — the `UseFunctionInvocation` middleware runs the tool round-trips. Collect `ToolsUsed` from the response's function-call contents.
- **System prompt** — domain-neutral now that two modules feed it: an AMIS assistant covering **expendable supplies and fixed assets**; **answer only from tool results, never invent quantities, statuses, custodians, or amounts**; currency ₱; say so plainly when tools return nothing; when a question spans both domains, call tools from both.
- `Features/v1/Assistant/AskAssistant/{Handler,Validator,Endpoint}.cs` — thin handler delegating to `IAssistantService`; validator (question ≤2000 chars, history ≤10 turns); `POST /api/v1/assistant/ask`, `.WithName("Assistant_AskAssistant")`, `.RequirePermission(AssistantPermissions.Assistant.Use)`.
- `AssistantModule.cs` / `AssistantModuleConstants.cs` / `AssemblyInfo.cs` with `[assembly: AmisModule(...)]` — **without the attribute the module is dormant with a green build and passing tests.**

### Phase 4 — Tool providers (one per module)

Each provider lives **inside its own module**, dispatches **its own** queries through `IMediator`, serializes results to compact JSON, and wraps every delegate body in try/catch returning error text (never throws).

> **Rule: a tool's `RequiredPermission` mirrors the permission of the endpoint that serves the same query** — verified per tool below (second pass caught two violations of this in the original draft). Where a tool is deliberately *stricter* than its endpoint, that is called out explicitly.

**`Modules.Expendable/Services/Assistant/ExpendableToolProvider.cs`**

| Tool | Dispatches | `RequiredPermission` (endpoint gate) |
|---|---|---|
| `semantic_product_search(query, top_k)` | `SemanticSearchProductsQuery` (new, Phase 2) — **contributed only when `SemanticSearch:Enabled`**; the tool list is built per request, so a disabled sub-feature is simply never offered to the model | `Expendable.Products.View` |
| `search_products(keyword, status)` | `SearchProductsQuery` (existing) | `Expendable.Products.View` |
| `get_stock_levels(keyword?)` | `GetWarehouseStockLevelsQuery` (existing) | **`Expendable.Inventory.ViewReports`** — the endpoint's actual gate (`GetWarehouseStockLevelsEndpoint.cs:20`). ⚠️ NOT `Inventory.View`, which is `IsBasic: true` (`ExpendableModule.cs:101`) and would leak warehouse-wide stock to every user. |
| `search_supply_requests(status?, …)` | `SearchSupplyRequestsQuery` (existing; filters by `EmployeeId` **string** — needs `find_employee` first for name-based questions) | `Expendable.SupplyRequests.View` |
| `get_employee_issuances(employee_id)` | `GetEmployeeIssuanceHistoryQuery` (existing, `SupplyRequestContracts.cs:150`) — "what has been *issued*" is issuance history, not request status | `Expendable.Inventory.ViewReports` (`GetEmployeeIssuanceHistoryEndpoint.cs:21`) |

**`Modules.AssetRegister/Services/Assistant/AssetRegisterToolProvider.cs`**

| Tool | Dispatches | `RequiredPermission` (endpoint gate) |
|---|---|---|
| `search_assets(keyword, lifecycle_state?, property_class?)` | `SearchAssetsQuery` (existing, `AssetContracts.cs:161` — there is no `SearchAssetRegistryQuery`; no `status` param, the closest is `LifecycleState`) | `AssetRegister.Assets.View` |
| `get_asset_by_property_no(property_no)` | `GetAssetByPropertyNoQuery` (existing) | `AssetRegister.Assets.View` |
| `get_asset_accountability(property_no)` | **`GetAssetScanDetailByPropertyNoQuery`** (existing, `AssetContracts.cs:141`) — the *only* query answering "who holds X" from a PropertyNo: its `AssetScanDetailDto` denormalizes `AccountableOfficerName`, `DocumentNo`, `LocationName`. (`GetAccountabilityQuery` takes a document **GUID** the model can never obtain; `SearchAccountabilitiesQuery.Keyword` matches DocumentNo only.) | **`AssetRegister.Accountability.View` — deliberately STRICTER than its endpoint** (which requires only `Assets.View`, correctly — see Risks #8). **Why:** a tool loop is an enumeration engine. `search_assets` (`Assets.View`) yields hundreds of PropertyNos and the model can call this tool on every one, collapsing REST's self-limiting point lookup into bulk custodian disclosure. So the tool is gated at the *enumeration* tier. This is the one intentional deviation from the mirror-the-endpoint rule. |
| `search_accountabilities(employee_id?, status?)` | `SearchAccountabilitiesQuery` (existing; by-employee filter is `ReceivedByEmployeeId` **Guid** — needs `find_employee` first) | `AssetRegister.Accountability.View` (`SearchAccountabilitiesEndpoint.cs:20`) |
| `get_ppe_ledger_card(property_no)` | `GetPpeLedgerCardQuery` (existing) — book value, depreciation | `AssetRegister.Assets.View` |

**`Modules.MasterData/Services/Assistant/MasterDataToolProvider.cs`** *(third provider — exists because no other tool can turn "Juan Dela Cruz" into an EmployeeId, and without that the flagship cross-module question is unanswerable)*

| Tool | Dispatches | `RequiredPermission` (endpoint gate) |
|---|---|---|
| `find_employee(name)` | `SearchEmployeeReferencesQuery` (existing, `EmployeeReferenceContracts.cs:95` — `Keyword` matches names) | `MasterData.Lookup.View` (`MasterDataLookupEndpoint.cs:21`) |

> Normalize `property_no` with `.Trim().ToUpperInvariant()` before lookup — same rule the MAUI client already follows.
>
> The flagship question now has a real chain: `find_employee` → `get_employee_issuances` + `search_accountabilities` — three modules, one answer.

All three providers are registered in their own module's `ConfigureServices`:

```csharp
services.AddScoped<IAssistantToolProvider, ExpendableToolProvider>();    // in ExpendableModule
services.AddScoped<IAssistantToolProvider, AssetRegisterToolProvider>(); // in AssetRegisterModule
services.AddScoped<IAssistantToolProvider, MasterDataToolProvider>();    // in MasterDataModule
```

No module references `Modules.Assistant` — only `Modules.Assistant.Contracts`. **The assistant references none of them.**

### Phase 5 — Module wiring & permissions

- **`AssistantPermissions.cs`** (Assistant.Contracts): `Assistant.Use`, `Assistant.Manage` — format `Permissions.Assistant.Use`.
- **`AssistantModule.cs`**: `RegisteredPermissions` += `new("Use AMIS Assistant", "Use", "Assistant")` — **no `IsBasic`**, this is the opt-in switch — and `new("Manage AMIS Assistant", "Manage", "Assistant")`. `ConfigureServices`: bind options **always** (so `/status` can answer), then branch on `Enabled`:

  ```csharp
  if (options.Enabled)
  {
      services.AddKeyedSingleton<IChatClient>("Assistant", (sp, _) => ChatClientFactory.Create(options));
      services.AddScoped<IAssistantService, ChatAssistantService>();
  }
  else
  {
      services.AddScoped<IAssistantService, DisabledAssistantService>();  // throws AssistantDisabledException → 503
  }
  ```

  Permissions register either way, so roles survive a toggle. `MapEndpoints`: `MapGroup("api/v{version:apiVersion}/assistant")` + AskAssistant + **GetAssistantStatus** (`.WithName("Assistant_GetStatus")`, returns `{ enabled }`).
- **`ExpendablePermissions.cs`**: add nested `SearchIndex` class (`Rebuild`) and register it in `ExpendableModule.RegisteredPermissions`. Register `ExpendableToolProvider`.
- **`AssetRegisterModule.cs`**: register `AssetRegisterToolProvider`. No new permissions — the tools reuse the module's existing keys.
- **`MasterDataModule.cs`**: register `MasterDataToolProvider`. No new permissions — `find_employee` reuses `Lookup.View`. MasterData is already in `moduleAssemblies` and the Mediator assembly list — no `Program.cs` change for it.
- **Host** (`AMIS.Api/Program.cs`): Mediator `o.Assemblies` += `typeof(AssistantModule)`, `typeof(AssistantContractsMarker)`, `typeof(AskAssistantCommand)`; `moduleAssemblies` += `typeof(AssistantModule).Assembly`.
- **`src/AMIS.Framework.slnx`** += both Assistant projects; **`AMIS.Api.csproj`** += both project references.

### Phase 6 — Blazor client + UI

See **[RAG-PoC-UI-Design.md](RAG-PoC-UI-Design.md)** — the full UI design (assistant panel + global launcher + Products page semantic-search/rebuild hooks + markdown rendering).

## Files

**New (Assistant module)** — `src/Modules/Assistant/`
- `Modules.Assistant.Contracts/` — `AssistantContractsMarker.cs`, `Permissions/AssistantPermissions.cs`, `v1/Assistant/AssistantContracts.cs`, `Tools/IAssistantToolProvider.cs`
- `Modules.Assistant/` — `AssistantModule.cs`, `AssistantModuleConstants.cs`, `AssemblyInfo.cs`, `Services/{AssistantOptions,ChatClientFactory,IAssistantService,ChatAssistantService,DisabledAssistantService}.cs`, `Features/v1/Assistant/AskAssistant/*`, `Features/v1/Assistant/GetAssistantStatus/*`

**New (Expendable)**
- `Domain/Products/ProductEmbedding.cs`; `Data/Configurations/ProductEmbeddingConfiguration.cs`
- `Services/Embeddings/{IEmbeddingService,LocalEmbeddingService,IProductEmbeddingService,ProductEmbeddingService}.cs`
- `Services/Assistant/ExpendableToolProvider.cs`
- `Contracts/v1/Products/ProductSemanticSearchContracts.cs`
- `Features/v1/Products/SemanticSearchProducts/*`, `Features/v1/Products/RebuildProductEmbeddings/*`
- Migration in `src/Host/Migrations.PostgreSQL/Expendable/`

**New (AssetRegister)**
- `Services/Assistant/AssetRegisterToolProvider.cs` — *this is the entire cost of adding the module*

**New (MasterData)**
- `Services/Assistant/MasterDataToolProvider.cs` — *one file again; the third module proves the pattern's price*

**Modified**
- `src/Directory.Packages.props`; `Modules.Expendable.csproj`; `Modules.AssetRegister.csproj`; `Modules.MasterData.csproj`
- `src/BuildingBlocks/Persistence/OptionsBuilderExtensions.cs` + `Persistence.csproj` (**user-approved BuildingBlocks change**: `UseVector()` in the Npgsql branch)
- `src/Host/AMIS.AppHost/AppHost.cs` (pgvector-enabled Postgres image)
- `ExpendablePermissions.cs`; `ExpendableModule.cs`; `ExpendableDbContext.cs`; `ExpendableDbInitializer.cs`; `AssetRegisterModule.cs`; `MasterDataModule.cs`
- `Features/v1/Products/CreateProduct|UpdateProduct|DeleteProduct` handlers (embedding hooks)
- `src/Host/AMIS.Api/Program.cs`; `AMIS.Api.csproj`; `src/AMIS.Framework.slnx`; `appsettings.json` + `.Development.json`
- Blazor — see the UI design doc

## Risks / verify-before-coding

1. `SmartComponents.LocalEmbeddings` — **still experimental** (latest is `0.1.0-preview10148`, April 2024; net8.0-era dependencies; verified 2026-07-14). A net8.0 library generally loads on .NET 10, but the smoke test is **mandatory, not ceremony**; the ONNX-runtime fallback path stays live. Verify thread-safety + L2 normalization.
2. ~~`Anthropic` SDK adapter unknown~~ → **Resolved (2026-07-14).** The official `Anthropic` package (currently v12.x) ships the Microsoft.Extensions.AI adapter: `client.AsIChatClient(options.Model)`, chainable with `.AsBuilder().UseFunctionInvocation()`. `Microsoft.Extensions.AI` is GA and the iteration cap is exactly `FunctionInvokingChatClient.MaximumIterationsPerRequest` — the Phase 3b snippet is correct as written. The ~80-line fallback shim is **not expected to be needed**. Remaining hazard: the community `Anthropic.SDK` (v5.x) is a *different* package — don't install it by mistake.
3. ~~`Pgvector.EntityFrameworkCore` vs EF Core 10 unknown~~ → **Resolved (2026-07-14).** Version **0.3.0** supports EF Core 9 **and 10** (repo is on 10.0.7). Still run the `CosineDistance`-translation smoke check at install; the `float[]` + in-memory-cosine fallback stays documented but is unlikely to be needed.
4. **BuildingBlocks blast radius** — `UseVector()` in `ConfigureHeroDatabase` touches every module's DbContext options. It's additive (type-mapping plugin only), but run the full solution build + test suite after the change.
5. **Postgres image swap** — `pgvector/pgvector:pgXX` must match the data volume's PG major version; otherwise start a fresh volume (dev data disposable). Non-Aspire environments need the pgvector extension installed on the server.
6. Extension creation (`CREATE EXTENSION vector`) requires elevated DB privileges — fine in dev containers; production DBAs may need to pre-create it.
7. ~~Tool-level authz is bypassed~~ → **Resolved by design.** Per-tool `RequiredPermission` + pre-model filtering + in-delegate re-check (Phase 3b). **This is now a correctness requirement, not a hardening step** — regression here silently leaks another module's data.
8. **Custodian data: point lookup vs. enumeration (policy, not a defect — settled 2026-07-14).** `GET /assets/by-property-no/{x}/scan-detail` returns the accountable officer while gated on only `Assets.View` (`GetAssetScanDetailByPropertyNoEndpoint.cs:19`). This is **correct and stays as-is**: its sole consumer is the MAUI scan screen, where "who holds this?" is the question the screen exists to answer, and over REST the lookup is self-limiting — you must already know a PropertyNo, which in practice means holding the asset. Bulk custodian queries (`SearchAccountabilities`) are separately gated on `Accountability.View`. The resulting policy:

   | Operation | Gate |
   |---|---|
   | Point lookup — one asset, by PropertyNo | `Assets.View` |
   | Enumeration — who holds what, in bulk | `Accountability.View` |

   **The assistant sits at the enumeration tier, and that is why `get_asset_accountability` requires `Accountability.View` despite its endpoint requiring less.** A tool loop is an enumeration engine: `search_assets` (`Assets.View`) returns hundreds of PropertyNos, and the model can call the custodian tool on each — collapsing a self-limiting point lookup into bulk disclosure. The stricter gate closes a path that exists *only because of the assistant*. Do not "harmonize" it down to `Assets.View` on the grounds that the endpoint does; that reasoning inverts the threat model.
9. **New-module wiring is easy to half-do.** A missing `[assembly: AmisModule(...)]` leaves the Assistant module dormant with a **green build and passing tests** — the endpoint simply 404s. Check the attribute first when `/api/v1/assistant/ask` doesn't exist.
10. **Endpoint name collisions** — `.WithName()` must be globally unique or *every* request 500s. Use `Assistant_AskAssistant`, `Expendable_SemanticSearchProducts`, `Expendable_RebuildProductEmbeddings` (no collisions with existing names — verified). Note: the module prefix deviates from Expendable's own `nameof(Query)` habit — deliberate, per `.claude/rules/api-conventions.md` (prefix guarantees global uniqueness). If the architecture tests' pinned module arrays are ever extended, the new endpoint class names (`AskAssistantEndpoint`, etc.) fall outside the verb whitelist in `EndpointConventionTests.cs:236-259` — extend the whitelist then, not now.
11. API host must reach the configured provider's endpoint (`api.anthropic.com` by default); real keys in `.Development.json`/user-secrets only.
12. **Provider capability variance** — tool-calling quality differs by provider/model. `claude-opus-4-8` is the tuned default; small local Ollama models may call tools unreliably. Treat non-default providers as best-effort.
13. bge-micro is English-centric — Filipino term quality weaker; ILike `search_products` remains the exact-match fallback.
14. Semantic search is Postgres-only (`UseVector()` lives in the Npgsql branch); the MSSQL provider path would need the feature disabled — acceptable since all deployments run POSTGRESQL.

## Verification checklist

1. `dotnet build src/AMIS.Framework.slnx` — 0 warnings; `dotnet test` green (BuildingBlocks change affects all modules).
2. Scratch embed smoke test (384 floats) before wiring.
3. Run AppHost → pgvector image starts; `SELECT * FROM pg_extension WHERE extname = 'vector'` returns a row; migration applies; `expendable."ProductEmbeddings"` exists with `Embedding vector(384)`; other modules' contexts still initialize (proves `UseVector()` is benign).
4. `POST .../products/embeddings/rebuild` → counts = seeded products; re-run → all skipped (hash idempotent).
5. `GET .../products/semantic-search?query=paper for printing` → "Bond Paper A4" ranks first; SQL log shows the `<=>` cosine-distance operator (DB-side ranking, not client-side).
6. Create/update/delete a product → embedding row appears/changes/disappears without rebuild.
7. `POST /api/v1/assistant/ask` "How many ballpens in stock?" → quantities match `GetWarehouseStockLevels`; nonexistent-data question → "found nothing", no hallucinated numbers.
8. **AssetRegister reachable** — "Who is accountable for property no X?" returns the real custodian (via `get_asset_accountability` → scan-detail); "What's the book value of X?" matches the PPE ledger card.
9. **Cross-module answer (the whole point)** — "What has been issued to Juan Dela Cruz?" chains `find_employee` (MasterData) → `get_employee_issuances` (Expendable) + `search_accountabilities` (AssetRegister): one coherent answer, tool chips from **three** modules.
10. **Per-tool permission filtering (the security gate — do not ship without it passing).** *Primary test:* a user with `Products.View` but **not** `Inventory.ViewReports` asks "how many ballpens in stock?" → `get_stock_levels` must be absent from the outbound request's tool list (check payload/logs) and the assistant says it cannot answer; grant `Inventory.ViewReports`, re-ask → it answers. *Secondary (custodian, stricter-than-REST):* needs an admin-built role with `Assets.View` but not `Accountability.View` — both are seeded `IsBasic`, so no default user has the split — then "who holds property no X?" must be declined while "does property no X exist?" still works.
11. 403 without `Assistant.Use`; admin works (proves `RegisteredPermissions` landed for the new module). Note `Assistant.Use` is **not** `IsBasic`, so a fresh non-admin user has no access until a role grants it — that is the intended default.
11b. **Kill-switches.** (a) `AssistantOptions:Enabled = false` **with a blank ApiKey** → app **starts cleanly** (proves no `IChatClient` is constructed), `/assistant/status` returns `{enabled:false}`, `POST /assistant/ask` returns **503 with a readable reason** (not 404), and Blazor shows **no FAB and no nav link** even for a user holding `Assistant.Use`. (b) Re-enable → all three come back with no role changes (permissions survived the toggle). (c) `SemanticSearch:Enabled = false` → smart-search toggle hidden, product create/update does **not** write embedding rows, rebuild endpoint 503s, and the assistant still answers stock/asset questions but **never calls `semantic_product_search`** (check the outbound tool list).
12. Two-tenant check: tenant A never sees tenant B products, assets, or custodians in semantic search or assistant answers.
13. Provider abstraction: set `Provider` to an uninstalled value → fails with the descriptive "provider not installed" error; optionally point `Provider: "Ollama"` at a local model and confirm the same assistant slice answers without code changes.

## Follow-ups (out of PoC scope)

- **Semantic asset search — over `AssetRegistry`, not `PropertyItemCatalog`** *(decided 2026-07-14; next follow-up in line)*. Design is settled, only the build is deferred:

  - **Table:** `assetregister.AssetEmbeddings`, mirroring `ProductEmbeddings` (`vector(384)`, `.IsMultiTenant()`, unique `(TenantId, AssetRegistryId)`, `ContentHash`, no `Version`, no `ISoftDeletable`).
  - **Embedding text — with two deliberate exclusions:**

    ```
    {Description} | {Brand} | {Model} | {PropertyClass} | {CategoryCode} | {AssetType} | {Category}
    ```

    **Exclude `PropertyNo` and `SerialNo`** — identifiers, not language. Embedding `SN-4477-XZ` produces noise that dilutes the real signal, and exact lookup is already covered properly by `GetAssetByPropertyNoQuery` + the `ILike` `search_assets` tool.

    **Exclude `CurrentCondition`, `CurrentCustodianId`, `CarryingAmount`.** These change on every accountability transfer and every depreciation run. Excluding them makes the embedding text **write-once at registration**, so transfers and the monthly depreciation job never invalidate a vector — combined with the `ContentHash` check, re-embedding effectively never fires after creation. This is the difference between a stable index and one that churns constantly.

  - **Results must be grouped by kind.** Assets are *instances*: 500 identical Dell Latitude 5440s produce 500 near-identical vectors, so a naive `top_k=10` returns the same laptop ten times and the other kinds never surface. **Over-fetch ~100 by cosine distance in the DB, then collapse by `Description` (or source catalog item)** and return the top-K *distinct kinds* with a match score, an instance count, and a representative `PropertyNo`:

    > **Laptop, Dell Latitude 5440** — 87% · 47 units · e.g. `2024-05-0113`
    > **Laptop, Lenovo ThinkPad E14** — 81% · 12 units · e.g. `2023-11-0044`

    The model can then call `search_assets` to enumerate actual instances if the user asks for them.

  - **This is the trigger to move `IEmbeddingService`** out of Expendable into `Assistant.Contracts`, so both modules share the one ONNX embedder: **shared compute, module-owned storage.** Do **not** build a central embeddings table — it would cross-write module data.
  - **Two costs that don't apply to products.** (1) *Cardinality* — the register is one row per physical thing the agency owns, so the "no vector index at PoC scale" assumption may not hold; past ~10k assets the HNSW index moves from follow-up into the migration. (2) *Bulk registration* — assets are created in batches from an IAR/PO, so a synchronous per-asset embed adds ~5 ms × N to that request (~1 s for a 200-line IAR). Add a batch upsert path, or accept the hit knowingly.
- Token streaming of assistant answers over the existing SignalR `AppHub` (the `NotificationWriter` → `user:{id}` pattern). More modules = more tool round-trips = longer waits; this stops being optional as the tool set grows.
- HNSW index on `ProductEmbeddings.Embedding` (`vector_cosine_ops`) once product counts justify it.
- Enable the non-default LLM providers: add the adapter package + fill in the corresponding `ChatClientFactory` branch — config-only after that.
- **Module 3+** — Vehicle, BudgetDisbursement, ProcurementAcquisition. Each costs exactly one `*ToolProvider.cs` and a DI line. Watch tool-count bloat: ~50 tool schemas degrades tool-selection accuracy and inflates every request. Cap each module at 3–5 high-value tools; if it still bloats, route in two stages (pick domain → load that domain's tools).
- NSwag client regeneration to replace the hand-written Blazor clients.
