# Blazor API Client Generation Guide

> **Last Updated:** March 8, 2026  
> **Framework:** AMIS .NET Starter Kit (Modular Monolith with CQRS + DDD)  
> **Tool:** NSwag 14.6.3 (Local dotnet tool)

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Quick Start: Regenerating Clients](#quick-start-regenerating-clients)
3. [Backend Setup (OpenAPI Exposure)](#backend-setup-openapi-exposure)
4. [NSwag Configuration](#nswag-configuration)
5. [Generated Code Structure](#generated-code-structure)
6. [Dependency Injection Setup](#dependency-injection-setup)
7. [Feature-Specific Wrapper Clients](#feature-specific-wrapper-clients)
8. [Workflow: Adding a New Endpoint](#workflow-adding-a-new-endpoint)
9. [Authorization & Token Injection](#authorization--token-injection)
10. [Drift Detection & Validation](#drift-detection--validation)
11. [Troubleshooting](#troubleshooting)
12. [Key Design Decisions](#key-design-decisions)

---

## Architecture Overview

### End-to-End Flow

```
┌────────────────────────────────────────────────────────────────────┐
│ BACKEND API (Playground.Api on port 7030)                         │
├────────────────────────────────────────────────────────────────────┤
│ • Runs on: https://localhost:7030                                 │
│ • Exposes OpenAPI spec at: /openapi/v1.json                       │
│ • Interactive docs at: /scalar                                    │
│ • Framework: BuildingBlocks/Web (AddHeroOpenApi + UseHeroOpenApi) │
└────────────────────┬─────────────────────────────────────────────┘
                     │
                     ↓ (fetches spec during generation)
                     
┌────────────────────────────────────────────────────────────────────┐
│ NSWAG CODE GENERATION                                             │
├────────────────────────────────────────────────────────────────────┤
│ • Tool: nswag.consolecore v14.6.3 (local dotnet tool)             │
│ • Config: scripts/openapi/nswag-playground.json                   │
│ • Script: scripts/openapi/generate-api-clients.ps1                │
│ • Runs: dotnet nswag run <config> /variables:SpecUrl=<url>        │
│                                                                    │
│ Generates:                                                         │
│ • Client classes (one per API resource [IExpendableClient, etc])  │
│ • DTO records (CreatePurchaseOrderCommand, etc.)                  │
│ • HTTP method wrappers (async, with serialization/deserialization)│
│ • Bearer token support                                            │
│ • Namespace: AMIS.Playground.Blazor.ApiClient                      │
│ • Output: ApiClient/Generated.cs (~1500 lines, single file)       │
└────────────────────┬─────────────────────────────────────────────┘
                     │
                     ↓ (includes in project)
                     
┌────────────────────────────────────────────────────────────────────┐
│ DEPENDENCY INJECTION (ApiClientRegistration.cs)                   │
├────────────────────────────────────────────────────────────────────┤
│ • Registers generated clients as transient services               │
│ • Creates HttpClient with BaseAddress and auth handler            │
│ • Wraps with feature-specific adapters for cleaner UI APIs        │
└────────────────────┬─────────────────────────────────────────────┘
                     │
                     ↓ (injects into pages)
                     
┌────────────────────────────────────────────────────────────────────┐
│ BLAZOR PAGES & COMPONENTS                                         │
├────────────────────────────────────────────────────────────────────┤
│ @inject IExpendablePurchasesClient PurchasesClient                │
│                                                                    │
│ // Type-safe, IntelliSense-enabled API calls:                     │
│ await PurchasesClient.CreateAsync(command);                       │
│ var purchases = await PurchasesClient.SearchAsync(...);           │
└────────────────────────────────────────────────────────────────────┘
```

### Key Principle: Decoupled Architecture

- **Backend:** Exposes OpenAPI spec (contracts, not implementation)
- **Blazor UI:** Generates clients from spec (never imports backend modules)
- **Communication:** Type-safe HTTP clients generated from spec
- **Result:** Zero coupling between frontend and backend code structures

---

## Quick Start: Regenerating Clients

### Prerequisites
- .NET SDK (10.0+)
- Backend API running locally on `https://localhost:7030`
- Local dotnet tools restored

### Step-by-Step

```powershell
# 1. Ensure local tools are installed
dotnet tool restore

# 2. Generate clients from running API
./scripts/openapi/generate-api-clients.ps1 -SpecUrl "https://localhost:7030/openapi/v1.json"

# 3. Verify no drift (optional, for CI validation)
./scripts/openapi/check-openapi-drift.ps1

# 4. Rebuild Blazor project to use new clients
dotnet build src/Playground/Playground.Blazor/Playground.Blazor.csproj

# 5. Commit changes
git add src/Playground/Playground.Blazor/ApiClient/Generated.cs
git commit -m "chore: regenerated API clients from updated spec"
```

### What Gets Generated
- **File:** `src/Playground/Playground.Blazor/ApiClient/Generated.cs`
- **Size:** ~1500 lines (single file, multiple client classes)
- **Namespace:** `AMIS.Playground.Blazor.ApiClient`
- **Types:** Client interfaces/classes, DTO records, API exceptions

---

## Backend Setup (OpenAPI Exposure)

### Configuration in Playground.Api

**Program.cs:**
```csharp
// Add OpenAPI service
builder.Services.AddHeroOpenApi(builder.Configuration);

// ... other setup ...

var app = builder.Build();

// Use OpenAPI endpoint mapping + Scalar UI
app.UseHeroOpenApi("/openapi/{documentName}.json");
```

**appsettings.json:**
```json
{
  "OpenApi": {
    "Title": "AMIS Playground API",
    "Version": "v1",
    "Description": "API for...",
    "Contact": {
      "Name": "Support",
      "Email": "support@..."
    },
    "License": {
      "Name": "MIT"
    }
  }
}
```

### What Gets Exposed

| Endpoint | Purpose |
|----------|---------|
| `GET /openapi/v1.json` | OpenAPI 3.0 spec (consumed by NSwag) |
| `GET /scalar` | Interactive API documentation (Scalar UI) |

### Framework Integration

**BuildingBlocks/Web/OpenApi/Extensions.cs:**
- `AddHeroOpenApi()` - Registers Microsoft.AspNetCore.OpenApi + transformers
- `UseHeroOpenApi()` - Maps endpoints + Scalar UI
- `BearerSecuritySchemeTransformer` - Adds Bearer auth to spec

---

## NSwag Configuration

### Configuration File Location
`scripts/openapi/nswag-playground.json`

### Key Settings

```json
{
  "runtime": "Net100",
  "codeGenerators": {
    "openApiToCSharpClient": {
      "generateClientClasses": true,
      "generateClientInterfaces": true,
      "generateResponseClasses": true,
      "injectHttpClient": true,
      "httpClientType": "System.Net.Http.HttpClient",
      
      // Output Configuration
      "namespace": "AMIS.Playground.Blazor.ApiClient",
      "output": "../../src/Playground/Playground.Blazor/ApiClient/Generated.cs",
      
      // Client Organization
      "className": "{controller}Client",
      "operationGenerationMode": "MultipleClientsFromPathSegments",
      
      // Serialization
      "jsonLibrary": "SystemTextJson",
      "dateType": "System.DateTimeOffset",
      "timeType": "System.TimeSpan",
      
      // Type Generation
      "generateDtoTypes": true,
      "classStyle": "Poco",
      "generateDataAnnotations": true,
      
      // Methods & Features
      "generateSyncMethods": false,
      "useBaseUrl": false,
      "generateExceptionClasses": true
    }
  }
}
```

### Important Settings Explained

| Setting | Value | Why |
|---------|-------|-----|
| `injectHttpClient` | `true` | HttpClient injected via DI (not hardcoded) |
| `useBaseUrl` | `false` | BaseAddress set in DI, not in generated code |
| `operationGenerationMode` | `MultipleClientsFromPathSegments` | Client per resource (`/api/v1/expendable/*` → `ExpendableClient`) |
| `generateSyncMethods` | `false` | Async-only (Blazor uses async) |
| `dateType` | `System.DateTimeOffset` | Better than DateTime for APIs |
| `jsonLibrary` | `SystemTextJson` | Default .NET serializer (not Newtonsoft) |

---

## Generated Code Structure

### Example: Client Class

**From OpenAPI:** `POST /api/v1/expendable/purchases`

**Generated Code:**
```csharp
[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.6.3.0")]
public interface IExpendableClient
{
    Task PurchasesPostAsync(CreatePurchaseOrderCommand body, 
        System.Threading.CancellationToken cancellationToken = default);
}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.6.3.0")]
public partial class ExpendableClient : IExpendableClient
{
    private readonly System.Net.Http.HttpClient _httpClient;

    public ExpendableClient(System.Net.Http.HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>Create a purchase order</summary>
    public async Task PurchasesPostAsync(CreatePurchaseOrderCommand body, 
        System.Threading.CancellationToken cancellationToken = default)
    {
        var urlBuilder_ = new System.Text.StringBuilder();
        urlBuilder_.Append("api/v1/expendable/purchases");

        var request_ = new System.Net.Http.HttpRequestMessage(
            System.Net.Http.HttpMethod.Post, urlBuilder_.ToString());
        
        var content_ = new System.Net.Http.ByteArrayContent(
            System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(body));
        content_.Headers.ContentType = 
            System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
        request_.Content = content_;

        var response_ = await _httpClient.SendAsync(request_, 
            System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        if (response_.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var objectResponse_ = await ReadObjectResponseAsync<Unit>(response_, 
                System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<string>>(), 
                cancellationToken);
            return objectResponse_.Object;
        }
        else
        {
            throw new ApiException("Response was not OK", (int)response_.StatusCode, ...);
        }
    }
}
```

### Example: DTO Record

**Generated Code:**
```csharp
[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0")]
public partial class CreatePurchaseOrderCommand
{
    [System.Text.Json.Serialization.JsonPropertyName("supplierId")]
    [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
    public string SupplierId { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("supplierName")]
    [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
    public string SupplierName { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("warehouseLocationId")]
    [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
    public System.Guid WarehouseLocationId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("warehouseLocationName")]
    public string WarehouseLocationName { get; set; }
}
```

---

## Dependency Injection Setup

### Main Service Configuration

**File:** `Services/Api/ApiClientRegistration.cs`

```csharp
public static IServiceCollection AddApiClients(
    this IServiceCollection services,
    IConfiguration configuration,
    IWebHostEnvironment environment)
{
    var apiBaseUrl = configuration["Api:BaseUrl"] 
        ?? throw new InvalidOperationException("Api:BaseUrl is missing");

    var apiUri = new Uri(apiBaseUrl);

    // HttpClientHandler with certificate validation override for dev
    static HttpClientHandler CreateHandler(Uri apiUri, IWebHostEnvironment env)
    {
        var handler = new HttpClientHandler();
        if (env.IsDevelopment() && (apiUri.Host == "localhost" || apiUri.Host == "127.0.0.1"))
        {
            handler.ServerCertificateCustomValidationCallback = 
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
        return handler;
    }

    // Main HttpClient with auth handler
    services.AddScoped(sp =>
    {
        var handler = sp.GetRequiredService<AuthorizationHeaderHandler>();
        handler.InnerHandler = CreateHandler(apiUri, environment);
        return new HttpClient(handler) { BaseAddress = apiUri };
    });

    // Token client (separate to avoid circular dependency)
    services.AddHttpClient("TokenClient", client =>
    {
        client.BaseAddress = apiUri;
    })
    .ConfigurePrimaryHttpMessageHandler(() => CreateHandler(apiUri, environment));

    // Register generated clients as transient
    services.AddTransient<ITokenClient>(sp =>
    {
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("TokenClient");
        return new TokenClient(client);
    });

    services.AddTransient<IExpendableClient>(sp =>
        new ExpendableClient(sp.GetRequiredService<HttpClient>()));

    services.AddTransient<IPurchasesClient>(sp =>
        new PurchasesClient(sp.GetRequiredService<HttpClient>()));

    // ... more client registrations ...
}
```

### Configuration in Program.cs

```csharp
// Program.cs (Blazor)
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] 
    ?? throw new InvalidOperationException("Api:BaseUrl is missing");

// ... auth setup ...

// Configure HTTP client
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthorizationHeaderHandler>();
    var apiUri = new Uri(apiBaseUrl);
    
    // Allow self-signed certs in dev
    if (builder.Environment.IsDevelopment() && 
        (apiUri.Host == "localhost" || apiUri.Host == "127.0.0.1"))
    {
        handler.InnerHandler = new HttpClientHandler 
        { 
            ServerCertificateCustomValidationCallback = 
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator 
        };
    }
    
    return new HttpClient(handler) { BaseAddress = apiUri };
});

// Add all API clients
builder.Services.AddApiClients(builder.Configuration, builder.Environment);
```

### Configuration in appsettings

```json
{
  "Api": {
    "BaseUrl": "https://localhost:7030"
  }
}
```

---

## Feature-Specific Wrapper Clients

### Why Wrapper Clients?

Generated clients are named after OpenAPI resources (`expend ableClient`, `purchasesClient`). For cleaner UI code, we wrap them in domain-specific interfaces.

**Example: Purchases Feature**

### Before (Using Generated Client Directly)

```csharp
@inject IPurchasesClient PurchasesClient

// Confusing: which purchase methods are available?
// Must know implementation details
```

### After (Using Feature Wrapper)

```csharp
@inject IExpendablePurchasesClient PurchasesClient

// Clear: this client handles purchase operations for the Expendable module
// Implementation details hidden
```

### Creating a Wrapper

**1. Define Interface** (`Services/Api/Expendable/IExpendablePurchasesClient.cs`)

```csharp
internal interface IExpendablePurchasesClient
{
    Task CreateAsync(CreatePurchaseOrderCommand command, CancellationToken ct = default);
    
    Task<PagedResponse<PurchaseDto>> SearchAsync(
        string? poNumber = null,
        string? status = null,
        int? pageNumber = null,
        int? pageSize = null,
        CancellationToken ct = default);

    Task<PurchaseDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task SubmitAsync(Guid id, CancellationToken ct = default);
    Task ApproveAsync(Guid id, CancellationToken ct = default);

    // Domain DTOs (can differ from generated DTOs)
    record PurchaseDto(
        Guid Id,
        string PONumber,
        string Status,
        string SupplierName,
        string WarehouseLocationName,
        DateTime CreatedDate,
        int LineItemCount);
}
```

**2. Implement Wrapper** (`Services/Api/Expendable/ExpendablePurchasesClient.cs`)

```csharp
internal sealed class ExpendablePurchasesClient : IExpendablePurchasesClient
{
    private readonly IExpendableClient _expendableClient;
    private readonly IPurchasesClient _purchasesClient;

    public ExpendablePurchasesClient(
        IExpendableClient expendableClient, 
        IPurchasesClient purchasesClient)
    {
        _expendableClient = expendableClient;
        _purchasesClient = purchasesClient;
    }

    public Task CreateAsync(CreatePurchaseOrderCommand command, CancellationToken ct = default) =>
        _expendableClient.PurchasesPostAsync(command, ct);

    public async Task<PagedResponse<IExpendablePurchasesClient.PurchaseDto>> SearchAsync(
        string? poNumber = null, string? status = null, int? pageNumber = null, 
        int? pageSize = null, CancellationToken ct = default)
    {
        // Placeholder: integrate with actual API endpoint
        await Task.CompletedTask;
        return new PagedResponse<IExpendablePurchasesClient.PurchaseDto>
        {
            Items = new List<IExpendablePurchasesClient.PurchaseDto>(),
            TotalCount = 0
        };
    }

    public Task SubmitAsync(Guid id, CancellationToken ct = default) =>
        _purchasesClient.SubmitAsync(id, ct);

    public Task ApproveAsync(Guid id, CancellationToken ct = default) =>
        _purchasesClient.ApproveAsync(id, ct);
}
```

**3. Register in DI** (`ApiClientRegistration.cs`)

```csharp
services.AddTransient<IExpendablePurchasesClient, ExpendablePurchasesClient>();
```

**4. Use in Pages** (`ExpendablePurchasesPage.razor`)

```csharp
@inject IExpendablePurchasesClient PurchasesClient

// Type-safe, domain-specific
await PurchasesClient.CreateAsync(command);
```

---

## Workflow: Adding a New Endpoint

### Backend Changes

**1. Create Command/Query** (`Modules.Expendable.Contracts/v1/Purchases/PurchaseContracts.cs`)

```csharp
public record UpdatePurchaseCommand(
    Guid Id,
    string Status,
    string? Notes = null) : ICommand<Unit>;
```

**2. Create Handler** (`Modules/Expendable/Features/v1/Purchases/UpdatePurchaseHandler.cs`)

```csharp
public sealed class UpdatePurchaseHandler : ICommandHandler<UpdatePurchaseCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdatePurchaseCommand cmd, CancellationToken ct)
    {
        // Business logic...
        return Unit.Value;
    }
}
```

**3. Create Endpoint** (`Modules/Expendable/Features/v1/Purchases/UpdatePurchaseEndpoint.cs`)

```csharp
public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
    endpoints.MapPut("/purchases/{id}", async (Guid id, UpdatePurchaseCommand cmd, 
        IMediator mediator, CancellationToken ct) =>
    {
        await mediator.Send(new UpdatePurchaseCommand(id, cmd.Status, cmd.Notes), ct);
        return TypedResults.NoContent();
    })
    .WithName(nameof(UpdatePurchaseCommand))
    .WithSummary("Update purchase status")
    .RequirePermission(CatalogPermissions.Purchases.Update);
```

### Blazor Changes

**4. Regenerate Clients**

```powershell
./scripts/openapi/generate-api-clients.ps1 -SpecUrl "https://localhost:7030/openapi/v1.json"
```

**5. Update Wrapper Client** (`Services/Api/Expendable/IExpendablePurchasesClient.cs`)

```csharp
internal interface IExpendablePurchasesClient
{
    // ... existing methods ...
    
    Task UpdateAsync(Guid id, string status, string? notes = null, CancellationToken ct = default);
}

internal sealed class ExpendablePurchasesClient : IExpendablePurchasesClient
{
    // ... existing methods ...
    
    public Task UpdateAsync(Guid id, string status, string? notes = null, CancellationToken ct = default) =>
        _purchasesClient.PurchasesPutAsync(id, new UpdatePurchaseCommand(id, status, notes), ct);
}
```

**6. Use in Page** (`ExpendablePurchasesPage.razor`)

```csharp
private async Task UpdateAsync(Guid purchaseId, string newStatus)
{
    await PurchasesClient.UpdateAsync(purchaseId, newStatus);
}
```

---

## Authorization & Token Injection

### How Bearer Tokens Are Added

**1. AuthorizationHeaderHandler** (`Services/Api/AuthorizationHeaderHandler.cs`)

```csharp
public class AuthorizationHeaderHandler : DelegatingHandler
{
    private readonly ICircuitTokenCache _tokenCache;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var token = await _tokenCache.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
```

**2. Token Refresh** (`Services/Api/ITokenRefreshService.cs`)

- Intercepts 401 (Unauthorized) responses
- Calls token endpoint with refresh token
- Retries original request with new access token

**3. Circuit-Scoped Cache** (`ICircuitTokenCache.cs`)

- Stores tokens per Blazor circuit (session)
- Clears on logout or token expiration

### Configuration

```csharp
// Program.cs
builder.Services.AddScoped<AuthorizationHeaderHandler>();
builder.Services.AddScoped<ITokenRefreshService, TokenRefreshService>();
builder.Services.AddScoped<ICircuitTokenCache, CircuitTokenCache>();

// Wired automatically to all HttpClient calls
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthorizationHeaderHandler>();
    return new HttpClient(handler) { BaseAddress = apiUri };
});
```

---

## Drift Detection & Validation

### Purpose

Ensure that generated clients (`Generated.cs`) stay in sync with the backend OpenAPI spec. If the spec changes but clients aren't regenerated, the Blazor UI will have stale method signatures.

### Manual Drift Check

```powershell
./scripts/openapi/check-openapi-drift.ps1 -SpecUrl "https://localhost:7030/openapi/v1.json"

# Success: "No drift detected." (exit code 0)
# Failure: Shows diff of Generated.cs changes (exit code 1)
```

### How It Works

```powershell
# 1. Regenerate clients from current spec
./scripts/openapi/generate-api-clients.ps1 -SpecUrl "<url>"

# 2. Check if Generated.cs changed
git diff --exit-code -- src/Playground/Playground.Blazor/ApiClient/Generated.cs

# 3. If changed: exit 1 (failure) → indicates spec was updated without regenerating
```

### CI Integration

Add to your CI pipeline (e.g., `.github/workflows/ci.yml`):

```yaml
- name: Check OpenAPI Drift
  run: ./scripts/openapi/check-openapi-drift.ps1 -SpecUrl "${{ secrets.API_SPEC_URL }}"
```

**Effect:** PRs fail if API changes without regenerating clients.

---

## Troubleshooting

### Issue: "dotnet nswag command not found"

**Cause:** Local tools not restored

**Solution:**
```powershell
dotnet tool restore
```

---

### Issue: "Cannot reach /openapi/v1.json: Connection refused"

**Cause:** Backend API not running

**Solution:**
```powershell
# Terminal 1: Start API
dotnet run --project src/Playground/Playground.Api

# Terminal 2: Generate clients
./scripts/openapi/generate-api-clients.ps1
```

---

### Issue: Generated clients don't have new methods

**Cause:** Old `Generated.cs` still in use (build not refreshed)

**Solution:**
```powershell
# Regenerate
./scripts/openapi/generate-api-clients.ps1

# Rebuild Blazor
dotnet clean src/Playground/Playground.Blazor
dotnet build src/Playground/Playground.Blazor
```

---

### Issue: "Bearer token not in requests"

**Cause:** `AuthorizationHeaderHandler` not injected

**Solution:** Ensure in `Program.cs`:
```csharp
services.AddScoped<AuthorizationHeaderHandler>();
services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthorizationHeaderHandler>();
    return new HttpClient(handler) { BaseAddress = apiUri };
});
```

---

### Issue: Self-signed certificate validation errors

**Cause:** Development cert not trusted in Blazor

**Solution:** Already handled in code (see `ApiClientRegistration.cs`):
```csharp
if (environment.IsDevelopment() && isLocalhost)
{
    handler.ServerCertificateCustomValidationCallback = 
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
}
```

**Only for development!** Never use in production.

---

### Issue: Circular dependency error in DI

**Cause:** `TokenRefreshService` → `TokenClient` → `HttpClient` → `AuthorizationHeaderHandler` → `TokenRefreshService`

**Solution:** Already handled with separate "TokenClient" named HttpClient:
```csharp
services.AddHttpClient("TokenClient", ...)  // No AuthorizationHeaderHandler
services.AddTransient<ITokenClient>(sp =>
    new TokenClient(factory.CreateClient("TokenClient")));
```

---

## Key Design Decisions

### 1. **Single Generated File (`Generated.cs`)**

✅ **Pro:** One source of truth, easy to version control  
❌ **Con:** Large file (1500+ lines)  
**Decision:** Keep as single file for simplicity

---

### 2. **Feature-Specific Wrapper Clients**

✅ **Pro:** Clean separation of concerns, domain-specific APIs  
✅ **Pro:** Implementation can evolve independently  
❌ **Con:** Extra layer (but minimal overhead)  
**Decision:** Use wrappers for UI-facing clients, keep generated clients internal

---

### 3. **Bearer Token in DI Handler (not Generated)**

✅ **Pro:** Centralized auth logic, easy to change strategies  
✅ **Pro:** Generated code stays focused on HTTP mechanics  
✅ **Pro:** No circular dependencies  
❌ **Con:** Requires manual DI setup  
**Decision:** Implement in `AuthorizationHeaderHandler`, not in generated code

---

### 4. **No Base URLs in Generated Code**

✅ **Pro:** Can change API endpoint at runtime (config)  
✅ **Pro:** Easy to test against different environments  
❌ **Con:** Requires manual `HttpClient.BaseAddress` setup  
**Decision:** Set `useBaseUrl: false` in NSwag config

---

### 5. **Async-Only Methods**

✅ **Pro:** Blazor uses async/await exclusively  
✅ **Pro:** No blocking calls (will deadlock on WebAssembly)  
❌ **Con:** Can't use from sync contexts  
**Decision:** Generate only async methods (`generateSyncMethods: false`)

---

### 6. **System.Text.Json (not Newtonsoft)**

✅ **Pro:** Default .NET serializer (smaller bundle)  
✅ **Pro:** Better performance  
✅ **Pro:** Native to .NET 8+  
❌ **Con:** Different attribute names than Newtonsoft  
**Decision:** Use `SystemTextJson` exclusively

---

## Best Practices Checklist

### When Adding a New Feature

- [ ] Create command/query in backend contracts
- [ ] Create handler + endpoint in module
- [ ] Ensure endpoint has route, HTTP method, permission
- [ ] Start backend API
- [ ] Run: `./scripts/openapi/generate-api-clients.ps1`
- [ ] Review `Generated.cs` diff (should only show new methods)
- [ ] Create/update wrapper client interface
- [ ] Implement wrapper (delegate to generated)
- [ ] Register in `ApiClientRegistration.cs`
- [ ] Inject into Blazor page
- [ ] Build & test
- [ ] Commit both backend changes AND `Generated.cs`

### When Debugging

- [ ] Check: Is backend running on `https://localhost:7030`?
- [ ] Check: Is `AuthorizationHeaderHandler` registered in DI?
- [ ] Check: Is token non-null in `CircuitTokenCache`?
- [ ] Check: Is `BaseAddress` set on `HttpClient`?
- [ ] See Network tab in browser DevTools for actual HTTP requests

### When Deploying

- [ ] Regenerate clients with production API URL
- [ ] Remove self-signed cert validation callback (not in production code)
- [ ] Ensure token refresh endpoints are reachable
- [ ] Test auth flow (login → get token → make API call)
- [ ] Verify Bearer header is in all requests

---

## References

| Resource | Location |
|----------|----------|
| Generation Script | `scripts/openapi/generate-api-clients.ps1` |
| NSwag Config | `scripts/openapi/nswag-playground.json` |
| Drift Check | `scripts/openapi/check-openapi-drift.ps1` |
| DI Setup | `src/Playground/Playground.Blazor/Services/Api/ApiClientRegistration.cs` |
| Auth Handler | `src/Playground/Playground.Blazor/Services/Api/AuthorizationHeaderHandler.cs` |
| Generated Clients | `src/Playground/Playground.Blazor/ApiClient/Generated.cs` |
| OpenAPI Backend Setup | `src/BuildingBlocks/Web/OpenApi/Extensions.cs` |
| Wrapper Clients | `src/Playground/Playground.Blazor/Services/Api/Expendable/` |

---

## Related Documentation

- **[ARCHITECTURE_ANALYSIS.md](ARCHITECTURE_ANALYSIS.md)** — System design & DDD layering
- **[AMIS Mediator Reference](CLAUDE.md#the-pattern)** — Commands, queries, handlers
- **[API Conventions](c:\AMIS101\.claude\rules\api-conventions.md)** — Endpoint design patterns

---

**Last Updated:** March 8, 2026  
**Maintained by:** Architecture Team  
**Questions?** Refer to troubleshooting section or check AMIS Discord/GitHub issues

