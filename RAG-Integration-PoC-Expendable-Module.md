# RAG Integration PoC — Expendable Module

> Status: **Planned — not yet implemented.** Approved design for a proof-of-concept; follow the phases in order.
> Date planned: 2026-07-08

## Context

The Expendable module's search is entirely `ILike` substring matching today, and there is zero AI/vector code anywhere in the solution. This PoC adds two RAG capabilities inside the Expendable module:

1. **Inventory Q&A assistant** — natural-language questions about expendable data (stock levels, request status, products), answered from live module data.
2. **Semantic product search** — embedding-based similarity search over products (find items by meaning, not just substring).

**Data policy: Hybrid** — embeddings computed **locally in-process** (bulk product data never leaves the server); generation via a **pluggable LLM provider** behind the standard `Microsoft.Extensions.AI` `IChatClient` abstraction — **default: Anthropic Claude** (`claude-opus-4-8`, official `Anthropic` C# SDK adapter), switchable by configuration to OpenAI/Azure OpenAI or a fully local Ollama model. Whichever provider is configured only ever sees the question plus the retrieved tool results.

**Architecture insight:** for structured inventory data, the retrieval half of "RAG" is best done as a **Claude tool-use loop over the module's existing query handlers** (tenant scoping holds for free), with the embedding-based semantic search serving both as a standalone endpoint and as one of the assistant's tools.

## Key decisions

| Decision | Choice | Why |
|---|---|---|
| Local embeddings | `SmartComponents.LocalEmbeddings` (bge-micro-v2 ONNX, 384-dim) behind a module-local `IEmbeddingService` | One-liner API, MIT, no external calls. Fallback if it misbehaves on .NET 10: `Microsoft.ML.OnnxRuntime` + `Microsoft.ML.Tokenizers` + all-MiniLM behind the same interface. |
| Vector storage | **pgvector** — `vector(384)` column on new `expendable.ProductEmbeddings` table via `Pgvector.EntityFrameworkCore`; similarity ranked **in the database** with `CosineDistance` | Native vector store on the Postgres we already run; scales past the in-memory ceiling and keeps the query one LINQ expression. Requires a **user-approved BuildingBlocks modification**: one `UseVector()` line inside the `UseNpgsql` options lambda in `OptionsBuilderExtensions.ConfigureHeroDatabase` (Postgres branch only — MSSQL branch untouched). Also requires a pgvector-enabled Postgres image in the AppHost. |
| Embedding lifecycle | Synchronous upsert in Create/Update/Delete product handlers (best-effort try/catch) + admin `POST /products/embeddings/rebuild` backfill endpoint; `ContentHash` column for idempotency | Local embedding is ms-scale CPU work — Hangfire adds nothing at PoC scale. Rebuild endpoint runs in caller's tenant context (correct tenancy for free). |
| LLM provider | **Pluggable via `Microsoft.Extensions.AI` `IChatClient`** — default `Anthropic` (`claude-opus-4-8`); `OpenAI`/`AzureOpenAI` and `Ollama` (local) selectable via `Provider` config | Assistant code depends only on `IChatClient` + `AIFunction` tools; each provider is one adapter registration. Anthropic's official C# SDK ships an IChatClient adapter; Ollama gives a zero-cloud option (upgrades Hybrid → fully self-hosted when required). |
| Assistant | **`IChatClient` function-invocation loop** (`.UseFunctionInvocation()` middleware runs the tool round-trips), non-streamed; stateless single-turn with client-side history replay (≤10 turns) | The middleware is the provider-agnostic equivalent of a manual tool loop; `MaxToolIterations` maps to its iteration cap, and per-tool try/catch lives inside each `AIFunction` delegate (return error text, never throw). Every tool dispatches through Mediator to the module's own handlers so Finbuckle tenant scoping applies. Blazor has no streaming plumbing — non-streamed PoC, SignalR streaming is the follow-up. |
| Permissions | `Permissions.Expendable.Assistant.Use` (IsBasic) for ask; `...Assistant.Manage` for rebuild; semantic search reuses `Products.View` | Must add to BOTH `ExpendablePermissions.cs` and `RegisteredPermissions` in `ExpendableModule.cs` (else 403 for everyone, admins included). Format verified against existing constants. |

## Implementation phases

### Phase 0 — Packages, infrastructure & config

- `src/Directory.Packages.props`: add `<PackageVersion>` entries for `Microsoft.Extensions.AI` (+ `Microsoft.Extensions.AI.Abstractions`), `Anthropic` (latest stable — verify on nuget.org; default provider adapter), `SmartComponents.LocalEmbeddings` (latest preview), and `Pgvector.EntityFrameworkCore` (latest — **verify EF Core 10 compatibility**; brings `Pgvector` transitively). Optional-provider adapters (`Microsoft.Extensions.AI.OpenAI`, `OllamaSharp`) are added **only when that provider is enabled** — not in the PoC baseline.
- Package references (version-less):
  - `Modules.Expendable.csproj`: `Microsoft.Extensions.AI`, `Anthropic`, `SmartComponents.LocalEmbeddings`, `Pgvector.EntityFrameworkCore`
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
  "ExpendableAssistantOptions": {
    "Provider": "Anthropic",          // Anthropic | OpenAI | AzureOpenAI | Ollama
    "ApiKey": "",
    "Model": "claude-opus-4-8",
    "Endpoint": null,                  // Azure OpenAI resource / Ollama URL; null for Anthropic & OpenAI
    "MaxTokens": 4096,
    "MaxToolIterations": 6
  }
  ```

- **Smoke test first**: scratch console `new LocalEmbedder().Embed("test")` → 384 floats, before wiring anything.

### Phase 1 — Embedding storage

- `Domain/Products/ProductEmbedding.cs` — plain entity: `Id`, `TenantId` (IHasTenant), `ProductId`, `Pgvector.Vector Embedding`, `ContentHash`, `ModelId`, `GeneratedOnUtc`.
  - **No `byte[] Version`/`IsRowVersion()`** (Npgsql insert breaker), no concurrency token, **no ISoftDeletable** (hard-delete with product; avoids EF10 named-filter conflict).
- `Data/Configurations/ProductEmbeddingConfiguration.cs` — `.ToTable("ProductEmbeddings", SchemaName).IsMultiTenant()`, unique index `(TenantId, ProductId)`, and:

  ```csharp
  builder.Property(x => x.Embedding).HasColumnType("vector(384)").IsRequired();
  ```

- `Data/ExpendableDbContext.cs`: add `DbSet<ProductEmbedding>`; in `OnModelCreating` add `modelBuilder.HasPostgresExtension("vector");` so the generated migration emits `CREATE EXTENSION IF NOT EXISTS vector` **before** creating the table (extension creation needs sufficient DB privileges — the dev container superuser is fine). Belt-and-braces: also add `CREATE EXTENSION IF NOT EXISTS vector;` next to the existing pgcrypto block in `ExpendableDbInitializer.MigrateAsync` so per-tenant databases (DB-per-tenant mode) get it too.
- Generate migration `AddProductEmbeddings` into `src/Host/Migrations.PostgreSQL/Expendable/` (assembly `AMIS.Playground.Migrations.PostgreSQL`); verify the column type is `vector(384)` and the extension annotation is present. The new table has no `Version` column, so the existing pgcrypto/Version backfill loop skips it.
- **No vector index at PoC scale** (sequential scan over hundreds–thousands of rows is fine). When volume grows, add an HNSW index in a later migration: `CREATE INDEX ... USING hnsw ("Embedding" vector_cosine_ops)`.

### Phase 2 — Embedding services + semantic search slice

- `Services/Embeddings/IEmbeddingService.cs` + `LocalEmbeddingService.cs` — singleton wrapping `LocalEmbedder` (add lock if thread-safety unverified).
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
- `Features/v1/Products/RebuildProductEmbeddings/{Handler,Endpoint}.cs` — `POST /products/embeddings/rebuild`, `.RequirePermission(Assistant.Manage)`, `.WithName("Expendable_RebuildProductEmbeddings")`, returns counts.
- Hook Create/Update/DeleteProduct handlers: after save, call upsert/remove in try/catch (log warning, never fail the business op).

### Phase 3 — Assistant slice (Claude tool loop)

- Contracts: `v1/Assistant/AssistantContracts.cs` — `AskAssistantCommand(string Question, IReadOnlyList<AssistantTurnDto>? History) : ICommand<AssistantAnswerDto>`; answer DTO includes `ToolsUsed`.
- `Services/Assistant/ExpendableAssistantOptions.cs` (`AddOptions<>().BindConfiguration(...)`) — includes `Provider`, `ApiKey`, `Model`, `Endpoint`, `MaxTokens`, `MaxToolIterations`.
- `Services/Assistant/ChatClientFactory.cs` — builds the provider-specific `IChatClient` from options and wraps it with the function-invocation middleware (registered as a keyed singleton so it never collides with other modules' future AI clients):

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
- `Services/Assistant/IAssistantService.cs` + `ChatAssistantService.cs` (scoped; keyed `IChatClient` + `IMediator` + options). Four tools declared as `AIFunctionFactory.Create(...)` delegates in `ChatOptions.Tools`, each dispatching via Mediator and serializing the result to compact JSON; wrap each delegate body in try/catch returning error text (never throw):
  - `semantic_product_search(query, top_k)` → new `SemanticSearchProductsQuery`
  - `search_products(keyword, status)` → existing `SearchProductsQuery`
  - `get_stock_levels(keyword?)` → existing `GetWarehouseStockLevelsQuery`
  - `search_supply_requests(status?, ...)` → existing `SearchSupplyRequestsQuery`
- Call shape: build `List<ChatMessage>` (system prompt + replayed history + question) → `await chatClient.GetResponseAsync(messages, new ChatOptions { Tools = [...], MaxOutputTokens = options.MaxTokens }, ct)` — the `UseFunctionInvocation` middleware runs the tool round-trips against whichever provider is configured. Collect `ToolsUsed` from the response's function-call contents for the answer DTO.
- System prompt: inventory assistant persona; **answer only from tool results, never invent quantities/statuses**; currency ₱; say so when tools return nothing.
- `Features/v1/Assistant/AskAssistant/{Handler,Validator,Endpoint}.cs` — thin handler delegating to `IAssistantService`; validator (question ≤2000 chars, history ≤10 turns); `POST /assistant/ask`, `.WithName("Expendable_AskAssistant")`, `.RequirePermission(Assistant.Use)`.

### Phase 4 — Module wiring

- `ExpendablePermissions.cs`: add nested `Assistant` class (`Use`, `Manage` — format `Permissions.Expendable.Assistant.Use`).
- `ExpendableModule.cs`:
  - `RegisteredPermissions` += `new("Use Expendable Assistant", "Use", "Expendable.Assistant", IsBasic: true)` and `new("Manage Expendable Assistant", "Manage", "Expendable.Assistant")`.
  - `ConfigureServices`: options binding; `AddSingleton<IEmbeddingService, LocalEmbeddingService>()`; `AddScoped<IProductEmbeddingService, ProductEmbeddingService>()`; keyed `IChatClient` via `ChatClientFactory` (`AddKeyedSingleton<IChatClient>("ExpendableAssistant", ...)`); `AddScoped<IAssistantService, ChatAssistantService>()`.
  - `MapEndpoints`: `var assistantGroup = moduleGroup.MapGroup("/assistant");` + map AskAssistant; SemanticSearch + Rebuild on existing `productsGroup`.

### Phase 5 — Blazor client + page

- `src/Host/AMIS.Blazor/ApiClient/ExpendableAssistantClient.cs` — hand-written `IExpendableAssistantClient` (`AskAsync` / `SemanticSearchAsync` / `RebuildEmbeddingsAsync`), modeled on `ApiClient/ChatClient.cs` (interim pattern until NSwag regen).
- `Services/Api/ApiClientRegistration.cs`: `AddTransient<IExpendableAssistantClient>(sp => new ExpendableAssistantClient(ResolveClient(sp)));`
- `Components/Pages/Expendable/InventoryAssistantPage.razor` — `@page "/expendable/assistant"`; transcript + **raw `MudTextField` Dense with `OnKeyDown` Enter-to-send** (AMIS* wrappers swallow EventCallbacks); "thinking…" indicator (10–30 s non-streamed); permission-gated on `Assistant.Use`.
- `Components/Layout/NavMenu.razor` — "Inventory Assistant" MudNavLink in the Expendable group (~lines 211–251), permission-gated.

## Files

**New (Contracts)** — `src/Modules/Expendable/Modules.Expendable.Contracts/`
- `v1/Assistant/AssistantContracts.cs`
- `v1/Products/ProductSemanticSearchContracts.cs`

**New (Module)** — `src/Modules/Expendable/Modules.Expendable/`
- `Domain/Products/ProductEmbedding.cs`
- `Data/Configurations/ProductEmbeddingConfiguration.cs`
- `Services/Embeddings/IEmbeddingService.cs`, `LocalEmbeddingService.cs`, `IProductEmbeddingService.cs`, `ProductEmbeddingService.cs`
- `Services/Assistant/ExpendableAssistantOptions.cs`, `ChatClientFactory.cs`, `IAssistantService.cs`, `ChatAssistantService.cs`
- `Features/v1/Products/SemanticSearchProducts/*`
- `Features/v1/Products/RebuildProductEmbeddings/*`
- `Features/v1/Assistant/AskAssistant/*`

**New (Host/Blazor)**
- Migration in `src/Host/Migrations.PostgreSQL/Expendable/`
- `src/Host/AMIS.Blazor/ApiClient/ExpendableAssistantClient.cs`
- `src/Host/AMIS.Blazor/Components/Pages/Expendable/InventoryAssistantPage.razor`

**Modified**
- `src/Directory.Packages.props`; `Modules.Expendable.csproj`
- `src/BuildingBlocks/Persistence/OptionsBuilderExtensions.cs` + `Persistence.csproj` (**user-approved BuildingBlocks change**: `UseVector()` in the Npgsql branch)
- `src/Host/AMIS.AppHost/AppHost.cs` (pgvector-enabled Postgres image)
- `src/Host/Migrations.PostgreSQL/Migrations.PostgreSQL.csproj` (Pgvector reference, if not transitive)
- `ExpendablePermissions.cs`; `ExpendableModule.cs`; `ExpendableDbContext.cs`; `ExpendableDbInitializer.cs` (vector extension)
- `Features/v1/Products/CreateProduct|UpdateProduct|DeleteProduct` handlers (embedding hooks)
- `src/Host/AMIS.Api/appsettings.json` + `appsettings.Development.json`
- `src/Host/AMIS.Blazor/Services/Api/ApiClientRegistration.cs`; `Components/Layout/NavMenu.razor`

## Risks / verify-before-coding

1. `SmartComponents.LocalEmbeddings` is experimental (2024, net8.0 target) — smoke-test on .NET 10 first; fallback path defined. Verify thread-safety + L2 normalization.
2. `Anthropic` NuGet version — pin latest stable at install time; compile-fix loop catches SDK shape drift. **Verify the SDK's `IChatClient` adapter API name** (Microsoft.Extensions.AI integration is documented but the exact extension method must be confirmed at install time); if the adapter is missing/immature, fallback is a thin `IChatClient` implementation over the SDK's native `Messages.Create` (~80 lines), keeping the provider abstraction intact.
3. **`Pgvector.EntityFrameworkCore` vs EF Core 10** — the package can lag major EF releases; verify the latest version restores and translates `CosineDistance` under EF Core 10.0.7 before committing to it. If it doesn't, the documented fallback is the earlier design: `float[]` (`real[]`) column + in-memory cosine behind the same handler signature — no other slice changes needed.
4. **BuildingBlocks blast radius** — `UseVector()` in `ConfigureHeroDatabase` touches every module's DbContext options. It's additive (type-mapping plugin only), but run the full solution build + test suite (`dotnet test src/AMIS.Framework.slnx`) after the change.
5. **Postgres image swap** — `pgvector/pgvector:pgXX` must match the data volume's PG major version; otherwise start a fresh volume (dev data disposable). Non-Aspire environments (production servers) need the pgvector extension package installed on the server.
6. Extension creation (`CREATE EXTENSION vector`) requires elevated DB privileges — fine in dev containers; production DBAs may need to pre-create it.
7. Tool-level authz: tools bypass endpoint permissions (acceptable for PoC — all read-only, endpoint gated by `Assistant.Use`); production hardening = per-tool permission checks.
8. API host must reach the configured provider's endpoint (`api.anthropic.com` by default); real keys in `.Development.json`/user-secrets only.
9. **Provider capability variance** — tool-calling quality differs by provider/model. `claude-opus-4-8` is the tuned default; small local Ollama models may call tools unreliably or hallucinate despite the grounding prompt — treat non-default providers as best-effort and validate per model before relying on them.
10. bge-micro is English-centric — Filipino term quality weaker; ILike `search_products` tool remains the exact-match fallback.
11. Semantic search is Postgres-only (`UseVector()` lives in the Npgsql branch); the MSSQL provider path would need the feature disabled — acceptable since all deployments run POSTGRESQL.

## Verification checklist

1. `dotnet build src/AMIS.Framework.slnx` — 0 warnings; `dotnet test` green (BuildingBlocks change affects all modules).
2. Scratch embed smoke test (384 floats) before wiring.
3. Run AppHost → pgvector image starts; `SELECT * FROM pg_extension WHERE extname = 'vector'` returns a row; migration applies; `expendable."ProductEmbeddings"` exists with `Embedding vector(384)`; other modules' contexts still initialize (proves `UseVector()` is benign).
4. `POST .../products/embeddings/rebuild` → counts = seeded products; re-run → all skipped (hash idempotent).
5. `GET .../products/semantic-search?query=paper for printing` → "Bond Paper A4" ranks first; SQL log shows the `<=>` cosine-distance operator (DB-side ranking, not client-side).
6. Create/update/delete a product → embedding row appears/changes/disappears without rebuild.
7. `POST .../assistant/ask` "How many ballpens in stock?" → quantities match `GetWarehouseStockLevels`; nonexistent-data question → "found nothing", no hallucinated numbers.
8. 403 without `Assistant.Use`/`Assistant.Manage`; admin works (proves `RegisteredPermissions` landed).
9. Two-tenant check: tenant A never sees tenant B products in semantic search or assistant answers.
10. Blazor: nav gated, Enter-to-send works, multi-turn follow-up via history replay works.
11. Provider abstraction: set `Provider` to an uninstalled value → startup/first-use fails with the descriptive "provider not installed" error (proves the switch works); optionally point `Provider: "Ollama"` at a local model and confirm the same assistant slice answers without code changes.

## Follow-ups (out of PoC scope)

- Token streaming of assistant answers over the existing SignalR `AppHub` (the `NotificationWriter` → `user:{id}` pattern).
- HNSW index on `ProductEmbeddings.Embedding` (`vector_cosine_ops`) once product counts justify it.
- Enable the non-default LLM providers: add the adapter package (`Microsoft.Extensions.AI.OpenAI` / `OllamaSharp`) + fill in the corresponding `ChatClientFactory` branch — config-only after that.
- Per-tool permission checks against the caller's permission set.
- NSwag client regeneration to replace the hand-written `ExpendableAssistantClient`.
- Extending the assistant's tool set (stock card, physical count report, consumption history).
