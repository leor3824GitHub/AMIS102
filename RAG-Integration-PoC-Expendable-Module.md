# RAG Integration PoC — Assistant Module (Expendable + AssetRegister)

> Status: **Planned — not yet implemented.** Approved design for a proof-of-concept; follow the phases in order.
> Date planned: 2026-07-08 · **Revised 2026-07-14** — the assistant is promoted out of Expendable into its own
> `Modules.Assistant`, and **AssetRegister** joins as a second tool-contributing module. Per-tool permission
> enforcement is now **mandatory**, not a follow-up.
>
> *(Filename kept for link stability; the scope is no longer Expendable-only.)*
> UI design: [RAG-PoC-UI-Design.md](RAG-PoC-UI-Design.md) — it replaces this plan's Phase 5.

## Context

The Expendable module's search is entirely `ILike` substring matching today, and there is zero AI/vector code anywhere in the solution. This PoC adds three capabilities:

1. **Cross-module Q&A assistant** — natural-language questions answered from live data in **Expendable** (stock levels, supply requests, products) **and AssetRegister** (assets, custodians, book value), from one conversation.
2. **Semantic product search** — embedding-based similarity search over Expendable products (find items by meaning, not just substring).
3. **Per-tool authorization** — the assistant only ever exposes the tools the *calling user* is permitted to use.

**Why AssetRegister is the second module:** it shares custodians and employees with Expendable, so *"what has been issued to Juan Dela Cruz?"* spans supply requests **and** property accountability. One assistant answering across both is something two separate assistants structurally cannot do. That cross-module answer is the point of the PoC.

**Data policy: Hybrid** — embeddings computed **locally in-process** (bulk product data never leaves the server); generation via a **pluggable LLM provider** behind the standard `Microsoft.Extensions.AI` `IChatClient` abstraction — **default: Anthropic Claude** (`claude-opus-4-8`, official `Anthropic` C# SDK adapter), switchable by configuration to OpenAI/Azure OpenAI or a fully local Ollama model. Whichever provider is configured only ever sees the question plus the retrieved tool results.

**Architecture insight:** for structured business data, the retrieval half of "RAG" is best done as a **tool-use loop over the modules' existing Mediator query handlers**. Mediator is one in-process bus, and every module's queries are public records in its `.Contracts` project — so a tool dispatches into the owning module's own handler, and **Finbuckle tenant scoping holds for free**. Embedding-based semantic search serves both as a standalone endpoint and as one of the assistant's tools.

## Key decisions

| Decision | Choice | Why |
|---|---|---|
| **Assistant placement** | **New `Modules.Assistant` (+ `.Contracts`)** — not inside Expendable | Forced by the second module: for AssetRegister to contribute tools it must implement an interface, and if that interface lived in `Modules.Expendable.Contracts` then **AssetRegister would depend on Expendable** — a bad coupling between unrelated bounded contexts. A neutral module is the only legal home. Cheap: the assistant is **stateless — no DbContext, no DbInitializer, no migration**. |
| **Tool contribution** | Modules *push* tools via `IAssistantToolProvider` (in `Modules.Assistant.Contracts`); the assistant collects `IEnumerable<IAssistantToolProvider>` from DI | Dependency arrow points **modules → Assistant.Contracts**; the assistant references no module. Adding a module's tools requires **zero edits to the assistant** — mirrors how modules already contribute permissions and endpoints. Bonus: the tool delegate lives inside the module, so it may use `internal` types. |
| **Per-tool authorization** | Each `AssistantTool` declares a `RequiredPermission`; the tool list is **filtered per caller before the model sees it**, then re-checked inside the delegate | **Mandatory, not optional.** Permissions are enforced at the *endpoint* (`.RequirePermission()`), never in handlers — so an unfiltered tool loop is a permission-bypass bus. AssetRegister grants `Assets.View` and `Accountability.View` **separately**, so "can see the asset register but not custodians" is a real user today. Mechanism already exists in-repo: `ICurrentUser.GetUserId()` + `IUserService.HasPermissionAsync(...)` (Identity **Contracts**), used exactly this way at `RequestRepairCommandHandler.cs:117`. |
| Local embeddings | `SmartComponents.LocalEmbeddings` (bge-micro-v2 ONNX, 384-dim) behind a module-local `IEmbeddingService` | One-liner API, MIT, no external calls. Fallback if it misbehaves on .NET 10: `Microsoft.ML.OnnxRuntime` + `Microsoft.ML.Tokenizers` + all-MiniLM behind the same interface. |
| Vector storage | **pgvector** — `vector(384)` column on new `expendable.ProductEmbeddings` table via `Pgvector.EntityFrameworkCore`; similarity ranked **in the database** with `CosineDistance` | Native vector store on the Postgres we already run; scales past the in-memory ceiling and keeps the query one LINQ expression. Requires a **user-approved BuildingBlocks modification**: one `UseVector()` line inside the `UseNpgsql` options lambda in `OptionsBuilderExtensions.ConfigureHeroDatabase` (Postgres branch only — MSSQL branch untouched). Also requires a pgvector-enabled Postgres image in the AppHost. |
| Embedding lifecycle | Synchronous upsert in Create/Update/Delete product handlers (best-effort try/catch) + admin `POST /products/embeddings/rebuild` backfill endpoint; `ContentHash` column for idempotency | Local embedding is ms-scale CPU work — Hangfire adds nothing at PoC scale. Rebuild endpoint runs in caller's tenant context (correct tenancy for free). |
| **Semantic search scope** | **Expendable only.** AssetRegister contributes Q&A tools but **no embeddings** in this PoC | Keeps the pgvector blast radius to one table. `IEmbeddingService` therefore stays inside Expendable; it only moves to `Assistant.Contracts` when a second module needs to embed. **Asset semantic search is the designated next follow-up and its design is already settled — over `AssetRegistry` (not `PropertyItemCatalog`), grouped by kind. See Follow-ups.** |
| LLM provider | **Pluggable via `Microsoft.Extensions.AI` `IChatClient`** — default `Anthropic` (`claude-opus-4-8`); `OpenAI`/`AzureOpenAI` and `Ollama` (local) selectable via `Provider` config | Assistant code depends only on `IChatClient` + `AIFunction` tools; each provider is one adapter registration. Anthropic's official C# SDK ships an IChatClient adapter; Ollama gives a zero-cloud option (upgrades Hybrid → fully self-hosted when required). |
| Assistant loop | **`IChatClient` function-invocation loop** (`.UseFunctionInvocation()` middleware runs the tool round-trips), non-streamed; stateless single-turn with client-side history replay (≤10 turns) | The middleware is the provider-agnostic equivalent of a manual tool loop; `MaxToolIterations` maps to its iteration cap, and per-tool try/catch lives inside each `AIFunction` delegate (return error text, never throw). Blazor has no streaming plumbing — non-streamed PoC, SignalR streaming is the follow-up. |
| Permissions | `Permissions.Assistant.Use` (IsBasic) / `Permissions.Assistant.Manage` in the **Assistant** module; `Permissions.Expendable.SearchIndex.Rebuild` in **Expendable**; semantic search reuses `Products.View` | Each module owns its own keys. Every one must be added to BOTH the `*Permissions.cs` constants **and** `RegisteredPermissions` in the owning `*Module.cs` — otherwise it 403s for everyone, admins included. |

## Implementation phases

### Phase 0 — Packages, infrastructure & config

- `src/Directory.Packages.props`: add `<PackageVersion>` entries for `Microsoft.Extensions.AI` (+ `Microsoft.Extensions.AI.Abstractions`), `Anthropic` (latest stable — verify on nuget.org; default provider adapter), `SmartComponents.LocalEmbeddings` (latest preview), and `Pgvector.EntityFrameworkCore` (latest — **verify EF Core 10 compatibility**; brings `Pgvector` transitively). Optional-provider adapters (`Microsoft.Extensions.AI.OpenAI`, `OllamaSharp`) are added **only when that provider is enabled** — not in the PoC baseline.
- Package references (version-less):
  - `Modules.Assistant.Contracts.csproj`: `Microsoft.Extensions.AI.Abstractions` (for the `AIFunction` type on `AssistantTool`)
  - `Modules.Assistant.csproj`: `Microsoft.Extensions.AI`, `Anthropic`
  - `Modules.Expendable.csproj`: `Microsoft.Extensions.AI.Abstractions`, `SmartComponents.LocalEmbeddings`, `Pgvector.EntityFrameworkCore`
  - `Modules.AssetRegister.csproj`: `Microsoft.Extensions.AI.Abstractions`
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
    "Provider": "Anthropic",          // Anthropic | OpenAI | AzureOpenAI | Ollama
    "ApiKey": "",
    "Model": "claude-opus-4-8",
    "Endpoint": null,                  // Azure OpenAI resource / Ollama URL; null for Anthropic & OpenAI
    "MaxTokens": 4096,
    "MaxToolIterations": 8
  }
  ```

  > `MaxToolIterations` raised from 6 → 8: cross-module questions chain more tool calls (e.g. look up the employee, then their supply requests, then their property accountability).

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
- Contracts: `v1/Products/ProductSemanticSearchContracts.cs` — `SemanticSearchProductsQuery(string Query, int TopK) : IQuery<...>` + hit DTO with `Score`.
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
- `Features/v1/Products/RebuildProductEmbeddings/{Handler,Endpoint}.cs` — `POST /products/embeddings/rebuild`, `.RequirePermission(ExpendablePermissions.SearchIndex.Rebuild)`, `.WithName("Expendable_RebuildProductEmbeddings")`, returns counts.
- Hook Create/Update/DeleteProduct handlers: after save, call upsert/remove in try/catch (log warning, never fail the business op).

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
      "Anthropic"   => /* Anthropic SDK IChatClient adapter, e.g. new AnthropicClient { ApiKey = ... }.AsIChatClient(options.Model) — verify exact adapter API at install time */,
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

- `Services/IAssistantService.cs` + `ChatAssistantService.cs` (scoped). Injects the keyed `IChatClient`, `IEnumerable<IAssistantToolProvider>`, `ICurrentUser`, `IUserService`, and options.

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

**`Modules.Expendable/Services/Assistant/ExpendableToolProvider.cs`**

| Tool | Dispatches | `RequiredPermission` |
|---|---|---|
| `semantic_product_search(query, top_k)` | `SemanticSearchProductsQuery` (new, Phase 2) | `Expendable.Products.View` |
| `search_products(keyword, status)` | `SearchProductsQuery` (existing) | `Expendable.Products.View` |
| `get_stock_levels(keyword?)` | `GetWarehouseStockLevelsQuery` (existing) | `Expendable.Inventory.View` |
| `search_supply_requests(status?, …)` | `SearchSupplyRequestsQuery` (existing) | `Expendable.SupplyRequests.View` |

**`Modules.AssetRegister/Services/Assistant/AssetRegisterToolProvider.cs`**

| Tool | Dispatches | `RequiredPermission` |
|---|---|---|
| `search_assets(keyword, status?, property_class?)` | `SearchAssetRegistryQuery` (existing) | `AssetRegister.Assets.View` |
| `get_asset_by_property_no(property_no)` | `GetAssetByPropertyNoQuery` (existing) | `AssetRegister.Assets.View` |
| `get_asset_accountability(…)` | `GetAccountabilityQuery` / accountability search — who currently holds what | **`AssetRegister.Accountability.View`** |
| `get_ppe_ledger_card(property_no)` | `GetPpeLedgerCardQuery` (existing) — book value, depreciation | `AssetRegister.Assets.View` |

> Normalize `property_no` with `.Trim().ToUpperInvariant()` before lookup — same rule the MAUI client already follows.
>
> The third row is the one that proves the design: `Assets.View` and `Accountability.View` are **granted separately today**, so a user with the former and not the latter must not learn custodians through the assistant.

Both providers are registered in their own module's `ConfigureServices`:

```csharp
services.AddScoped<IAssistantToolProvider, ExpendableToolProvider>();   // in ExpendableModule
services.AddScoped<IAssistantToolProvider, AssetRegisterToolProvider>(); // in AssetRegisterModule
```

Neither module references `Modules.Assistant` — only `Modules.Assistant.Contracts`. **The assistant references neither module.**

### Phase 5 — Module wiring & permissions

- **`AssistantPermissions.cs`** (Assistant.Contracts): `Assistant.Use`, `Assistant.Manage` — format `Permissions.Assistant.Use`.
- **`AssistantModule.cs`**: `RegisteredPermissions` += `new("Use AMIS Assistant", "Use", "Assistant", IsBasic: true)` and `new("Manage AMIS Assistant", "Manage", "Assistant")`. `ConfigureServices`: options binding; keyed `IChatClient` via `ChatClientFactory` (`AddKeyedSingleton<IChatClient>("Assistant", ...)`); `AddScoped<IAssistantService, ChatAssistantService>()`. `MapEndpoints`: `MapGroup("api/v{version:apiVersion}/assistant")` + AskAssistant.
- **`ExpendablePermissions.cs`**: add nested `SearchIndex` class (`Rebuild`) and register it in `ExpendableModule.RegisteredPermissions`. Register `ExpendableToolProvider`.
- **`AssetRegisterModule.cs`**: register `AssetRegisterToolProvider`. No new permissions — the tools reuse the module's existing `View` keys.
- **Host** (`AMIS.Api/Program.cs`): Mediator `o.Assemblies` += `typeof(AssistantModule)`, `typeof(AssistantContractsMarker)`, `typeof(AskAssistantCommand)`; `moduleAssemblies` += `typeof(AssistantModule).Assembly`.
- **`src/AMIS.Framework.slnx`** += both Assistant projects; **`AMIS.Api.csproj`** += both project references.

### Phase 6 — Blazor client + UI

See **[RAG-PoC-UI-Design.md](RAG-PoC-UI-Design.md)** — the full UI design (assistant panel + global launcher + Products page semantic-search/rebuild hooks + markdown rendering).

## Files

**New (Assistant module)** — `src/Modules/Assistant/`
- `Modules.Assistant.Contracts/` — `AssistantContractsMarker.cs`, `Permissions/AssistantPermissions.cs`, `v1/Assistant/AssistantContracts.cs`, `Tools/IAssistantToolProvider.cs`
- `Modules.Assistant/` — `AssistantModule.cs`, `AssistantModuleConstants.cs`, `AssemblyInfo.cs`, `Services/{AssistantOptions,ChatClientFactory,IAssistantService,ChatAssistantService}.cs`, `Features/v1/Assistant/AskAssistant/*`

**New (Expendable)**
- `Domain/Products/ProductEmbedding.cs`; `Data/Configurations/ProductEmbeddingConfiguration.cs`
- `Services/Embeddings/{IEmbeddingService,LocalEmbeddingService,IProductEmbeddingService,ProductEmbeddingService}.cs`
- `Services/Assistant/ExpendableToolProvider.cs`
- `Contracts/v1/Products/ProductSemanticSearchContracts.cs`
- `Features/v1/Products/SemanticSearchProducts/*`, `Features/v1/Products/RebuildProductEmbeddings/*`
- Migration in `src/Host/Migrations.PostgreSQL/Expendable/`

**New (AssetRegister)**
- `Services/Assistant/AssetRegisterToolProvider.cs` — *this is the entire cost of adding the module*

**Modified**
- `src/Directory.Packages.props`; `Modules.Expendable.csproj`; `Modules.AssetRegister.csproj`
- `src/BuildingBlocks/Persistence/OptionsBuilderExtensions.cs` + `Persistence.csproj` (**user-approved BuildingBlocks change**: `UseVector()` in the Npgsql branch)
- `src/Host/AMIS.AppHost/AppHost.cs` (pgvector-enabled Postgres image)
- `ExpendablePermissions.cs`; `ExpendableModule.cs`; `ExpendableDbContext.cs`; `ExpendableDbInitializer.cs`; `AssetRegisterModule.cs`
- `Features/v1/Products/CreateProduct|UpdateProduct|DeleteProduct` handlers (embedding hooks)
- `src/Host/AMIS.Api/Program.cs`; `AMIS.Api.csproj`; `src/AMIS.Framework.slnx`; `appsettings.json` + `.Development.json`
- Blazor — see the UI design doc

## Risks / verify-before-coding

1. `SmartComponents.LocalEmbeddings` is experimental (2024, net8.0 target) — smoke-test on .NET 10 first; fallback path defined. Verify thread-safety + L2 normalization.
2. `Anthropic` NuGet version — pin latest stable at install time. **Verify the SDK's `IChatClient` adapter API name**; if the adapter is missing/immature, fallback is a thin `IChatClient` implementation over the SDK's native `Messages.Create` (~80 lines), keeping the provider abstraction intact.
3. **`Pgvector.EntityFrameworkCore` vs EF Core 10** — verify the latest version restores and translates `CosineDistance` under EF Core 10.0.7. If it doesn't, the documented fallback is `float[]` (`real[]`) column + in-memory cosine behind the same handler signature — no other slice changes needed.
4. **BuildingBlocks blast radius** — `UseVector()` in `ConfigureHeroDatabase` touches every module's DbContext options. It's additive (type-mapping plugin only), but run the full solution build + test suite after the change.
5. **Postgres image swap** — `pgvector/pgvector:pgXX` must match the data volume's PG major version; otherwise start a fresh volume (dev data disposable). Non-Aspire environments need the pgvector extension installed on the server.
6. Extension creation (`CREATE EXTENSION vector`) requires elevated DB privileges — fine in dev containers; production DBAs may need to pre-create it.
7. ~~Tool-level authz is bypassed~~ → **Resolved by design.** Per-tool `RequiredPermission` + pre-model filtering + in-delegate re-check (Phase 3b). **This is now a correctness requirement, not a hardening step** — regression here silently leaks another module's data.
8. **New-module wiring is easy to half-do.** A missing `[assembly: AmisModule(...)]` leaves the Assistant module dormant with a **green build and passing tests** — the endpoint simply 404s. Check the attribute first when `/api/v1/assistant/ask` doesn't exist.
9. **Endpoint name collisions** — `.WithName()` must be globally unique or *every* request 500s. Use `Assistant_AskAssistant`, `Expendable_SemanticSearchProducts`, `Expendable_RebuildProductEmbeddings`.
10. API host must reach the configured provider's endpoint (`api.anthropic.com` by default); real keys in `.Development.json`/user-secrets only.
11. **Provider capability variance** — tool-calling quality differs by provider/model. `claude-opus-4-8` is the tuned default; small local Ollama models may call tools unreliably. Treat non-default providers as best-effort.
12. bge-micro is English-centric — Filipino term quality weaker; ILike `search_products` remains the exact-match fallback.
13. Semantic search is Postgres-only (`UseVector()` lives in the Npgsql branch); the MSSQL provider path would need the feature disabled — acceptable since all deployments run POSTGRESQL.

## Verification checklist

1. `dotnet build src/AMIS.Framework.slnx` — 0 warnings; `dotnet test` green (BuildingBlocks change affects all modules).
2. Scratch embed smoke test (384 floats) before wiring.
3. Run AppHost → pgvector image starts; `SELECT * FROM pg_extension WHERE extname = 'vector'` returns a row; migration applies; `expendable."ProductEmbeddings"` exists with `Embedding vector(384)`; other modules' contexts still initialize (proves `UseVector()` is benign).
4. `POST .../products/embeddings/rebuild` → counts = seeded products; re-run → all skipped (hash idempotent).
5. `GET .../products/semantic-search?query=paper for printing` → "Bond Paper A4" ranks first; SQL log shows the `<=>` cosine-distance operator (DB-side ranking, not client-side).
6. Create/update/delete a product → embedding row appears/changes/disappears without rebuild.
7. `POST /api/v1/assistant/ask` "How many ballpens in stock?" → quantities match `GetWarehouseStockLevels`; nonexistent-data question → "found nothing", no hallucinated numbers.
8. **AssetRegister reachable** — "Who is accountable for property no X?" returns the real custodian; "What's the book value of X?" matches the PPE ledger card.
9. **Cross-module answer (the whole point)** — "What has been issued to {employee}?" returns **both** their expendable supply issuances **and** their property accountabilities, in one answer, with tool chips from both modules.
10. **Per-tool permission filtering** — user with `Assets.View` but **not** `Accountability.View`: ask "who holds property no X?" → the assistant must say it cannot answer, and the custodian tool must not appear in the request's tool list at all (check the outbound payload/logs). Grant `Accountability.View`, re-ask → it answers. **This test is the security gate; do not ship without it passing.**
11. 403 without `Assistant.Use`; admin works (proves `RegisteredPermissions` landed for the new module).
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
