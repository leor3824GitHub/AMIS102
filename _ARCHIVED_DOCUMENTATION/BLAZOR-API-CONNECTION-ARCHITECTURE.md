# Blazor Client to API Connection Architecture

## Overview

The Blazor Server application (`Playground.Blazor`) connects to the .NET API (`Playground.Api`) through a sophisticated, production-ready architecture that implements:

- **Backend-for-Frontend (BFF) Pattern** - Simple authentication layer between Blazor and API
- **JWT Token Management** - Automatic token refresh with proactive expiration detection
- **Cookie-Based Authentication** - Secure server-side session management via ASP.NET Core cookies
- **Delegating HTTP Handler** - Automatic authorization header injection and token lifecycle management
- **Circuit-Scoped Token Caching** - Handles Blazor Server's circuit-isolation model
- **Multi-Tenancy Support** - Tenant awareness throughout the authentication flow

---

## 1. Architecture Layers

### Layer 1: Blazor UI (Interactive Components)
- **Location:** `src/Playground/Playground.Blazor`
- **Technology:** Blazor Server Rendering (SSR) with Interactive Razor Components
- **Protocol:** HTTP/HTTPS to local API service
- **Primary Responsibility:** User interface, client-side business logic, API client orchestration

### Layer 2: Backend-for-Frontend (BFF)
- **Location:** Same Blazor application, mapped at `/bff/auth/*`
- **Endpoints:**
  - `POST /bff/auth/login` - Handles login form submission, calls API, sets auth cookie
  - `POST /bff/auth/logout` - Clears cookies
  - `GET /auth/logout` - Browser redirect-safe logout
- **Purpose:** Acts as a thin authentication gateway between Blazor components and the identity API

### Layer 3: HTTP Client Abstraction
- **Location:** `src/Playground/Playground.Blazor/Services/Api/`
- **Key Components:**
  - `AuthorizationHeaderHandler` - DelegatingHandler that injects JWT tokens into API requests
  - `TokenRefreshService` - Manages JWT refresh token flow
  - `CircuitTokenCache` - Stores refreshed tokens per Blazor circuit
  - Generated API clients - NSwag-generated typed clients for each API module
- **Purpose:** Standardize and secure all HTTP communication with the backend API

### Layer 4: API Server
- **Location:** `src/Playground/Playground.Api`
- **Technology:** ASP.NET Core Web API
- **Modules:** Identity, Multitenancy, Auditing, Expendable
- **Authentication:** JWT Bearer tokens + Cookie fallback
- **Primary Responsibility:** Business logic, data persistence, API contracts

---

## 2. Connection Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                    BLAZOR BROWSER                                   │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Razor Components                                           │   │
│  │  @inject IIdentityClient                                   │   │
│  │  @inject IProductsClient                                   │   │
│  │  await identityClient.RegisterAsync(model)                 │   │
│  └─────────────────────────────────────────────────────────────┘   │
└──────────────────┬──────────────────────────────────────────────────┘
                   │ HTTP/HTTPS
                   │
┌──────────────────▼──────────────────────────────────────────────────┐
│              BLAZOR SERVER (d:\VB\AMIS101\Playground.Blazor)        │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  BFF AUTH ENDPOINTS (/bff/auth/*)                          │   │
│  │  - SimpleBffAuth.cs                                        │   │
│  │  - POST /bff/auth/login (-> API token endpoint)           │   │
│  │  - POST /bff/auth/logout (clears cookies)                 │   │
│  │  - GET /auth/logout (redirect-safe)                       │   │
│  └─────────────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  HTTP CLIENT PIPELINE                                       │   │
│  │  1. AuthorizationHeaderHandler (adds JWT from cookie)      │   │
│  │     ↓                                                       │   │
│  │  2. TokenRefreshService (refreshes expired tokens)         │   │
│  │     ↓                                                       │   │
│  │  3. CircuitTokenCache (stores refreshed tokens)            │   │
│  │     ↓                                                       │   │
│  │  4. HttpClientHandler (with cert validation for localhost) │   │
│  │     ↓                                                       │   │
│  │  5. Generated API Clients (IIdentityClient, etc)           │   │
│  └─────────────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  AUTHENTICATION STATE MANAGEMENT                            │   │
│  │  - CookieAuthenticationStateProvider (reads HttpContext)   │   │
│  │  - IAuthStateNotifier (signals session expiration)         │   │
│  │  - IUserProfileState (syncs user data)                     │   │
│  └─────────────────────────────────────────────────────────────┘   │
└──────────────────┬──────────────────────────────────────────────────┘
   HTTPS/TLS 1.2+  │ Authorization: Bearer <JWT_TOKEN>
                   │
┌──────────────────▼──────────────────────────────────────────────────┐
│          ASP.NET CORE API SERVER (https://localhost:7030)          │
│                 src/Playground/Playground.Api                       │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  IDENTITY MODULE                                            │   │
│  │  - POST /api/v1/auth/tokens - Generate tokens              │   │
│  │  - POST /api/v1/auth/refresh - Refresh tokens              │   │
│  │  - Other Identity operations (users, roles, etc)           │   │
│  └─────────────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  MULTITENANCY MODULE                                        │   │
│  │  - Tenant isolation per request                            │   │
│  │  - Multi-database routing                                  │   │
│  └─────────────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  EXPENDABLE MODULE (POS/Inventory)                          │   │
│  │  - Products, Purchases, Supply Requests                    │   │
│  │  - Cart, Warehouse, Inventory                              │   │
│  └─────────────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  PERSISTENCE LAYER                                          │   │
│  │  - PostgreSQL (primary)                                    │   │
│  │  - Per-tenant databases (Finbuckle MultiTenant)            │   │
│  │  - Audit logging                                           │   │
│  └─────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 3. Configuration & Setup

### 3.1 Blazor Program.cs Configuration

```csharp
// API base URL configuration
var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
                 ?? throw new InvalidOperationException("Api:BaseUrl configuration is missing.");

// Authentication setup (Cookie-based)
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;           // XSS protection
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // HTTPS only
        options.Cookie.SameSite = SameSiteMode.Strict;            // CSRF protection
        options.Cookie.Name = ".AMIS.Auth";
    });

// Register authentication state provider
builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();

// Register token management services
builder.Services.AddScoped<AuthorizationHeaderHandler>();
builder.Services.AddScoped<ITokenRefreshService, TokenRefreshService>();
builder.Services.AddScoped<ICircuitTokenCache, CircuitTokenCache>();
builder.Services.AddScoped<IAuthStateNotifier, AuthStateNotifier>();

// Configure HttpClient for API calls
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthorizationHeaderHandler>();
    var apiUri = new Uri(apiBaseUrl);
    var innerHandler = new HttpClientHandler();

    // Allow self-signed certs for localhost in development
    if (builder.Environment.IsDevelopment() &&
        (string.Equals(apiUri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(apiUri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)))
    {
        innerHandler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }

    handler.InnerHandler = innerHandler;
    return new HttpClient(handler) { BaseAddress = apiUri };
});

// Register all API clients (generated by NSwag)
builder.Services.AddApiClients(builder.Configuration, builder.Environment);
```

### 3.2 Configuration Files

**appsettings.json:**
```json
{
  "Api": {
    "BaseUrl": "https://localhost:7030"
  }
}
```

**appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**appsettings.Production.json:**
```json
{
  "Api": {
    "BaseUrl": ""  // Set via environment variable or secrets
  },
  "AllowedHosts": "ui.example.com"
}
```

---

## 4. Authentication & Authorization Flow

### 4.1 Login Flow

```
1. User fills login form (email, password, tenant) on SimpleLogin.razor
                        ↓
2. Form POST to /bff/auth/login (SimpleBffAuth.cs)
                        ↓
3. BFF calls API: tokenClient.IssueAsync() 
   Request: POST /api/v1/auth/tokens
   Body: { email, password, tenant }
                        ↓
4. API validates credentials, returns JWT tokens:
   {
     "accessToken": "eyJhbGc...",    // Short-lived (15-30 min)
     "refreshToken": "refresh_...",   // Long-lived (7-30 days)
     "expiresIn": 1800
   }
                        ↓
5. BFF extracts claims from JWT (subject, email, name, roles, tenant)
                        ↓
6. BFF calls SignInAsync() to create secure auth cookie:
   - Cookie name: ".AMIS.Auth"
   - Encrypted payload containing:
     * access_token claim
     * refresh_token claim
     * tenant claim
     * role claims
     * name & email claims
   - Cookie security: HttpOnly, Secure, SameSite=Strict
   - Expiration: 7 days with sliding window
                        ↓
7. BFF redirects to "/" (home page)
                        ↓
8. Browser receives cookie and authenticated request
                        ↓
9. AuthenticationStateProvider reads cookie and populates AuthenticationState
                        ↓
10. Blazor components can check User.Identity.IsAuthenticated
```

### 4.2 API Request Flow With Token Injection

```
1. Component calls API client:
   var users = await UsersClient.SearchAsync(filter);
                        ↓
2. Generated client creates HttpRequestMessage:
   GET /api/v1/identity/users?pageNumber=1&pageSize=10
                        ↓
3. Request passes through AuthorizationHeaderHandler (DelegatingHandler)
   
   a) Extract current access token:
      - First check CircuitTokenCache (refreshed tokens)
      - Fall back to httpContext.User.FindFirst("access_token")
      - If found, attach as Bearer token:
        Authorization: Bearer eyJhbGc...
                        ↓
   b) Send request to API
                        ↓
   c) Check response status:
      - If 200: Return response to component ✓
      - If 401: Token may be expired, attempt refresh (see 4.3)
      - If 403: Permission denied (user handled by component)
                        ↓
4. Component receives typed response:
   SearchUserResponse { Data, TotalCount, PageNumber, PageSize }
```

### 4.3 Token Refresh Flow (CRITICAL!)

```
Triggered when: API returns 401 Unauthorized

1. AuthorizationHeaderHandler receives 401 response
                        ↓
2. Guard: If sign-out already initiated, skip refresh → return 401
                        ↓
3. Attempt token refresh via TokenRefreshService
                        ↓
4. TokenRefreshService validates:
   ✓ HttpContext exists
   ✓ Refresh token available (from cache or claims)
   ✓ Refresh not recently failed (5-min blacklist)
   ✓ Cached refresh not too old (30-sec cache)
                        ↓
5. Acquire SemaphoreSlim lock (prevents thundering herd):
   - Multiple requests shouldn't all call refresh API simultaneously
   - Share single refresh response among concurrent requests
                        ↓
6. Call API: tokenClient.RefreshAsync()
   Request: POST /api/v1/auth/tokens/refresh
   Body: {
     "token": "old_jwt_token",
     "refreshToken": "refresh_...",
     "tenant": "tenant_id"
   }
                        ↓
7. API validates refresh token:
   - Check refresh token not revoked
   - Check refresh token not expired
   - Check tenant still valid
   - Return new access token (and optionally new refresh token)
                        ↓
8. On success:
   a) Update CircuitTokenCache with new tokens
   b) Update httpContext claims (SignInAsync) to update cookie
   c) Cache result for 30 seconds (FailedTokenCacheDuration)
   d) Clone original request with new JWT
   e) Retry request automatically
                        ↓
9. On failure (401 from refresh):
   a) Mark as recently failed (5-min blacklist)
   b) Clear CircuitTokenCache
   c) Call IAuthStateNotifier.NotifySessionExpired()
   d) Component receives event → navigates to /login with forceLoad
   e) ForceLoad=true forces hard browser redirect (clears cookies)
                        ↓
10. Return response to component (either successful retry or 401)
```

### 4.4 Session Expiration Notification

When token refresh fails:

```csharp
// AuthorizationHeaderHandler detects 401 + refresh failure
await SignOutUserAsync();

// This triggers IAuthStateNotifier.NotifySessionExpired()
// Which fires SessionExpired event

// PlaygroundLayout.razor listens:
protected override void OnInitialized()
{
    AuthStateNotifier.SessionExpired += async (_, __) =>
    {
        await JS.InvokeVoidAsync("eval", 
            "window.location.href='/login?toast=session_expired'; location.reload(true)");
    };
}

// Result: User redirected to login with toast notification
```

---

## 5. Key Services Deep Dive

### 5.1 AuthorizationHeaderHandler

**File:** `Services/Api/AuthorizationHeaderHandler.cs`

**Responsibility:** Injected as a DelegatingHandler in the HttpClient pipeline to:
1. Extract JWT token from claims or cache
2. Attach as Bearer token to every request
3. Handle 401 responses with token refresh retry
4. Sign out user if refresh fails

**Key Properties:**
```csharp
private const TimeSpan TokenExpirationBuffer = TimeSpan.FromMinutes(2);
// Refreshes token 2 minutes BEFORE expiration to avoid edge cases
```

**Decision Logic:**
- If response is NOT 401: Return immediately
- If response IS 401:
  - Already signed out? → Return 401
  - No token available? → Return 401
  - Try refresh → If success, auto-retry request
  - If refresh fails → Sign out, notify components

### 5.2 TokenRefreshService

**File:** `Services/Api/TokenRefreshService.cs`

**Responsibility:** Manages the token refresh API call with:
1. Concurrency control (SemaphoreSlim)
2. Caching to prevent duplicate API calls
3. Failure tracking (5-min blacklist for recently-failed tokens)
4. JWT parsing to build new claims
5. Cookie update via SignInAsync

**Concurrency Handling:**
```
Multiple simultaneous 401s from different components
                ↓
All acquire lock sequentially (10-sec timeout)
                ↓
First one: Makes API call, caches result (30 sec)
Others: Wait for lock, find cached result, use it
                ↓
Single API call for multiple concurrent requests
```

**Cache Duration:**
- Successful refresh: 30 seconds (RefreshCacheDuration)
- Failed refresh: 5 minutes (FailedTokenCacheDuration)

### 5.3 CircuitTokenCache

**File:** `Services/Api/CircuitTokenCache.cs`

**Why It Exists:** 
In Blazor Server, `HttpContext.User` is cached per circuit and NOT updated after `SignInAsync`. 
So components would still see the old token until the circuit updates.

**Solution:** Store refreshed tokens in a circuit-scoped service that:
1. Gets checked first before falling back to claims
2. Gets cleared on logout or session expiration
3. Is instance-per-circuit (Scoped registration)

**Flow:**
```
AuthorizationHeaderHandler.GetAccessTokenAsync():
1. Check CircuitTokenCache.AccessToken first (refreshed)
2. Fall back to HttpContext.User claims (original)
3. If both empty, return null
```

### 5.4 CookieAuthenticationStateProvider

**File:** `Services/CookieAuthenticationStateProvider.cs`

```csharp
internal sealed class CookieAuthenticationStateProvider 
    : ServerAuthenticationStateProvider;
```

**Key Point:** Intentionally empty! 

Inherits all behavior from `ServerAuthenticationStateProvider`, which:
- Automatically reads from `HttpContext.User`
- Populated by ASP.NET Core's cookie authentication middleware
- Works with SSR (Server-Side Rendering)

### 5.5 IAuthStateNotifier

**File:** `Services/IAuthStateNotifier.cs`

Provides event-driven notification system for:
- Session expiration
- User logout
- Authentication state changes

**Used by:** `PlaygroundLayout.razor` to trigger navigation on session expiration

```csharp
AuthStateNotifier.SessionExpired += async (_, __) =>
{
    // Navigate to login with forceLoad=true (hard refresh, clears cookies)
    Navigation.NavigateTo("/login", forceLoad: true);
};
```

---

## 6. API Client Registration

**File:** `Services/Api/ApiClientRegistration.cs`

Registers all typed API clients generated by NSwag:

```csharp
// Named HttpClient for token operations (no auth handler - avoids circular dependency)
services.AddHttpClient("TokenClient", client =>
{
    client.BaseAddress = apiUri;
});

// Typed clients registered as Transient
// Each receives the scoped HttpClient with auth handler
services.AddTransient<IIdentityClient>(sp =>
    new IdentityClient(sp.GetRequiredService<HttpClient>()));

services.AddTransient<IProductsClient>(sp =>
    new ProductsClient(sp.GetRequiredService<HttpClient>()));

// ... more clients for each module
```

**Why "TokenClient" is special:**
- Doesn't use AuthorizationHeaderHandler (avoids circular dependency)
- AuthorizationHeaderHandler → needs token refresh
- Token refresh → calls TokenClient
- Without special handling: Circular loop!

**Solution:** TokenClient uses "TokenClient" HttpClientHandler directly, bypassing auth handler

---

## 7. Generated API Clients

**File:** `ApiClient/Generated.cs`

Contains NSwag-generated typed clients:
- `IIdentityClient` - Auth, users, roles, permissions
- `ITenantsClient` - Multi-tenancy operations
- `IAuditsClient` - Audit logging queries
- `IProductsClient` - Expendable module products
- `IPurchasesClient` - Expendable module purchases
- `ICartClient` - Shopping cart operations
- `IInventoryClient` - Warehouse/inventory management
- And more...

**Client Interface Example:**
```csharp
public interface IIdentityClient
{
    ValueTask RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken = default);
    ValueTask<UserDto> UsersGetAsync(Guid id, CancellationToken cancellationToken = default);
    ValueTask<ICollection<RoleDto>> RolesGetAsync(CancellationToken cancellationToken = default);
    // ... more methods
}
```

**Generation Process:**
- NSwag reads OpenAPI spec from API
- Generates strongly-typed clients
- Handles: JSON serialization, error handling, parameter binding
- Uses ValueTask for async operations

---

## 8. Component Usage Example

**File:** `Components/Pages/Users/UsersPage.razor`

```csharp
@inject IIdentityClient IdentityClient
@inject IUsersClient UsersClient
@inject ISnackbar Snackbar

@code {
    protected override async Task OnInitializedAsync()
    {
        await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            // Call generated API client
            var response = await UsersClient.SearchAsync(new SearchUsersCommand
            {
                PageNumber = _page,
                PageSize = _pageSize,
                Search = _filter.Search,
                IsActive = _filter.IsActive
            });

            _users = response.Data;
            _totalCount = response.TotalCount;
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            // AuthorizationHeaderHandler already handled token refresh
            // If we still get 401, session truly expired
            Snackbar.Add("Session expired. Please login again.", Severity.Error);
        }
        catch (ApiException ex)
        {
            Snackbar.Add($"Failed to load users: {ex.Message}", Severity.Error);
        }
    }

    private async Task DeleteAsync(Guid userId)
    {
        await UsersClient.DeleteAsync(userId);
        await LoadUsersAsync();
        Snackbar.Add("User deleted successfully", Severity.Success);
    }
}
```

---

## 9. Error Handling Strategies

### 9.1 Network Errors

```csharp
try
{
    await client.SomeOperationAsync();
}
catch (HttpRequestException ex)
{
    // Network issue, timeout, TLS error, etc.
    Snackbar.Add("Cannot reach API", Severity.Error);
}
```

### 9.2 401 Unauthorized

```csharp
catch (ApiException ex) when (ex.StatusCode == 401)
{
    // AuthorizationHeaderHandler handles 401 with automatic refresh
    // If we still receive 401 here, refresh failed
    // AuthStateNotifier already navigated to login
}
```

### 9.3 403 Forbidden

```csharp
catch (ApiException ex) when (ex.StatusCode == 403)
{
    // User lacks permission for this operation
    Snackbar.Add("You don't have permission for this action", Severity.Error);
}
```

### 9.4 Validation Errors (400)

```csharp
catch (ApiException ex) when (ex.StatusCode == 400)
{
    // Parse validation error response
    var errors = JsonSerializer.Deserialize<ValidationProblem>(ex.Response);
    foreach (var error in errors.Errors)
    {
        form.AddError(error.Key, error.Value.First());
    }
}
```

---

## 10. Security Considerations

### 10.1 XSS Protection
- Cookies: `HttpOnly = true` (inaccessible to JavaScript)
- JWT tokens stored in claims, not localStorage
- CSP headers managed by framework

### 10.2 CSRF Protection
- Cookies: `SameSite = Strict` (only sent to same site)
- ASP.NET Core AntiForgery on form endpoints
- BFF endpoints disable AntiForgery for API calls

### 10.3 HTTPS/TLS
- Production: Always enforced
- Development: Self-signed certs allowed for localhost

### 10.4 Token Security
- Access tokens: Short-lived (15-30 minutes)
- Refresh tokens: Long-lived, stored securely in encoded claims
- Tokens never logged or exposed
- Rotation on refresh (optional, per API policy)

### 10.5 Multi-Tenancy
- Tenant claim included in JWT
- API enforces tenant isolation
- Tokens tied to tenant - can't use tenant A token for tenant B

---

## 11. Deployment Architecture

### Development (`appsettings.Development.json`)
```json
{
  "Api": {
    "BaseUrl": "https://localhost:7030"
  }
}
```
- Blazor runs on https://localhost:7032
- API runs on https://localhost:7030
- CORS NOT needed (same origin in deployment)

### Production (`appsettings.Production.json`)
```json
{
  "Api": {
    "BaseUrl": ""  // Set via environment variable
  },
  "AllowedHosts": "ui.example.com"
}
```
- Blazor UI and API both behind ALB
- Blazor: ui.example.com
- API: api.example.com (or same origin /api/* routed by ALB)
- NO CORS needed (ALB routes internally)
- Environment variable: `Api__BaseUrl=https://api.example.com`

---

## 12. Troubleshooting Guide

### Issue: 401 on Every Request
**Causes:**
1. API not started or wrong URL
2. JWT validation key mismatch
3. Token actually expired

**Debug:**
```csharp
// Log token extraction
_logger.LogInformation("Access token: {Token}", accessToken?.Substring(0, 20) + "...");

// Check JWT expiration
var handler = new JwtSecurityTokenHandler();
var jwt = handler.ReadJwtToken(accessToken);
_logger.LogInformation("Token expires at: {Expiration}", jwt.ValidTo);
```

### Issue: "TLS Error" Connecting to API
**Cause:** Self-signed certificate not trusted

**Fix:**
```csharp
// Already handled in Program.cs for localhost in Development:
if (builder.Environment.IsDevelopment() && 
    (apiUri.Host == "localhost" || apiUri.Host == "127.0.0.1"))
{
    innerHandler.ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
}
```

### Issue: Token Refresh Stuck in Loop
**Cause:** Refresh token API endpoint not working

**Debug:**
```csharp
// Check TokenRefreshService logging
var logger = logger.GetRequiredService<ILogger<TokenRefreshService>>();
// Should see: "Token refresh successful" or "Token refresh failed"
```

### Issue: Components See Old Token After Login
**Cause:** CircuitTokenCache not being populated

**Debug:**
```csharp
// In AuthorizationHeaderHandler
_logger.LogInformation("Access token from cache: {HasCache}", 
    !string.IsNullOrEmpty(_circuitTokenCache.AccessToken));
_logger.LogInformation("Access token from claims: {HasClaims}",
    !string.IsNullOrEmpty(user.FindFirst("access_token")?.Value));
```

---

## 13. Request/Response Cycle Example

**Scenario:** User creates a new product

```
1. COMPONENT (ProductsPage.razor)
   ↓
   var newProduct = new CreateProductCommand 
   { 
       Name = "Widget", 
       Price = 9.99 
   };
   var productId = await ProductsClient.CreateAsync(newProduct);

2. GENERATED CLIENT (ProductsClient, from Generated.cs)
   ↓
   Creates: POST /api/v1/expendable/products
   Content-Type: application/json
   Body: { "name": "Widget", "price": 9.99 }

3. AUTHORIZATION HEADER HANDLER
   ↓
   a) Get access token from CircuitTokenCache or claims
   b) Add header: Authorization: Bearer eyJhbGc...
   c) Send request:
      POST /api/v1/expendable/products HTTP/2
      Authorization: Bearer eyJhbGc...
      Content-Type: application/json
      Body: { "name": "Widget", "price": 9.99 }

4. KESTREL (Production server listens on 7030)
   ↓
   Receives HTTPS request

5. ASP.NET CORE MIDDLEWARE
   ↓
   a) HTTPS Redirection middleware (already HTTPS ✓)
   b) Authentication middleware
      - Extracts Bearer token from header
      - Validates JWT signature and expiration
      - Creates ClaimsPrincipal from JWT claims
   c) Authorization middleware
      - Checks .RequirePermission(ExpendablePermissions.Products.Create)
      - User has this permission? ✓ Continue

6. ROUTE HANDLER (API Endpoint)
   ↓
   [HttpPost("")]
   [Authorize]
   [RequirePermission(ExpendablePermissions.Products.Create)]
   public async Task<CreatedResult> CreateAsync(
       CreateProductCommand command, 
       IMediator mediator)
   {
       var productId = await mediator.Send(command);
       return Created($"/api/v1/products/{productId}", productId);
   }

7. MEDIATOR (CQRS)
   ↓
   a) FindHandler(CreateProductCommand)
   b) Execute CreateProductHandler
      - Create Product aggregate
      - Validate command
      - Add to repository
      - Persist to PostgreSQL
   c) Return: productId = "550e8400-e29b-41d4-a716-446655440000"

8. RESPONSE
   ↓
   HTTP/2 201 Created
   Location: /api/v1/products/550e8400-e29b-41d4-a716-446655440000
   Content-Type: application/json
   Body: "550e8400-e29b-41d4-a716-446655440000"
   × No Set-Cookie (API uses JWT, not cookies)

9. BACK TO HANDLER (AuthorizationHeaderHandler)
   ↓
   Status: 201 (not 401)
   → Return response as-is

10. GENERATED CLIENT
    ↓
    Deserialize response: string productId = "550e8400-e29b-41d4-a716-446655440000"

11. COMPONENT
    ↓
    var productId = await ProductsClient.CreateAsync(newProduct);
    // productId = "550e8400-e29b-41d4-a716-446655440000"
    
    Snackbar.Add("Product created successfully", Severity.Success);
    await LoadProductsAsync(); // Refresh list
```

---

## 14. Key Files Reference

| File | Purpose |
|------|---------|
| `Program.cs` | Blazor app DI setup, HttpClient config |
| `Services/SimpleBffAuth.cs` | Login/logout endpoints (/bff/auth/*) |
| `Services/Api/AuthorizationHeaderHandler.cs` | JWT injection + 401 refresh |
| `Services/Api/TokenRefreshService.cs` | Token refresh API call handler |
| `Services/Api/CircuitTokenCache.cs` | Circuit-scoped token storage |
| `Services/CookieAuthenticationStateProvider.cs` | Auth state from cookies |
| `Services/IAuthStateNotifier.cs` | Session expiration notifications |
| `Services/Api/ApiClientRegistration.cs` | DI registration of API clients |
| `ApiClient/Generated.cs` | NSwag-generated typed API clients |
| `Components/Pages/SimpleLogin.razor` | Login UI form |
| `appsettings.json` | API base URL config |

---

## 15. Summary

The Blazor-to-API connection uses a sophisticated, production-grade architecture that:

✅ **Secure:** Cookies (HttpOnly, Secure, SameSite), JWT tokens, HTTPS-only  
✅ **Resilient:** Automatic token refresh, graceful degradation, error handling  
✅ **Efficient:** Token caching, concurrent request deduplication  
✅ **User-Friendly:** Transparent auth updates, session expiration notifications  
✅ **Scalable:** Multi-tenancy aware, modular design, DI-based testability  
✅ **Developer-Friendly:** Typed clients, clear separation of concerns  

The key insight: **BFF Pattern** normalizes authentication (cookies) between browser and Blazor, while API uses standard JWT bearer tokens. Each client type gets the appropriate security model.

