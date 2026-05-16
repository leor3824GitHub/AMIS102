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
| `IUserProfileState` | Scoped | `PlaygroundLayout` | Current user name, email, role, avatar |
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
@OrgProfileState.Profile?.RegionalManagerName         @* Regional Manager II *@
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
@if (!string.IsNullOrWhiteSpace(_org?.RegionalManagerName))
{
    <div class="sign-block">
        <div class="sign-name">@_org.RegionalManagerName</div>
        <div class="sign-role">@(_org.RegionalManagerDesignation ?? "Regional Manager II")</div>
    </div>
}
```

Available officer fields — always pair Name with Designation, never hardcode the title:

| Name field | Designation field | Default fallback (if null) |
| --- | --- | --- |
| `RegionalManagerName` | `RegionalManagerDesignation` | "Regional Manager II" |
| `AssistantRegionalManagerName` | `AssistantRegionalManagerDesignation` | "Assistant Regional Manager" |
| `AccountantName` | `AccountantDesignation` | "Accountant IV" |
| `SupervisingAdminOfficerName` | `SupervisingAdminOfficerDesignation` | "Supervising Administrative Officer" |

Designations are free-text and can be "Acting Regional Manager II", "OIC-Regional Manager II", etc. — always use the stored value on print, never hardcode the title string.

### Keeping State Fresh

`OrganizationProfilePage` calls `OrgProfileState.SetProfile(result)` immediately after a successful save. Any report page the user navigates to after saving will see the updated officers and agency name — no re-login required.

### Reference Implementation

`src/Playground/Playground.Blazor/Components/Pages/AssetProcurement/AssetIARExhibit3PrintPage.razor` is the canonical example. Replicate the pattern for all other report pages.

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
