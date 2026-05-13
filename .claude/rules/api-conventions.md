---
paths:
  - "src/Modules/**/Features/**/*"
  - "src/Modules/**/*Endpoint*.cs"
---

# API Conventions

Rules for API endpoints in FSH.

## Endpoint Requirements

Every endpoint MUST have:

```csharp
endpoints.MapPost("/", handler)
    .WithName("{Module}_{Action}{Entity}")  // Required: GLOBALLY unique name
    .WithSummary("Description")              // Required: OpenAPI description
    .RequirePermission(Permission)           // Required: Or .AllowAnonymous()
```

## Endpoint Names Must Be Globally Unique

ASP.NET Core requires `WithName(...)` values to be unique across the **entire application** — not just within a module. If two endpoints share a name, `DataSourceDependentMatcher.CreateMatcher` throws at startup, and the cached exception is rethrown on **every** request (including `/health/ready`). Every endpoint returns 500 with a generic "An unexpected error occurred" until fixed.

### Rule

- **Prefix every endpoint name with its module** to guarantee uniqueness: `"AssetRegister_CreatePropertyItemCatalog"`, `"AssetManagement_UpdateSemiExpendableItem"`, `"Expendable_GetPhysicalCountReport"`.
- **Do NOT use `nameof(SomeCommand)` as the name** when that command type lives in a Contracts package shared across modules. Two modules consuming the same contract type will both produce the same string and collide.
- A bare `nameof(LocalCommand)` is acceptable only when the command type is `internal` to a single module and its name is unique across the codebase. If in doubt, prefix.

### Detection

Before committing endpoint changes, run:

```bash
grep -rh "\.WithName(" src/Modules src/BuildingBlocks --include="*.cs" \
  | sed -E 's/.*\.WithName\(([^)]+)\).*/\1/' \
  | sort | uniq -c | sort -rn | awk '$1 > 1'
```

Any output means a duplicate exists and the app will fail to serve any request.

## HTTP Method Mapping

| Operation | Method | Return |
|-----------|--------|--------|
| Create | `MapPost` | `TypedResults.Created(...)` |
| Read single | `MapGet` | `TypedResults.Ok(...)` |
| Read list | `MapGet` | `TypedResults.Ok(...)` |
| Update | `MapPut` | `TypedResults.Ok(...)` or `NoContent()` |
| Delete | `MapDelete` | `TypedResults.NoContent()` |

## Route Patterns

```
/api/v1/{module}/{entities}           # Collection
/api/v1/{module}/{entities}/{id}      # Single item
/api/v1/{module}/{entities}/{id}/sub  # Sub-resource
```

## Response Types

Always use `TypedResults`:
- `TypedResults.Ok(data)`
- `TypedResults.Created($"/path/{id}", data)`
- `TypedResults.NoContent()`
- `TypedResults.NotFound()`
- `TypedResults.BadRequest(errors)`

Never return raw objects or use `Results.Ok()`.

## Permission Format

```csharp
.RequirePermission({Module}Permissions.{Entity}.{Action})
```

Actions: `View`, `Create`, `Update`, `Delete`

## Query Parameters

Use `[AsParameters]` for complex queries:

```csharp
endpoints.MapGet("/", async ([AsParameters] GetProductsQuery query, ...) => ...)
```
