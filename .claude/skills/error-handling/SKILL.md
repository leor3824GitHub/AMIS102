---
name: error-handling
description: Exception types, HTTP status mappings, and where to throw vs. handle errors in FSH handlers and services. Reference when implementing error cases in features.
user-invocable: false
---

# Error Handling

FSH uses typed exceptions from `BuildingBlocks/Core/Exceptions`. They automatically map to HTTP responses — **never catch and re-wrap them** in handlers.

## Exception Types

| Exception                | HTTP Status      | When to throw                                              |
| ------------------------ | ---------------- | ---------------------------------------------------------- |
| `NotFoundException`      | 404 Not Found    | Entity not found by ID                                     |
| `ForbiddenException`     | 403 Forbidden    | Authenticated but not authorized for the specific resource |
| `UnauthorizedException`  | 401 Unauthorized | Not authenticated                                          |
| `CustomException` (base) | 500 (default)    | Custom domain errors with explicit status code             |

All inherit from `CustomException` which carries `HttpStatusCode StatusCode`.

## Namespace

```csharp
using FSH.Framework.Core.Exceptions;
```

## Usage Patterns

### Not Found

```csharp
var entity = await repository.GetByIdAsync(id, ct)
    ?? throw new NotFoundException($"{nameof(Product)} {id} not found.");
```

Or with a specification:

```csharp
var entity = await repository.FirstOrDefaultAsync(new ProductByIdSpec(id), ct)
    ?? throw new NotFoundException($"Product {id} not found.");
```

### Forbidden (resource-level authorization)

```csharp
if (entity.TenantId != currentUser.TenantId)
    throw new ForbiddenException("Access to this resource is not allowed.");
```

Use this for row-level checks **after** loading the entity. Endpoint-level auth uses `.RequirePermission()` instead.

### Unauthorized

```csharp
if (!currentUser.IsAuthenticated)
    throw new UnauthorizedException();
```

Usually handled by middleware; only throw explicitly in services that bypass the auth pipeline.

### Custom domain errors

Extend `CustomException` for domain-specific error types:

```csharp
public sealed class DuplicateItemException(string name)
    : CustomException($"An item named '{name}' already exists.", [], HttpStatusCode.Conflict);
```

Then throw like any other:

```csharp
if (await repository.AnyAsync(new ProductByNameSpec(command.Name), ct))
    throw new DuplicateItemException(command.Name);
```

## Where to Handle

| Layer           | Rule                                                                                                        |
| --------------- | ----------------------------------------------------------------------------------------------------------- |
| Handlers        | **Throw** typed exceptions. Never catch them.                                                               |
| Endpoints       | **Do not catch**. The framework middleware converts exceptions to ProblemDetails.                           |
| Domain entities | Throw `ArgumentException` / `ArgumentOutOfRangeException` for invariant violations (not custom exceptions). |
| Validators      | Use FluentValidation rules. Return validation errors before the handler runs.                               |

## Validation vs. Exception

| Scenario                                  | Use                                 |
| ----------------------------------------- | ----------------------------------- |
| Input format/range errors                 | FluentValidation in `*Validator.cs` |
| Business rule violations on existing data | `CustomException` in handler        |
| Missing resource                          | `NotFoundException` in handler      |
| Permission check on specific row          | `ForbiddenException` in handler     |

## Key Rules

1. **Never swallow exceptions** — let them propagate to the global handler
2. **Validators run first** — invalid input never reaches a handler
3. **`NotFoundException` in handlers** — not endpoints; endpoints return the handler result
4. **No `try/catch` in handlers** unless you are translating a third-party exception into a typed FSH exception
