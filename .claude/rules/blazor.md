---
paths:
  - "src/Playground/Playground.Blazor/**"
---

# Blazor UI Rules

`Playground.Blazor` is a **Blazor Server** application. It consumes the REST API via typed HTTP clients and uses scoped DI services for shared session state across components.

## Shared Session State Pattern

Blazor Server uses **Scoped** lifetime per circuit (one scope = one connected user). Shared state services follow the `IUserProfileState` pattern: scoped, event-based, owned by `PlaygroundLayout`.

### Existing State Services

| Service | Lifetime | Owner | Purpose |
| --- | --- | --- | --- |
| `IUserProfileState` | Scoped | `PlaygroundLayout` | Current user: name, email, role, avatar, `EmployeeId`, `EmployeeFullName`, `EmployeePositionName` |
| `IOrganizationProfileState` | Scoped | `PlaygroundLayout` | Agency name, address, 4 key officers — for report headers |
| `ITenantThemeState` | Scoped | `PlaygroundLayout` | Theme colors, dark mode, logo, favicon |
| `IAuthStateNotifier` | Scoped | `PlaygroundLayout` | Session expiry broadcast |
| `ICircuitTokenCache` | Scoped | Framework | Token cache per circuit |

### Pattern for New State Services

Follow `IUserProfileState` exactly:

```csharp
// src/Playground/Playground.Blazor/Services/MyState.cs
internal interface IMyState
{
    SomeDto? Value { get; }
    event Action? OnChanged;
    void Set(SomeDto? value);
}

internal sealed class MyState : IMyState
{
    public SomeDto? Value { get; private set; }
    public event Action? OnChanged;

    public void Set(SomeDto? value)
    {
        Value = value;
        OnChanged?.Invoke();
    }
}
```

**Registration** — add to `Program.cs` alongside the other scoped state services (around line 62):

```csharp
builder.Services.AddScoped<IMyState, MyState>();
```

**Initialization** — load in `PlaygroundLayout.OnAfterRenderAsync(firstRender)`, piggybacking an existing API call where possible. Never add a new HTTP call just to populate state if the data is already fetched elsewhere.

**Disposal** — if the layout subscribes to the state's `OnChanged` event, unsubscribe in `Dispose()`.

---

## Organization Profile in Reports

`IOrganizationProfileState` holds the tenant's agency details (name, short name, address, 4 key officers). It is populated once per session by `PlaygroundLayout` — the same `OrgProfileClient.GetAsync()` call that checks whether the setup dialog should appear.

**It is always ready before any page renders. No async load needed in report pages.**

### How to Use in a Report / Print Page

Two lines of new code per page:

```razor
@* 1. Inject — namespace is globally imported in _Imports.razor *@
@inject IOrganizationProfileState OrgProfileState

@* 2. Use in markup — no await, no loading spinner, no try/catch *@
@OrgProfileState.Profile?.Name
@OrgProfileState.Profile?.ShortName
@OrgProfileState.Profile?.Address
@OrgProfileState.Profile?.ApprovingOfficialName       @* Approving official — Regional Manager II / Branch Manager *@
@OrgProfileState.Profile?.AssistantRegionalManagerName
@OrgProfileState.Profile?.AccountantName              @* Accountant IV *@
@OrgProfileState.Profile?.SupervisingAdminOfficerName
```

### Standard Agency Header Block for Print Pages

Use this markup at the top of every print-area `<div>` so all reports are consistent:

```razor
@{
    var _org = OrgProfileState.Profile;
}
@if (_org is not null)
{
    <div style="text-align:center; margin-bottom:6px; line-height:1.4;">
        <div style="font-size:11px;">Republic of the Philippines</div>
        <div style="font-size:13px; font-weight:700;">@_org.Name</div>
        @if (!string.IsNullOrWhiteSpace(_org.ShortName))
        {
            <div style="font-size:11px;">(@_org.ShortName)</div>
        }
        @if (!string.IsNullOrWhiteSpace(_org.Address))
        {
            <div style="font-size:11px;">@_org.Address</div>
        }
    </div>
}
```

### Standard Officer Signature Block for Print Pages

Always use the stored designation — never hardcode the title string:

```razor
@if (!string.IsNullOrWhiteSpace(_org?.ApprovingOfficialName))
{
    <div class="sign-block">
        <div class="sign-name">@_org.ApprovingOfficialName</div>
        <div class="sign-role">@(_org.ApprovingOfficialDesignation ?? "Designation")</div>
    </div>
}
```

Available officer fields — always pair Name with Designation, never hardcode the title:

| Name field | Designation field | Default fallback (if null) |
| --- | --- | --- |
| `ApprovingOfficialName` | `ApprovingOfficialDesignation` | "Designation" |
| `AssistantRegionalManagerName` | `AssistantRegionalManagerDesignation` | "Assistant Regional Manager" |
| `AccountantName` | `AccountantDesignation` | "Accountant IV" |
| `SupervisingAdminOfficerName` | `SupervisingAdminOfficerDesignation` | "Supervising Administrative Officer" |

Designations are free-text and can be "Acting Regional Manager II", "OIC-Regional Manager II", etc. — always use the stored value on print, never hardcode the title string.

### Keeping State Fresh

`OrganizationProfilePage` calls `OrgProfileState.SetProfile(result)` immediately after a successful save. Any report page the user navigates to after saving will see the updated officers and agency name — no re-login required.

### Reference Implementation

`src/Playground/Playground.Blazor/Components/Pages/AssetProcurement/AssetIARExhibit3PrintPage.razor` is the canonical example. Replicate the pattern for all other report pages.

---

## Permission Gating for UI

Every page action — buttons, menu items, row icons — MUST be gated by the same permission its API endpoint enforces with `.RequirePermission(...)`. Backend authorization is necessary but not sufficient: a user without permission should never see the button.

Permissions flow: at login the API returns the user's effective permissions (derived from their assigned roles via `UserPermissionService.GetPermissionsAsync`). `PlaygroundLayout` loads them once per circuit into `UserProfileState.Permissions` (a `HashSet<string>`). Pages read this synchronously — no async, no extra HTTP calls.

### Pattern

Permission **strings** are declared once per module in the **Contracts** project — at `Modules.{Name}.Contracts/Permissions/{Name}Permissions.cs`. The server endpoint and the Blazor page reference the same constant. Never declare `private const string Permission* = "..."` inside a page — that's magic-string duplication that drifts when the key is renamed.

```csharp
// src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition.Contracts/Permissions/ProcurementPermissions.cs
namespace AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

public static class ProcurementPermissions
{
    public static class PurchaseRequests
    {
        public const string Create  = "Permissions.Procurement.PurchaseRequests.Create";
        public const string Approve = "Permissions.Procurement.PurchaseRequests.Approve";
        // ...
    }
}
```

Server endpoint (impl project — already references its own Contracts):

```csharp
endpoints.MapPost("/", handler)
    .RequirePermission(ProcurementPermissions.PurchaseRequests.Create);
```

Blazor page (already references the Contracts project):

```razor
@using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions
@inject IUserProfileState UserProfileState

@if (_canCreate)
{
    <MudButton OnClick="OpenCreateDialog">+ New</MudButton>
}

@if (item.Status == SomeStatus.PendingApproval && _canApprove)
{
    <MudIconButton Icon="@Icons.Material.Filled.CheckCircle"
                   OnClick="@(() => ApproveAsync(item.Id))" />
}

@code {
    private bool _canCreate  => UserProfileState.Permissions.Contains(ProcurementPermissions.PurchaseRequests.Create);
    private bool _canApprove => UserProfileState.Permissions.Contains(ProcurementPermissions.PurchaseRequests.Approve);
}
```

### Rules

| ⚠️ Rule | Why |
| --- | --- |
| Permission string constants live in `Modules.{Name}.Contracts/Permissions/{Name}Permissions.cs` — one source for both UI and API | Renames become compile errors instead of silent UI drift |
| Never declare `private const string Permission* = "..."` in a page | Magic-string duplication breaks the single source of truth |
| Every action button gated with `UserProfileState.Permissions.Contains(SomePermissions.Xxx.Yyy)` | Mirrors endpoint `.RequirePermission()` — users don't see actions they can't perform |
| Combine permission check with status check via `&&`, not nested `@if` | Single line of gating intent per button |
| Read `UserProfileState.Permissions` directly — no async, no caching | Already loaded once per circuit by `PlaygroundLayout` |
| Newly assigned roles require **user re-login** to take effect | API-side permission cache is keyed per user |

### Canonical references

- Constants: [ProcurementPermissions.cs](../../src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition.Contracts/Permissions/ProcurementPermissions.cs)
- UI usage: [AssetIARsPage.razor](../../src/Playground/Playground.Blazor/Components/Pages/AssetProcurement/AssetIARsPage.razor)

---

## Compact UI Controls

| ⚠️ Rule | Why |
| --- | --- |
| Prefer `AMISTextField`, `AMISSelect`, `AMISAutocomplete` | Enforces compact defaults consistently |
| If using raw Mud inputs, set `Dense="true"` + `Margin="Margin.Dense"` | Keeps filter/form rows aligned and compact |
| Use `Size="Size.Small"` for filter-row inputs and action buttons | Establishes the 40px compact baseline |
| Avoid mixing default and compact controls in one row | Prevents visible height mismatch and misalignment |

---

## API Client Lifetimes

All API clients (`IMaster_dataClient`, `ILookupClient`, etc.) are registered **Transient** in `ApiClientRegistration.cs`. They are stateless and resolved fresh per injection point. Do not cache them manually — inject and use directly.

**Details:** See `src/Playground/Playground.Blazor/Services/Api/ApiClientRegistration.cs`
