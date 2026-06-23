# AMIS Dynamic Report Designer — Implementation Plan

> The canonical technical spec for the report designer, grounded in the **actual**
> AMIS codebase (verified against `Modules.QuestPdfReporting`, `BuildingBlocks/Core`,
> `BuildingBlocks/Web`, and the existing reporting rules). Concept: serializable AST →
> recursive QuestPDF interpreter → Blazor canvas.
>
> **This file = the "what & how" (architecture, code shapes, rules A–I).**
> **Effort, sequencing, and where-to-stop cut-lines live in the companion
> [`AMIS_Report_Designer_Roadmap.md`](AMIS_Report_Designer_Roadmap.md).**
>
> **Naming note:** This plan uses numbered "Parts" (Part 1–6 below) for internal
> structure. The Roadmap uses "Phases" (Phase 0–5) for *sequencing and effort*.
> They are different axes — Parts describe *what to build*; Phases describe *when
> to build it and where to stop*. See the
> [cross-reference table](#plan-to-roadmap-cross-reference) at the end of this
> file.

---

## RULE 0 — The designer is greenfield and independent (scope)

The report designer is a **new, self-contained capability**. It does **not** depend on
the existing report engines (`QuestPdfReporting`, `RdlcReporting`, `FastReporting`),
and you are free to design any report layout and any data source you want. Only two
constraints apply, and both are about correctness — not dependencies:

1. **Don't expose the statutory forms for canvas editing.** ICS, PAR, PO, PR, DV, BUR,
   RSPI and similar government forms are legally fixed layouts that already exist in
   code/`.frx`/`.rdlc`. The designer is for **new, ad-hoc, user-defined reports** — it
   never edits the official forms. (You *may* later let a user *clone* one as a
   starting point, but the statutory originals stay authoritative.)
2. **Data stays tenant-scoped.** This system tracks government property under strict
   accountability and is multi-tenant. However customizable the data source is, a
   report must only ever read the **current tenant's** data. This is the one hard rule.

Everything else — layout, styling, components, template format, and the data-source
model — is yours to define. The designer reuses QuestPDF for rendering (and therefore
`QuestPdfReporting`'s license/`ReportAssets`/paper-size setup), but that's an
implementation convenience, not a dependency on the existing reports.

---

## Confirmed conventions (verified in repo — no more "confirm-in-repo" guesses)

| Concern | Actual type / namespace | Source |
|---|---|---|
| Module contract | `IModule` → `AMIS.Framework.Web.Modules` (`ConfigureServices(IHostApplicationBuilder)`, `MapEndpoints(IEndpointRouteBuilder)`) | `BuildingBlocks/Web/Modules/IModule.cs` |
| Mediator | `IQueryHandler<,>` / `IQuery<>` / `ICommandHandler<,>` from **`Mediator`** namespace; handlers return `ValueTask<T>` | `QuestPdfReporting` handlers |
| Entity base | `BaseEntity<TId>` (generic) in `AMIS.Framework.Core.Domain`; `Id` is `protected set` — **never set Id manually** | `BuildingBlocks/Core/Domain/BaseEntity.cs` |
| Auditing | interface is **`IAuditableEntity`** (not `IAuditable`) | `BuildingBlocks/Core/Domain/IAuditableEntity.cs` |
| Tenancy | `IMustHaveTenant` (`Guid TenantId`), auto-filtered; `BaseDbContext : MultiTenantDbContext` | `persistence.md`, `BaseDbContext.cs` |
| Not-found | this module family throws **`KeyNotFoundException`** (not a custom `NotFoundException`) | `PrintBudgetUtilizationRecordQueryHandler.cs` |
| QuestPDF license | `QuestPDF.Settings.License = LicenseType.Community;` in `ConfigureServices` | `QuestPdfReportingModule.cs` |
| Permissions | `PermissionConstants.Register(<ModuleConstants>.Permissions)` from `AMIS.Framework.Shared.Constants` | `QuestPdfReportingModule.cs` |
| Render call | `new MyDoc(...).GeneratePdf()` — `GeneratePdf()` is a QuestPDF **extension on `IDocument`**; do NOT define your own method named `GeneratePdf` (infinite recursion) | `QuestPdfReporting` handlers |
| Static assets | `<EmbeddedResource Include="ReportAssets\**\*.png" />` for logos | `Modules.QuestPdfReporting.csproj` |

### Module decision

The designer needs **persistence** (saved templates) — which `QuestPdfReporting`
deliberately lacks. So create a **new module `Modules.ReportDesigner`** with its own
`ReportDesignerDbContext`. It **renders** like `QuestPdfReporting` (QuestPDF, mediator
data fetch) and **persists** like `MasterData` (own DbContext, migrations). It must
**not** add a DbContext into `QuestPdfReporting` (keeps that module stateless).

---

## Phase 1 — The Shared AST (Contracts) + Domain Aggregate

### 1a. AST POCOs — `Modules.ReportDesigner.Contracts` (shared by canvas, persistence, engine)

```csharp
namespace AMIS.Modules.ReportDesigner.Contracts.v1.Designer.Ast;

public static class AstSchema
{
    public const int CurrentAstSchemaVersion = 1; // bump on breaking AST shape changes
}

public enum NodeType { Page, Row, Column, Text, Table, Image, Spacer }

/// <summary>Serializable layout node. Persisted as jsonb, rendered by the canvas,
/// walked by the QuestPDF interpreter. Mutable by design (the editor mutates it).</summary>
public sealed class ReportNode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public NodeType Type { get; set; }

    // Layout — stored in POINTS (QuestPDF native), never px. See RULE C2.
    public string? BackgroundColor { get; set; }   // "#RRGGBB" only — see RULE C3
    public float? MarginTop { get; set; }
    public float? MarginBottom { get; set; }
    public float? MarginLeft { get; set; }
    public float? MarginRight { get; set; }
    public float? Padding { get; set; }

    // Text
    public string? Content { get; set; }            // tokens: "PropertyNo: {{Asset.PropertyNo}}"
    public int FontSize { get; set; } = 11;
    public bool IsBold { get; set; }
    public string Alignment { get; set; } = "Left"; // Left|Center|Right|Justify

    // Table — rows are DATA-BOUND, not child nodes (see RULE C1)
    public List<TableColumnDefinition> TableColumns { get; set; } = [];
    public string? BindSourceCollection { get; set; } // e.g. "Assets"

    public List<ReportNode> Children { get; set; } = [];
}

public sealed class TableColumnDefinition
{
    public string HeaderText { get; set; } = string.Empty;
    public string DataToken { get; set; } = string.Empty; // row-relative: "{{Description}}"
    public float WidthRatio { get; set; } = 1f;
}
```

### 1b. Aggregate — `Modules.ReportDesigner/Domain/ReportTemplate.cs`

```csharp
using AMIS.Framework.Core.Domain;                 // BaseEntity<TId>
using AMIS.Framework.Core.Domain.Contracts;       // IAuditableEntity, IMustHaveTenant (verify exact ns)
using AMIS.Modules.ReportDesigner.Contracts.v1.Designer.Ast;

namespace AMIS.Modules.ReportDesigner.Domain;

public sealed class ReportTemplate : BaseEntity<Guid>, IAuditableEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }                       // Finbuckle auto-filter
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public Guid DataSourceId { get; private set; }            // FK → ReportDataSource.Id
    public int SchemaVersion { get; private set; }           // AST schema version at save time
    public ReportNode RootNode { get; private set; } = default!;

    private ReportTemplate() { }                             // EF

    public static ReportTemplate Create(string name, Guid dataSourceId, ReportNode rootNode, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (dataSourceId == Guid.Empty) throw new ArgumentException("DataSourceId must not be empty.", nameof(dataSourceId));
        ArgumentNullException.ThrowIfNull(rootNode);
        // Id is set by the framework (protected set) — do NOT assign it here.
        return new ReportTemplate
        {
            Name = name.Trim(),
            DataSourceId = dataSourceId,
            Description = description?.Trim(),
            RootNode = rootNode,
            SchemaVersion = AstSchema.CurrentAstSchemaVersion
        };
    }

    public void Update(string name, string? description, Guid dataSourceId, ReportNode rootNode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (dataSourceId == Guid.Empty) throw new ArgumentException("DataSourceId must not be empty.", nameof(dataSourceId));
        ArgumentNullException.ThrowIfNull(rootNode);
        Name = name.Trim();
        Description = description?.Trim();
        DataSourceId = dataSourceId;
        RootNode = rootNode;
        SchemaVersion = AstSchema.CurrentAstSchemaVersion;
    }
}
```

---

## Phase 1b — The Data Source Designer (admin-defined, runtime) ← your chosen model

Reports bind to **admin-defined data sources**, not hardcoded field lists. An admin
composes a data source at runtime from **reportable sources that modules publish**; the
system reads its schema, and that schema becomes the canvas field-picker. Fully
customizable *and* tenant-safe — every reportable source returns tenant-scoped,
read-only rows. Below is the implementation-ready shape, ending in a worked `Assets`
source built on the real `AssetRegister` contracts.

### 1b.1 — Schema vocabulary (`Contracts/v1/DataSources/`)

```csharp
namespace AMIS.Modules.ReportDesigner.Contracts.v1.DataSources;

public enum ReportFieldType { Text, Number, Money, Date, DateTime, Bool, Enum }
public enum RelationCardinality { ManyToOne, OneToMany }

/// <summary>One bindable field a source exposes. <c>Name</c> is the token segment used
/// in templates: a field named "PropertyNo" is bound as {{PropertyNo}}.</summary>
public sealed record ReportableField(
    string Name,
    string DisplayName,
    ReportFieldType Type,
    string? DefaultFormat = null,   // token-grammar format key, e.g. "money", "date" (RULE D4)
    bool IsNullable = false);

/// <summary>A navigable relation to another reportable source.
/// ManyToOne flattens into the row as "Custodian.FullName"; OneToMany is a child collection.</summary>
public sealed record ReportableRelation(
    string Name,                    // token prefix / collection name, e.g. "Custodian"
    string DisplayName,
    string TargetSourceKey,         // another IReportableSource.Key, e.g. "Employees"
    string ForeignKeyField,         // field on THIS source, e.g. "CurrentCustodianId"
    RelationCardinality Cardinality = RelationCardinality.ManyToOne);
```

### 1b.2 — Render-time request (what the provider hands a source)

```csharp
public enum FilterOperator { Equals, NotEquals, Contains, GreaterThan, LessThan, Between, In }

public sealed record DataSourceFilter(string FieldPath, FilterOperator Operator, object? Value, object? Value2 = null);
public sealed record DataSourceSort(string FieldPath, bool Descending = false);

/// <summary>Derived from the saved spec at render time. Tenant is AMBIENT — resolved
/// inside the source's owning module — never passed across the boundary (RULE D2).</summary>
public sealed record ReportSourceRequest(
    IReadOnlyList<string> SelectedFields,      // root field names to project
    IReadOnlyList<string> IncludedRelations,   // relation names to resolve/flatten
    IReadOnlyList<DataSourceFilter> Filters,
    IReadOnlyList<DataSourceSort> Sorts,
    int MaxRows = 5000);
```

### 1b.3 — The source contract (projects to dictionaries → zero reflection downstream)

```csharp
public interface IReportableSource
{
    string Key { get; }                                  // "Assets"
    string DisplayName { get; }                          // "Asset Registry"
    IReadOnlyList<ReportableField> Fields { get; }
    IReadOnlyList<ReportableRelation> Relations { get; }

    /// MUST return TENANT-SCOPED, read-only rows keyed by field name. ManyToOne relation
    /// fields are flattened as "Relation.Field" (e.g. "Custodian.FullName").
    ValueTask<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryAsync(
        ReportSourceRequest request, CancellationToken ct);
}
```

> **Payoff:** because sources emit plain dictionaries keyed by catalog tokens, the
> binding resolver becomes a **dictionary lookup + format** — *no reflection over live
> entities at all*. That single design choice satisfies RULE B4/D3 by construction.

### 1b.4 — The saved definition (`Domain/` aggregate + jsonb spec)

```csharp
public sealed record SelectedField(string FieldPath, string? Alias = null, string? FormatOverride = null);
public sealed record IncludedRelation(string RelationName, IReadOnlyList<SelectedField> Fields);

/// <summary>The admin's saved composition — persisted as jsonb on ReportDataSource.
/// The bindable catalog shown in the canvas is DERIVED from this (1b.5).</summary>
public sealed record DataSourceSpec(
    string RootSourceKey,
    IReadOnlyList<SelectedField> Fields,
    IReadOnlyList<IncludedRelation> Relations,
    IReadOnlyList<DataSourceFilter> Filters,
    IReadOnlyList<DataSourceSort> Sorts,
    int RowLimit = 5000);

public sealed class ReportDataSource : BaseEntity<Guid>, IAuditableEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = default!;
    public string RootSourceKey { get; private set; } = default!;
    public DataSourceSpec Spec { get; private set; } = default!;   // jsonb (same pattern as ReportNode)

    private ReportDataSource() { }
    public static ReportDataSource Create(string name, DataSourceSpec spec)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(spec);
        return new ReportDataSource { Name = name.Trim(), RootSourceKey = spec.RootSourceKey, Spec = spec };
    }
    public void Update(string name, DataSourceSpec spec)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(spec);
        Name = name.Trim(); RootSourceKey = spec.RootSourceKey; Spec = spec;
    }
}
```

`ReportTemplate.DataSourceKey` holds the `ReportDataSource.Id`. The "allow-list" is the
admin's selection — generated, never hand-written.

### 1b.5 — Deriving the catalog (the allow-list = what the admin chose)

```csharp
public sealed record CatalogEntry(string Token, string DisplayName, ReportFieldType Type, string? Format);

public static class DataSourceCatalog
{
    /// Tokens the field-picker offers and the resolver enforces (RULE D3).
    public static IReadOnlyList<CatalogEntry> Build(
        DataSourceSpec spec, IReportableSource root, Func<string, IReportableSource> resolve)
    {
        var entries = new List<CatalogEntry>();

        foreach (var sel in spec.Fields)
        {
            var f = root.Fields.First(x => x.Name == sel.FieldPath);
            entries.Add(new(sel.Alias ?? f.Name, f.DisplayName, f.Type, sel.FormatOverride ?? f.DefaultFormat));
        }

        foreach (var rel in spec.Relations)
        {
            var relDef = root.Relations.First(r => r.Name == rel.RelationName);
            var target = resolve(relDef.TargetSourceKey);
            foreach (var sel in rel.Fields)
            {
                var f = target.Fields.First(x => x.Name == sel.FieldPath);
                entries.Add(new(
                    $"{relDef.Name}.{sel.Alias ?? f.Name}",          // "Custodian.FullName"
                    $"{relDef.DisplayName} → {f.DisplayName}",
                    f.Type, sel.FormatOverride ?? f.DefaultFormat));
            }
        }
        return entries;
    }
}
```

### 1b.6 — Worked example: the `Assets` source (real `AssetRegister` contracts)

```csharp
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;     // AssetRegistrySummaryDto, SearchAssetsQuery
using AMIS.Modules.ReportDesigner.Contracts.v1.DataSources;
using Mediator;

namespace AMIS.Modules.ReportDesigner.Sources;

/// <summary>Publishes the Asset Registry as a reportable source. Fetches ONLY through
/// AssetRegister.Contracts via mediator — tenant scoping is applied by that module's
/// SearchAssetsQuery handler, so reports are tenant-safe by construction (RULE D2/A2).</summary>
public sealed class AssetsReportableSource(IMediator mediator) : IReportableSource
{
    public string Key => "Assets";
    public string DisplayName => "Asset Registry";

    public IReadOnlyList<ReportableField> Fields { get; } =
    [
        new("PropertyNo",       "Property No",      ReportFieldType.Text),
        new("Description",      "Description",      ReportFieldType.Text),
        new("AssetType",        "Asset Type",       ReportFieldType.Enum),
        new("UnitCost",         "Unit Cost",        ReportFieldType.Money, DefaultFormat: "money"),
        new("AcquisitionDate",  "Acquisition Date", ReportFieldType.Date,  DefaultFormat: "date"),
        new("LifecycleState",   "Lifecycle State",  ReportFieldType.Enum),
        new("CurrentCondition", "Condition",        ReportFieldType.Enum),
    ];

    public IReadOnlyList<ReportableRelation> Relations { get; } =
    [
        // Each asset has one custodian → flattened tokens Custodian.FullName / Custodian.Position.
        new("Custodian", "Custodian", TargetSourceKey: "Employees",
            ForeignKeyField: "CurrentCustodianId", RelationCardinality.ManyToOne),
    ];

    public async ValueTask<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryAsync(
        ReportSourceRequest request, CancellationToken ct)
    {
        // 1. Pull tenant-scoped assets via Contracts (page until MaxRows or exhausted).
        var assets = new List<AssetRegistrySummaryDto>();
        var page = 1;
        while (assets.Count < request.MaxRows)
        {
            var result = await mediator.Send(new SearchAssetsQuery(PageNumber: page, PageSize: 200), ct);
            assets.AddRange(result.Items);
            if (!result.HasNext) break;
            page++;
        }

        // 2. Resolve the Custodian relation only if the spec included it (ManyToOne flatten).
        var custodians = request.IncludedRelations.Contains("Custodian")
            ? await LoadCustodiansAsync(assets, ct)
            : new Dictionary<Guid, (string FullName, string Position)>();

        // 3. Project ONLY the selected fields into row dictionaries.
        return assets.Take(request.MaxRows).Select(a =>
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in request.SelectedFields)
                row[field] = field switch
                {
                    "PropertyNo"       => a.PropertyNo,
                    "Description"      => a.Description,
                    "AssetType"        => a.AssetType.ToString(),
                    "UnitCost"         => a.UnitCost,            // decimal → resolver formats as money
                    "AcquisitionDate"  => a.AcquisitionDate,    // DateOnly → resolver formats as date
                    "LifecycleState"   => a.LifecycleState.ToString(),
                    "CurrentCondition" => a.CurrentCondition.ToString(),
                    _ => null
                };

            if (custodians.Count > 0 && a.CurrentCustodianId is { } id && custodians.TryGetValue(id, out var c))
            {
                row["Custodian.FullName"] = c.FullName;
                row["Custodian.Position"] = c.Position;
            }
            return (IReadOnlyDictionary<string, object?>)row;
        }).ToList();
    }

    private async ValueTask<Dictionary<Guid, (string FullName, string Position)>> LoadCustodiansAsync(
        IReadOnlyList<AssetRegistrySummaryDto> assets, CancellationToken ct)
    {
        var ids = assets.Where(a => a.CurrentCustodianId is not null)
                        .Select(a => a.CurrentCustodianId!.Value).Distinct().ToList();
        if (ids.Count == 0) return [];

        // Resolve via the owning module's Contracts (e.g. MasterData employees) — tenant-scoped there.
        // var employees = await mediator.Send(new GetEmployeesByIdsQuery(ids), ct);
        // return employees.ToDictionary(e => e.Id, e => (e.FullName, e.Position));
        return [];   // ← wire to the real employee contract when the "Employees" source lands
    }
}
```

### 1b.7 — Provider + DI + how it feeds the engine

```csharp
public sealed record ReportRenderData(
    /// <summary>Key = <c>IReportableSource.Key</c> (e.g. "Assets");
    /// value = the projected row dictionaries. A Table node with
    /// <c>BindSourceCollection = "Assets"</c> looks this key up.</summary>
    IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> Collections,
    IReadOnlyList<CatalogEntry> Catalog,
    string RootSourceKey);

/// <summary>Renders a saved <see cref="DataSourceSpec"/> into row dictionaries the engine
/// can bind. Implementations must be <c>Scoped</c> because sources resolve tenant context.</summary>
public interface IReportDataSourceProvider
{
    ValueTask<ReportRenderData> LoadAsync(DataSourceSpec spec, CancellationToken ct);
}

/// <summary>Everything the QuestPDF engine needs per render: data, catalog, page settings.
/// Constructed in the handler from <see cref="ReportRenderData"/> + template page settings.</summary>
public sealed record ReportRenderContext(
    IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> Data,
    IReadOnlyList<CatalogEntry> Catalog,
    PageSize PageSize,
    float Margin);

public sealed class ReportDataSourceProvider(IEnumerable<IReportableSource> sources)
    : IReportDataSourceProvider
{
    private IReportableSource Resolve(string key) =>
        sources.FirstOrDefault(s => s.Key == key)
        ?? throw new KeyNotFoundException($"Reportable source '{key}' is not registered.");

    public async ValueTask<ReportRenderData> LoadAsync(DataSourceSpec spec, CancellationToken ct)
    {
        var root = Resolve(spec.RootSourceKey);
        var request = new ReportSourceRequest(
            SelectedFields:    spec.Fields.Select(f => f.FieldPath).ToList(),
            IncludedRelations: spec.Relations.Select(r => r.RelationName).ToList(),
            Filters:           spec.Filters,
            Sorts:             spec.Sorts,
            MaxRows:           spec.RowLimit);

        var rows    = await root.QueryAsync(request, ct);
        var catalog = DataSourceCatalog.Build(spec, root, Resolve);
        // Collections keyed by source key so a Table's BindSourceCollection resolves correctly.
        var collections = new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>>
        {
            [spec.RootSourceKey] = rows
        };
        return new ReportRenderData(collections, catalog, spec.RootSourceKey);
    }
}
```

```csharp
// DI registration — each adapter is one line (ReportDesignerModule.ConfigureServices):
builder.Services.AddScoped<IReportableSource, AssetsReportableSource>();
builder.Services.AddScoped<IReportDataSourceProvider, ReportDataSourceProvider>();
```

Wiring into the render flow (Phase 3 handler / Phase 4 engine):

1. `GenerateReportHandler` loads the template, then loads `ReportDataSource` by
   `template.DataSourceId`, then calls `provider.LoadAsync(dataSource.Spec, ct)`.
2. Build the engine context: `new ReportRenderContext(renderData.Collections, renderData.Catalog, pageSize, margin)`.
   A Table node with `BindSourceCollection = "Assets"` resolves its rows via
   `ctx.Data["Assets"]` — the collection key matches `IReportableSource.Key`.
3. `ResolveCell(row, token)` is now just `row.TryGetValue(token, out var v)` + format —
   reflection-free. A token absent from the catalog fails validation on save.

> **Build order:** ship the `Assets` source end-to-end first (publish → Data Source
> Designer picks fields → template binds → PDF). Adding `Employees`, `Vehicles`, etc.
> later is one `IReportableSource` + one DI line each — **zero engine/designer changes**.

> **Boundary note:** the adapter lives in `Modules.ReportDesigner` and references only
> `AssetRegister.Contracts` (the same pattern `QuestPdfReporting` already uses to
> reference many `*.Contracts`). Alternatively the owning module can publish its own
> source by referencing `ReportDesigner.Contracts` — pick one convention and keep it.

---

## Phase 2 — Persistence (`BaseDbContext`, jsonb, Npgsql concurrency)

```csharp
public sealed class ReportDesignerDbContext(/* BaseDbContext ctor deps */ DbContextOptions<ReportDesignerDbContext> options /* + tenant/settings/env per BaseDbContext */)
    : BaseDbContext(/* pass through */)
{
    public DbSet<ReportTemplate> ReportTemplates => Set<ReportTemplate>();
    public DbSet<ReportDataSource> ReportDataSources => Set<ReportDataSource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ReportDesignerModuleConstants.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReportDesignerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

```csharp
internal sealed class ReportTemplateConfiguration : IEntityTypeConfiguration<ReportTemplate>
{
    public void Configure(EntityTypeBuilder<ReportTemplate> builder)
    {
        builder.ToTable("report_templates", ReportDesignerModuleConstants.SchemaName);
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(1000);
        builder.Property(t => t.DataSourceId).IsRequired();
        builder.Property(t => t.SchemaVersion);
        builder.HasIndex(t => t.DataSourceId);

        builder.Property(t => t.RootNode)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, ReportJson.Options),
                v => JsonSerializer.Deserialize<ReportNode>(v, ReportJson.Options)!);

        // ⚠️ Concurrency: use Postgres xmin, NOT a byte[] RowVersion.
        //    byte[] IsRowVersion() breaks Npgsql INSERTs (NOT NULL violation).
        builder.UseXminAsConcurrencyToken();

        builder.HasIndex(t => t.TenantId);
    }
}
```

```csharp
internal sealed class ReportDataSourceConfiguration : IEntityTypeConfiguration<ReportDataSource>
{
    public void Configure(EntityTypeBuilder<ReportDataSource> builder)
    {
        builder.ToTable("report_data_sources", ReportDesignerModuleConstants.SchemaName);
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.RootSourceKey).IsRequired().HasMaxLength(100);

        builder.Property(d => d.Spec)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, ReportJson.Options),
                v => JsonSerializer.Deserialize<DataSourceSpec>(v, ReportJson.Options)!);

        builder.UseXminAsConcurrencyToken();
        builder.HasIndex(d => d.TenantId);
        builder.HasIndex(d => d.RootSourceKey);
    }
}
```

> **Soft-delete caution:** if `ReportTemplate` needs soft delete, rely on whatever
> `BaseDbContext` already provides. Do **not** implement `ISoftDeletable` *and* add a
> second named `SoftDelete` query filter — that combination fails EF10 `migrations add`.

```csharp
public static class ReportDesignerModuleConstants
{
    public const string SchemaName = "report_designer";

    public static class Permissions
    {
        public const string View     = "reportdesigner.templates.view";
        public const string Create   = "reportdesigner.templates.create";
        public const string Update   = "reportdesigner.templates.update";
        public const string Delete   = "reportdesigner.templates.delete";
        public const string Generate = "reportdesigner.reports.generate";
    }
}
```

Add `ReportDesignerDbContextFactory` (design-time) and `ReportDesignerDbInitializer`
(`IDbInitializer`, `MigrateAsync`/`SeedAsync`) under `Data/`, mirroring `MasterData`.

---

## Phase 3 — Application slices (Mediator; `Generate` + curated data fetch)

```csharp
namespace AMIS.Modules.ReportDesigner.Contracts.v1.Designer.GenerateReport;
using Mediator;
public sealed record GenerateReportQuery(Guid TemplateId) : IQuery<byte[]>;
```

```csharp
namespace AMIS.Modules.ReportDesigner.Features.v1.Designer.GenerateReport;
using Mediator;

public sealed class GenerateReportHandler(
    ReportDesignerDbContext db,                 // own templates: direct read is fine
    IReportDataSourceProvider dataSources,      // curated, mediator-backed (RULE D1)
    IReportBindingResolver resolver)
    : IQueryHandler<GenerateReportQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(GenerateReportQuery request, CancellationToken ct)
    {
        var template = await db.ReportTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, ct)   // tenant auto-filtered
            ?? throw new KeyNotFoundException($"Report template '{request.TemplateId}' not found.");

        var dataSource = await db.ReportDataSources
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == template.DataSourceId, ct)
            ?? throw new KeyNotFoundException($"Data source '{template.DataSourceId}' not found.");

        ReportAstValidator.EnsureValid(template.RootNode, dataSource.Spec); // RULE C/D re-check on render

        var renderData = await dataSources.LoadAsync(dataSource.Spec, ct);  // fetch via Contracts
        // Inject report-level scalar tokens (e.g. {{ReportDate}}, {{OrganizationName}})
        // as a reserved "$scalars" collection so Text nodes in headers/footers can bind them.
        var scalars = new Dictionary<string, object?>
        {
            ["ReportDate"] = DateOnly.FromDateTime(DateTime.UtcNow),
            // add org profile fields from IOrganizationProfileState when wiring Blazor
        };
        var collections = new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>>(renderData.Collections)
        {
            ["$scalars"] = [scalars]
        };
        var ctx = new ReportRenderContext(collections, renderData.Catalog,
            PageSizes.A4, margin: 40f); // TODO: read from template page settings once added
        var engine = new QuestReportEngine(template.RootNode, ctx, resolver);
        return engine.GeneratePdf();    // QuestPDF extension on IDocument
    }
}
```

```csharp
public sealed class GenerateReportValidator : AbstractValidator<GenerateReportQuery>
{
    public GenerateReportValidator() => RuleFor(x => x.TemplateId).NotEmpty();
}
```

```csharp
public static class GenerateReportEndpoint
{
    public static RouteHandlerBuilder MapGenerateReport(this IEndpointRouteBuilder group) =>
        group.MapGet("/{templateId:guid}/generate",
            async (Guid templateId, IMediator mediator, CancellationToken ct) =>
            {
                var pdf = await mediator.Send(new GenerateReportQuery(templateId), ct);
                return Results.File(pdf, "application/pdf", $"report-{templateId}.pdf");
            })
        .WithName("ReportDesigner_GenerateReport")          // module-prefixed → globally unique
        .WithSummary("Render a saved report template to PDF")
        .Produces(200, null, "application/pdf")
        .RequirePermission(ReportDesignerModuleConstants.Permissions.Generate);
}
```

CRUD slices (`Create/Update/Get/List`) follow the same shape: `ICommand`/`IQuery`,
a validator, `ReportDesigner_`-prefixed endpoint name, `TypedResults`,
`.RequirePermission(...)`.

### `PreviewReport` slice (live designer loop)

Accepts an *unsaved* AST + a resolved data-source id — no template record required.
Gated by the `Generate` permission. The handler re-validates the AST server-side
(RULE B3) before rendering to avoid trusting a client-supplied node tree.

```csharp
// Contract (in Contracts project)
public sealed record PreviewReportQuery(
    Guid DataSourceId,
    ReportNode RootNode)      // unsaved AST from the canvas
    : IQuery<byte[]>;

// Endpoint
public static RouteHandlerBuilder MapPreviewReport(this IEndpointRouteBuilder group) =>
    group.MapPost("/preview",
        async ([FromBody] PreviewReportQuery query, IMediator mediator, CancellationToken ct) =>
        {
            var pdf = await mediator.Send(query, ct);
            return Results.File(pdf, "application/pdf", "preview.pdf");
        })
    .WithName("ReportDesigner_PreviewReport")
    .WithSummary("Render an unsaved AST to PDF for live preview")
    .Produces(200, null, "application/pdf")
    .RequirePermission(ReportDesignerModuleConstants.Permissions.Generate);
```

The handler mirrors `GenerateReportHandler` but skips the template DB load:
loads `ReportDataSource` by id → validates AST → `dataSources.LoadAsync` → render.
**Never skip `ReportAstValidator.EnsureValid` on preview** (B3).

---

## Phase 4 — QuestPDF interpreter (`Engine/`)

```csharp
// Binding resolver — nested paths + formatting, bounded by the derived allow-list (RULE D)
public interface IReportBindingResolver
{
    /// <summary>Expands <c>{{Token}}</c> / <c>{{Token:format}}</c> in <paramref name="content"/>
    /// using scalar values from <paramref name="rootData"/>. Tokens absent from
    /// <paramref name="catalog"/> resolve to empty string (RULE D3).</summary>
    string BindText(
        string? content,
        IReadOnlyDictionary<string, object?> rootData,
        IReadOnlyList<CatalogEntry> catalog);

    /// <summary>Resolves a single <paramref name="token"/> against one data row.
    /// Row dictionary keys are already the catalog tokens (e.g. "PropertyNo",
    /// "Custodian.FullName") so this is a dictionary lookup + format, no reflection.</summary>
    string ResolveCell(
        IReadOnlyDictionary<string, object?> row,
        string token,
        IReadOnlyList<CatalogEntry> catalog);
}
```

```csharp
public sealed class QuestReportEngine(
    ReportNode root,
    ReportRenderContext ctx,                  // data + DataSourceKey + page settings
    IReportBindingResolver resolver) : IDocument
{
    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(ctx.PageSize);          // reuse GovernmentPaperSizes mapping
            page.MarginPoint(ctx.Margin);
            page.PageColor(QuestPDF.Helpers.Colors.White);
            page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11));

            page.Content().Column(col =>
            {
                foreach (var child in root.Children)
                    RenderNode(col.Item(), child);
            });

            page.Footer().AlignCenter().Text(t => { t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); });
        });
    }

    private void RenderNode(IContainer c, ReportNode node)
    {
        if (node.Padding is { } p) c = c.Padding(p);
        if (!string.IsNullOrWhiteSpace(node.BackgroundColor)) c = c.Background(node.BackgroundColor);

        switch (node.Type)
        {
            case NodeType.Row:    c.Row(r => { foreach (var ch in node.Children) RenderNode(r.RelativeItem(), ch); }); break;
            case NodeType.Column: c.Column(x => { foreach (var ch in node.Children) RenderNode(x.Item(), ch); }); break;
            case NodeType.Spacer: c.Height(node.MarginTop ?? 10); break;
            case NodeType.Text:
                c.Text(text =>
                {
                    // rootData for BindText = flatten root-collection row 0 for scalar fields;
                    // for a full-page scalar text node, pass an empty dict — tokens like
                    // {{TotalItems}} must come from the handler-injected ctx.Data scalar map.
                    var rootScalars = ctx.Data.TryGetValue("$scalars", out var s)
                        ? s.FirstOrDefault() ?? new Dictionary<string, object?>()
                        : new Dictionary<string, object?>();
                    var span = text.Span(resolver.BindText(node.Content, rootScalars, ctx.Catalog)).FontSize(node.FontSize);
                    if (node.IsBold) span.Bold();
                    ApplyAlignment(text, node.Alignment);
                });
                break;
            case NodeType.Table: RenderTable(c, node); break;
            case NodeType.Image: RenderImage(c, node); break;
        }
    }

    private void RenderTable(IContainer c, ReportNode node)
    {
        if (node.BindSourceCollection is null ||
            !ctx.Data.TryGetValue(node.BindSourceCollection, out var rows) ||
            rows.Count == 0) return;

        c.Table(table =>
        {
            table.ColumnsDefinition(cols => { foreach (var col in node.TableColumns) cols.RelativeColumn(col.WidthRatio); });
            table.Header(h => { foreach (var col in node.TableColumns) h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text(col.HeaderText).Bold(); });
            foreach (var row in rows)
                foreach (var col in node.TableColumns)
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5)
                         .Text(resolver.ResolveCell(row, col.DataToken, ctx.Catalog));
        });
    }

    private static void RenderImage(IContainer c, ReportNode node)
    {
        // Phase 1: stub — renders a placeholder until Image source is wired (RULE E6).
        // Use EmbeddedResource via ReportAssets once asset paths are on the node.
        c.Placeholder();
    }

    private static void ApplyAlignment(TextDescriptor t, string a)
    {
        switch (a) { case "Center": t.AlignCenter(); break; case "Right": t.AlignRight(); break; case "Justify": t.Justify(); break; default: t.AlignLeft(); break; }
    }
}
```

---

## Phase 5 — Module wiring (`IModule`) + host registration

```csharp
using AMIS.Framework.Shared.Constants;            // PermissionConstants
using AMIS.Framework.Web.Modules;                 // IModule
using QuestPDF.Infrastructure;

public sealed class ReportDesignerModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        PermissionConstants.Register(ReportDesignerModuleConstants.Permissions);
        QuestPDF.Settings.License = LicenseType.Community;       // same as QuestPdfReporting

        builder.Services.AddHeroDbContext<ReportDesignerDbContext>();
        builder.Services.AddScoped<IDbInitializer, ReportDesignerDbInitializer>();
        builder.Services.AddSingleton<IReportBindingResolver, ReportBindingResolver>();
        builder.Services.AddScoped<IReportDataSourceProvider, ReportDataSourceProvider>();

        // Register each reportable source — one line per source. Adding a new source never
        // touches the engine or designer; just add the class and one DI line here (RULE D7).
        builder.Services.AddScoped<IReportableSource, AssetsReportableSource>();
        // builder.Services.AddScoped<IReportableSource, VehiclesReportableSource>();
        // builder.Services.AddScoped<IReportableSource, EmployeesReportableSource>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var versionSet = endpoints.NewApiVersionSet().HasApiVersion(new ApiVersion(1)).ReportApiVersions().Build();
        var baseArgs = new { tags = "ReportDesigner", versionSet };

        // Template management + render
        var templateGroup = endpoints.MapGroup("api/v{version:apiVersion}/report-designer/templates")
            .WithTags("ReportDesigner").WithApiVersionSet(versionSet);
        templateGroup.MapGenerateReport();
        templateGroup.MapPreviewReport();
        // templateGroup.MapCreateTemplate / MapUpdateTemplate / MapGetTemplate / MapListTemplates / MapDeleteTemplate

        // Admin data-source management (Phase 2)
        var dsGroup = endpoints.MapGroup("api/v{version:apiVersion}/report-designer/data-sources")
            .WithTags("ReportDesigner").WithApiVersionSet(versionSet);
        // dsGroup.MapCreateDataSource / MapUpdateDataSource / MapListDataSources / MapDeleteDataSource

        // Available source catalog (what the Data Source Designer can pick from)
        var catalogGroup = endpoints.MapGroup("api/v{version:apiVersion}/report-designer/sources")
            .WithTags("ReportDesigner").WithApiVersionSet(versionSet);
        // catalogGroup.MapListAvailableSources — returns all registered IReportableSource keys + fields/relations
    }
}
```

Host wiring (`AMIS.Api/Program.cs`): add `typeof(ReportDesignerModule)`,
`typeof(ReportDesignerContractsMarker)`, and a representative query to the Mediator
`Assemblies` list; add `typeof(ReportDesignerModule).Assembly` to `moduleAssemblies`;
register both projects in `AMIS.Framework.slnx` and `AMIS.Api.csproj`.

---

## Phase 6 — Blazor designer (see DETAILED RULES F)

Recursive `ReportCanvasNode.razor` per the original concept, but the drag-drop and
state bodies are the real work (roadmap Phases 3–4). All AMIS Blazor rules apply
(AMIS*/Mud components, permission-gated buttons, UTF-8 saves).

---

# DETAILED IMPLEMENTATION RULES

These are the specific rules that keep a WYSIWYG report maker from collapsing under
its own edge cases. Treat them as MUST unless marked SHOULD.

## A. Boundary & module rules
- **A1 (MUST)** New module `Modules.ReportDesigner` (Contracts + impl). Do not add persistence to `Modules.QuestPdfReporting`.
- **A2 (MUST)** The ReportDesigner reads bindable data **only** through registered `IReportableSource`s (published by the owning modules, tenant-scoped). It never references another module's DbContext, Domain, or Features directly. A source implementation may itself fetch via that module's `.Contracts`/`IMediator`.
- **A3 (MUST)** The designer's own templates persist in `ReportDesignerDbContext`; reads of *own* templates may use the DbContext directly.
- **A4 (MUST)** Statutory forms (ICS/PAR/PO/PR/DV/BUR/RSPI) are **out of scope** — they remain code/`.frx`/`.rdlc`. The designer never edits them.

## B. Permissions, tenancy, security
- **B1 (MUST)** Register permissions via `PermissionConstants.Register(...)`; gate every endpoint with `.RequirePermission(...)` and every Blazor action with `UserProfileState.Permissions.Contains(...)`.
- **B2 (MUST)** `ReportTemplate : IMustHaveTenant` → templates are tenant-isolated automatically. Never load a template across tenants, including in preview.
- **B3 (MUST)** Re-validate permission **and** re-validate the AST **on render**, server-side. Never trust a client-supplied AST blindly (preview slice included).
- **B4 (MUST)** Binding is a **data lookup, never code execution.** No expression eval, no `DataTable.Compute`, no reflection method invocation — property reads only, through the allow-list (D1).

## C. AST schema rules
- **C1 (MUST)** Containment matrix — enforce in `ReportAstValidator`:
  | Node | May contain |
  |---|---|
  | Page | Row, Column, Table, Text, Image, Spacer |
  | Row | Column (each child is a relative-width cell) |
  | Column | any node |
  | Table | **no child nodes** — rows are data-bound; only `TableColumns` |
  | Text, Image, Spacer | **leaf** — no children |
- **C2 (MUST)** All sizes stored in **points** (QuestPDF native). The canvas converts pt→px for display (`px ≈ pt × 96/72 = pt × 1.333`). Never persist px.
- **C3 (MUST)** Colors are `#RRGGBB` strings; validate on save; reject anything else.
- **C4 (MUST)** Persist `SchemaVersion`; provide an AST upgrader when bumping `CurrentAstSchemaVersion`. Old templates must still render.
- **C5 (SHOULD)** Guardrails: max depth 32, max node count 500, max template jsonb size ~256 KB. Reject on save with a clear message.
- **C6 (MUST)** Node `Id`s are stable GUIDs and unique within a template — the selection/undo/drag systems key off them.

## D. Data-source & binding rules (admin-defined model — your chosen direction)
- **D1 (MUST)** Data sources are **admin-defined at runtime** via the Data Source Designer (Phase 1b), composed from module-published `IReportableSource`s. There is **no hardcoded field list** — the bindable catalog is *derived* from the saved `ReportDataSource.Spec`. This is what makes the data source fully customizable.
- **D2 (MUST — the hard rule)** Every `IReportableSource.QueryAsync` returns **tenant-scoped, read-only** data. Tenant filtering lives inside the source implementation (in the owning module, using the tenant context); the designer can never widen it. A report cannot read another tenant's data no matter how the template is authored.
- **D3 (MUST)** The resolver binds **only** to fields present in the chosen `ReportDataSource.Spec`. A token referencing a field not in the spec → validation error on save, empty string on render. No open `GetProperty(arbitraryString)` over live entities, no expression eval.
- **D4 (MUST)** Token grammar: `{{Path}}` / `{{Path:format}}`. Formats: `date`, `datetime`, `money` (₱ + thousands + 2dp), `int`, `pct`. Unknown format → raw value, never throw. Null anywhere in a path → empty string; max path depth 4.
- **D5 (MUST)** Tables bind to one collection (a relation declared on the root source); cell tokens are **row-relative** (`{{Description}}`), resolved per row through the same derived catalog.
- **D6 (SHOULD)** Caps: max rows per query (e.g. 5,000) + a render-time budget; exceed → friendly error, not a hang. Cache compiled accessors per (source, path) — reflection per cell is too slow at scale.
- **D7 (MUST)** Adding a new bindable area = a module registering one more `IReportableSource`. The engine and designer never change. This is how "fully customizable" stays maintainable.
- **D8 (SHOULD)** Cross-source composition (Assets + Custodian + Category) is modeled through explicit `IReportableSource.Relations`, **not** arbitrary user SQL. Keeps joins safe and tenant-scoped while still feeling open to the admin.

## E. QuestPDF rendering rules
- **E1 (MUST)** Set `QuestPDF.Settings.License = LicenseType.Community;` once in `ConfigureServices` (already done by `QuestPdfReporting`; set it here too since this is a separate module init path).
- **E2 (MUST)** Never name an instance method `GeneratePdf` on an `IDocument` — call the QuestPDF **extension** `document.GeneratePdf()`. (The 1st-pass plan had an infinite-recursion shim; removed.)
- **E3 (MUST)** Wrap rendering; catch `DocumentLayoutException` (content that can't fit / infinite layout) → return a 422 with "layout doesn't fit; adjust column widths/margins," not a 500.
- **E4 (SHOULD)** Set a max-page guard (e.g. `Settings.DocumentLayoutExceptionThreshold`) to fail fast on runaway templates.
- **E5 (MUST)** Reuse `PaperSize`/`Orientation`/`Margin` parameters and the `GovernmentPaperSizes` presets (A4, LongBond, etc.) — same UX as existing reports.
- **E6 (MUST)** Logos/static images via `ReportAssets/**` embedded resources (already wired in the csproj pattern) — not file paths.
- **E7 (MUST)** `table.Header()` for repeating headers across pages; totals/footers computed in the handler/provider and passed as bound values (don't compute in the view tree).
- **E8 (SHOULD)** Keep render deterministic for golden tests — no `DateTime.Now` inside `Compose`; "printed on" is a bound field supplied by the handler.

## F. Blazor WYSIWYG designer rules
- **F1 (MUST)** **Single source of truth:** one mutable AST held in a scoped state container; the recursive canvas renders from it. Put `@key="node.Id"` on every rendered child so Blazor diffing stays stable during reorders.
- **F2 (MUST)** **Selection model:** track `SelectedNodeId`; the properties panel two-way-binds to the selected node, then calls a single `StateHasChanged`/notify. Don't pass mutable nodes through deep `[Parameter]` chains without a notify-up callback.
- **F3 (MUST)** **Drag-drop safety:** validate every drop against the containment matrix (C1) **and** reject dropping a node into its own descendant (cycle). Track the dragged node by Id in a state pointer — do **not** serialize whole nodes into `DataTransfer`.
- **F4 (SHOULD)** Provide explicit move/indent controls (move up/down, in/out) in addition to drag — they're the accessible fallback and cover Phase-3 (structured editor) before full drag lands.
- **F5 (MUST)** **Undo/redo** via a bounded snapshot stack of serialized AST (cap ~50). Debounce snapshots (e.g. on commit, not per keystroke).
- **F6 (MUST)** **Live preview is debounced** (≥500 ms idle) and uses **capped sample data** — never re-render the PDF on every keystroke. The PDF preview (via the `Preview` slice) is the **authoritative fidelity check**; the canvas is approximate.
- **F7 (MUST)** **Concurrency:** templates carry the xmin token (Phase 2). On save conflict, show "edited elsewhere; reload" — don't silently overwrite.
- **F8 (MUST)** Dirty tracking + explicit Save; warn on navigate-away with unsaved changes.
- **F9 (MUST)** Save all `.razor`/`.cs` as **UTF-8** (₱, —, … appear in templates); prefer `AMISButton`/`AMISTextField`/Mud over raw HTML controls; permission-gate every action (B1).

## G. Persistence & migration rules
- **G1 (MUST)** AST stored as one `jsonb` column with stable `JsonSerializerOptions` (`ReportJson.Options`) shared by engine + persistence + API so round-trips are byte-stable.
- **G2 (MUST)** Concurrency via `UseXminAsConcurrencyToken()` — **not** a `byte[]` RowVersion (breaks Npgsql inserts).
- **G3 (MUST)** Don't combine `ISoftDeletable` with a second named `SoftDelete` filter (EF10 `migrations add` fails).
- **G4 (MUST)** Migrations live in `Migrations.PostgreSQL` against `ReportDesignerDbContext`; schema `report_designer`.

## H. Testing rules
- **H1 (MUST)** Golden-file PDF tests: fixed template + fixed data → assert a stable hash (deterministic per E8).
- **H2 (MUST)** AST validator tests: containment violations, depth/size caps, cyclic drops all rejected.
- **H3 (MUST)** Binding tests: nested path, each format, missing field, null chain, collection rows.
- **H4 (MUST)** Security tests: token outside the catalog is rejected; cross-tenant template id returns not-found; preview re-validates permission + AST.
- **H5 (MUST)** Architecture test: `Modules.ReportDesigner` references no other module's internals (Contracts only).

## I. Decision log (fill in before coding)
- [ ] I1 — Confirmed greenfield scope: ad-hoc reports only; statutory forms not canvas-editable (RULE 0).
- [ ] I2 — Confirmed tenant-scoping enforcement point lives in each `IReportableSource` (RULE D2).
- [ ] I3 — Chosen first reportable source(s) to publish + Data Source Designer v1 scope (RULE D, Phase 1b).
- [ ] I4 — Paper sizes / orientation supported at launch (RULE E5).
- [ ] I5 — Roadmap cut-line targeted for v1 (see `AMIS_Report_Designer_Roadmap.md`; recommend Cut-line 2).

---

## Plan-to-Roadmap Cross-Reference

| Plan Part | Roadmap Phase | What gets built |
|---|---|---|
| Part 1 (AST) + Part 2 (Persistence skeleton) | **Phase 0 — Spike** | Module skeleton, jsonb round-trip, binding + pagination proof-of-concept |
| Part 1 (full AST) + Part 1b (data sources) + Part 2 (full persistence) + Part 3 (generate slice) + Part 4 (engine) + Part 5 (module wiring) | **Phase 1 — Runtime engine** | Saved JSON template → correct PDF; developer can hand-author |
| Part 1b + Part 3 (CRUD slices) + Part 2 (`ReportDataSource`) | **Phase 2 — Admin data sources + template mgmt** | Admin composes data sources, builds templates, previews, rolls out |
| Part 6 — structured Blazor canvas | **Phase 3 — Structured visual editor** | Visual assembly without drag-drop |
| Part 6 — drag-drop canvas | **Phase 4 — Full WYSIWYG** | True drag-to-place canvas |
| All hardening | **Phase 5 — Polish** | Undo/redo, versioning, advanced nodes |

---

## Key decisions & corrections baked into this plan

These are the load-bearing choices that distinguish this plan from a generic
"box-model report builder" concept — keep them in mind when implementing:

- Every namespace/type is grounded against the repo (see the conventions table above) — no "confirm-in-repo" guesses remain except `IMustHaveTenant`'s exact namespace.
- **RULE 0 — greenfield & independent.** The designer does not depend on the existing engines; the only constraints are "don't canvas-edit statutory forms" and "data stays tenant-scoped."
- **Data source is admin-defined (runtime Data Source Designer, Phase 1b).** Modules publish tenant-scoped `IReportableSource`s; the binding catalog is *derived* from what the admin selects (fully customizable **and** safe) — there is **no** hardcoded field list.
- **Module is `Modules.ReportDesigner`** (renders like `QuestPdfReporting`, but persists on its own DbContext) — not a persistence add-on to `QuestPdfReporting`.
- **Bug traps that bit the early drafts, now codified as rules:** the recursive `GeneratePdf()` shim (E2), `IAuditable`→`IAuditableEntity`, `BaseEntity`→`BaseEntity<Guid>` (protected-set Id), `NotFoundException`→`KeyNotFoundException`, the `Mediator` namespace (not MediatR), license setup, and xmin concurrency (not a `byte[]` RowVersion, G2).
- Added the full **DETAILED IMPLEMENTATION RULES** (A–I): containment matrix, points-not-px, allow-listed binding catalog, drag-drop cycle/containment safety, debounced preview, golden-file tests, and a pre-coding decision log.
- **Third-pass fixes (this revision):**
  - `AstSchema` static class defined (bare `const` was orphaned — `AstSchema.CurrentAstSchemaVersion` would not compile).
  - `ReportTemplate.DataSourceKey: string` → `DataSourceId: Guid` (FK to `ReportDataSource`); `string` was semantically wrong — the template references a saved data-source record, not a raw source key.
  - `GenerateReportHandler` now loads `ReportDataSource` first, then passes its `Spec` to the provider (previous version called `LoadAsync` with a string, which didn't match the provider signature).
  - `ReportRenderData.Rows` → `Collections: IReadOnlyDictionary<string, ...>` so multiple source collections (Assets, Vehicles, etc.) can co-exist in one render context.
  - `ReportRenderContext` type defined explicitly (was referenced in the engine but never declared).
  - `IReportDataSourceProvider` interface made explicit (was implied only by the provider class).
  - `IReportBindingResolver` signature fixed — removed undefined `IFieldCatalog`, replaced with `IReadOnlyList<CatalogEntry>` throughout.
  - `RenderTable` updated to use correctly-typed `ctx.Data` dictionary (was `IEnumerable<object>` cast which would always return empty).
  - `RenderNode` Image case added (stub via `Placeholder()`).
  - `ReportDataSourceConfiguration` added alongside `ReportTemplateConfiguration`.
  - `ReportDataSources` DbSet added to `ReportDesignerDbContext`.
  - `PreviewReport` slice shape defined (was mentioned in passing but not shown).
  - Plan-to-Roadmap cross-reference table added to end of this file.
