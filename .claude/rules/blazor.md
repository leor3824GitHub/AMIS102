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

`src/Playground/Playground.Blazor/Components/Pages/InspectionAcceptanceReports/InspectionAcceptanceReportExhibit3PrintPage.razor` is the canonical example. Replicate the pattern for all other report pages.

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
- UI usage: [InspectionAcceptanceReportsPage.razor](../../src/Playground/Playground.Blazor/Components/Pages/InspectionAcceptanceReports/InspectionAcceptanceReportsPage.razor)

---

## Compact UI Controls

All filter bars, form rows, and action button groups share a **40px compact baseline**. Every control on the same horizontal row MUST resolve to that baseline — otherwise `AlignItems.Center` centers controls of unequal height and the row visibly floats off-axis.

### Rules

| ⚠️ Rule | Why |
| --- | --- |
| Prefer `AMISTextField`, `AMISSelect`, `AMISAutocomplete` from `BuildingBlocks/Blazor.UI` | Wrappers enforce `Dense="true" + Margin="Margin.Dense" + InputSize="Size.Small"` — no per-page sizing drift |
| Prefer `AMISButton` / `AMISIconButton` from `BuildingBlocks/Blazor.UI` over raw `MudButton`/`MudIconButton` | Single source of truth for button styling (`.AMIS-btn`) + sizing. `AMISButton` defaults to `Size.Medium`; `AMISIconButton` defaults to `Size.Small` |
| If using raw Mud inputs, set `Dense="true"` + `Margin="Margin.Dense"` | Drops a 56px Outlined input to the 40px compact baseline |
| **Two-tier button sizing.** Standalone primary CTAs (e.g. page-header "New X") stay `Size.Medium` (48px). Buttons sharing a row with compact inputs use `Size="Size.Small"` (40px) | A header CTA sits alone, so 48px is correct and prominent; an inline action button must match the 40px input baseline next to it |
| Every control in the same `MudGrid`/`MudStack` row must share the same density | `AlignItems.Center` only looks right when all heights are equal |
| Action buttons paired with inputs in the same row → all must be `Size.Small` | The most common misalignment is a default-size input + default-size button (56 vs 48px) |
| If a single button must stay at default size (e.g. primary CTA), drop it to its own row | Don't try to mix heights in one row — split rows instead |

### Filter + Action Bar — Canonical Pattern

**Two acceptable layouts.** Pick one per page; do not mix.

#### Pattern A — Inputs and buttons on the same row (compact)

Use when filters are few (≤ 3) and actions are secondary (export, download). Every control MUST be compact.

```razor
<MudPaper Class="pa-4 mb-4" Elevation="1">
    <MudGrid Spacing="2" AlignItems="AlignItems.Center">
        <MudItem xs="12" sm="8">
            <MudAutocomplete T="ProductDto"
                             Label="Select Product"
                             Value="_selectedProduct"
                             ValueChanged="OnProductSelected"
                             SearchFunc="SearchProductsAsync"
                             Variant="Variant.Outlined"
                             Dense="true"
                             Margin="Margin.Dense" />        @* ← compact baseline *@
        </MudItem>
        <MudItem xs="12" sm="4">
            <MudStack Row="true" Spacing="2">
                <MudButton Size="Size.Small"                 @* ← matches input baseline *@
                           Variant="Variant.Outlined"
                           StartIcon="@Icons.Material.Filled.Download"
                           OnClick="ExportCsvAsync">Export CSV</MudButton>
                <MudButton Size="Size.Small"
                           Variant="Variant.Filled" Color="Color.Error"
                           StartIcon="@Icons.Material.Filled.PictureAsPdf"
                           OnClick="DownloadPdfAsync">Download PDF</MudButton>
            </MudStack>
        </MudItem>
    </MudGrid>
</MudPaper>
```

#### Pattern B — Inputs on top, action buttons in a row below

Use when filters are many (4+) or the primary action is a "Generate Report" CTA. Filters can be default-density; buttons live on their own row so the mismatch is irrelevant.

Canonical reference: [DepartmentIssuanceReportPage.razor](../../src/Playground/Playground.Blazor/Components/Pages/Expendable/DepartmentIssuanceReportPage.razor), [PhysicalCountReportPage.razor](../../src/Playground/Playground.Blazor/Components/Pages/Expendable/PhysicalCountReportPage.razor).

### Anti-Pattern

```razor
@* ❌ Wrong — input is 56px (default Outlined), buttons are 48px (default).
   AlignItems.Center centers them by vertical mid-line, so buttons "float"
   above the input's text baseline. Visually misaligned. *@
<MudGrid Spacing="2" AlignItems="AlignItems.Center">
    <MudItem xs="8">
        <MudAutocomplete ... Variant="Variant.Outlined" />          @* 56px *@
    </MudItem>
    <MudItem xs="4">
        <MudStack Row="true">
            <MudButton Variant="Variant.Outlined">Export CSV</MudButton>     @* 48px *@
            <MudButton Variant="Variant.Filled">Download PDF</MudButton>     @* 48px *@
        </MudStack>
    </MudItem>
</MudGrid>
```

---

## API Client Lifetimes

All API clients (`IMaster_dataClient`, `ILookupClient`, etc.) are registered **Transient** in `ApiClientRegistration.cs`. They are stateless and resolved fresh per injection point. Do not cache them manually — inject and use directly.

**Details:** See `src/Playground/Playground.Blazor/Services/Api/ApiClientRegistration.cs`

---

## Unicode / Encoding in `.razor` Files

`.razor` files contain non-ASCII glyphs in UI text: `₱` (peso, U+20B1), `—` em-dash, `…` ellipsis, `→` arrow, `≤`/`≥`, `≈`, `§`, `▲`/`▼` sort indicators, and Filipino names (`ñ`). **These files MUST be saved as UTF-8.**

### The recurring bug

Saving a `.razor` file in a non-UTF-8 codepage (ANSI/Windows-1252) silently corrupts every non-ASCII glyph by **one of two mechanisms**:

- Downgraded to `?` (ASCII 0x3F) — e.g. `₱@item.Amount` becomes `?@item.Amount`, rendering "?12.25" instead of "₱12.25".
- Replaced with `�` (U+FFFD, bytes `EF BF BD`) — e.g. `Code — Name` becomes `Code � Name`.

Note: a Razor `? @expr` / `?@expr` in **markup** is always a literal `?` + expression — real C# ternaries never put `@` after `?` — so those are reliably peso corruption, not code.

### Rules

| ⚠️ Rule | Why |
| --- | --- |
| Editor must save `.razor`/`.cs` as **UTF-8** (`"files.encoding": "utf8"` in VS Code) | Otherwise the next save re-corrupts every `₱`, `—`, `→`, `ñ`, etc. |
| Detect corruption before committing: `Grep "�\|\?@\|\? @\|AdornmentText=\"\?\""` | Any hit means a glyph was lost |
| Never "fix" these with a blind global `?`→`₱` replace | `?` is also ternaries, nullables, query strings, and real question marks — context-match instead (peso = `?@`/`? @`/`AdornmentText="?"`/`"?"+money`/`?5,000`) |
| `�` (U+FFFD) is unrecoverable — the original glyph is **lost** and must be inferred from context | separators→`—`, truncation `[..N]+`→`…`, units→`≈`, names→`ñ`, directional (Transfer/ICS/PAR)→`→` |
| When bulk-fixing via script, use a byte-safe Latin1 round-trip (`ISO-8859-1` decode → replace → encode) | Preserves encoding/BOM exactly; plain text I/O can re-encode and strip BOM. **Verify quotes survived** (`grep -o '"' \| wc -l`) — a buggy refactor script once stripped every `"` from `PPEIssuanceReportsPage.razor` and committed it unbuildable |

The PDF generators and domain `.cs` (e.g. `EmployeeIssuancePdfDocument.cs`, `AssetCategory.cs`) were saved correctly as UTF-8 and use `₱`/`≤`/`≈` directly — keep them that way.
