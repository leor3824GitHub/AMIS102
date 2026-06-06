# Blazor Reusable Components Plan (Second Pass)

> BuildingBlocks · MudBlazor · AMIS Framework

## Second-Pass Objectives

- Make the plan implementation-ready (component contracts, behavior, acceptance criteria)
- Reduce migration risk by defining rollout sequence and compatibility rules
- Enforce accessibility, performance, and testability from day one

## Current Inventory

Located in `src/BuildingBlocks/Blazor.UI/Components/`:

| Category | Existing Components |
|---|---|
| Inputs | `AMISTextField`, `AMISSelect`, `AMISAutocomplete` |
| Cards | `AMISCard`, `AMISStatCard` |
| Data | `AMISTable` (basic, client-side only) |
| Page | `AMISPageHeader` |
| Feedback | `AMISAlert`, Snackbar wrappers |
| Display | `AMISBadge` |
| Navigation | `AMISBreadcrumbs` |
| Dialogs | `AMISConfirmDialog` |
| Button | `AMISButton` |

---

## Proposed Components

### Priority 1 — Highest ROI (Most Cross-Module Reuse)

---

#### 1. `AMISDataTable<TItem>` — Server-Side Paginated Table

**Location:** `Components/Data/AMISDataTable.razor`

**Problem:** Every list page calls `MudTable` directly with no consistent server-side paging, sorting, or search bar wiring. Each module duplicates the same boilerplate.

**Design:**

```razor
<AMISDataTable TItem="ItemDto"
               ServerData="LoadDataAsync"
               SearchPlaceholder="Search items..."
               TotalItems="@_totalCount">
    <ToolbarContent>
        <AMISButton StartIcon="@Icons.Material.Filled.Add" OnClick="OpenCreate">Add</AMISButton>
    </ToolbarContent>
    <HeaderContent>
        <MudTh>Name</MudTh>
        <MudTh>Status</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd>@context.Name</MudTd>
        <MudTd><AMISStatusChip Status="@context.Status" /></MudTd>
    </RowTemplate>
</AMISDataTable>
```

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `ServerData` | `Func<TableState, CancellationToken, Task<TableData<TItem>>>` | MudTable server-side callback |
| `SearchPlaceholder` | `string?` | Search bar placeholder text |
| `TotalItems` | `int` | Total record count for pagination |
| `Loading` | `bool` | Overrides loading state manually |
| `ToolbarContent` | `RenderFragment?` | Buttons/filters rendered above the table |
| `HeaderContent` | `RenderFragment` | `<MudTh>` column headers |
| `RowTemplate` | `RenderFragment<TItem>` | `<MudTd>` row cells |
| `EmptyContent` | `RenderFragment?` | Overrides default empty state |

**Behavior:**
- Built-in debounced search bar (400 ms)
- Loading skeleton during fetch
- Falls back to `AMISEmptyState` when `Items` is empty
- Default `Dense="true"`, `Hover="true"`, consistent with AMIS table style

**Acceptance criteria:**
- Supports server-driven paging, sorting, and search in one callback flow
- Exposes query context (page, pageSize, sortLabel, sortDirection, searchText)
- Prevents duplicate requests when users type quickly (debounced + cancellation token)
- Works in `InteractiveServer` render mode with no JS dependency for core behavior
- Keeps row virtualization optional (off by default, opt-in on very large datasets)

**Non-functional requirements:**
- Accessibility: search input has label/aria-label; table headers expose sort state
- Performance: no full-page re-render for page changes; only table subtree updates
- Reliability: request cancellation wired end-to-end to API client methods

---

#### 2. `AMISFormDialog` — Generic CRUD Dialog Wrapper

**Location:** `Components/Dialogs/AMISFormDialog.razor`

**Problem:** Every create/edit dialog in each module manually renders its own `MudDialog` with title, close button, Save/Cancel footer, and loading spinner on submit. Identical structure, different content.

**Design:**

```razor
<AMISFormDialog Title="Create Unit of Measurement"
                OnSave="SubmitAsync"
                OnCancel="MudDialog.Cancel"
                IsLoading="@_saving">
    <AMISTextField Label="Code" @bind-Value="_model.Code" Required="true" />
    <AMISTextField Label="Name" @bind-Value="_model.Name" Required="true" />
    <AMISSelect T="string" Label="Category" @bind-Value="_model.Category">
        <MudSelectItem Value="@("A")">Category A</MudSelectItem>
    </AMISSelect>
</AMISFormDialog>
```

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `Title` | `string` | Dialog title bar text |
| `ChildContent` | `RenderFragment` | Form fields |
| `OnSave` | `EventCallback` | Called when Save is clicked; dialog stays open while `IsLoading` |
| `OnCancel` | `EventCallback` | Called when Cancel/close is clicked |
| `IsLoading` | `bool` | Shows spinner on Save button, disables both buttons |
| `SaveLabel` | `string` | Default: `"Save"` |
| `CancelLabel` | `string` | Default: `"Cancel"` |
| `MaxWidth` | `MaxWidth` | Default: `MaxWidth.Small` |

**Acceptance criteria:**
- Supports create and edit modes with dynamic title/labels
- Disable Save while `IsLoading` or when `IsValid` is false
- Keyboard support: `Esc` cancels, `Enter` submits when valid
- Prevents double submit by disabling Save after first click until callback resolves
- Slot for validation summary and API error display

---

#### 3. `AMISStatusChip` — Semantic Status Badge

**Location:** `Components/Chips/AMISStatusChip.razor`

**Problem:** Status badges (Active/Inactive, Pending/Approved/Rejected, Draft/Posted) appear on nearly every entity list. Each module hard-codes its own `MudChip` with color and label.

**Design:**

```razor
<!-- Auto-mapped from string status -->
<AMISStatusChip Status="@item.Status" />

<!-- Explicit override -->
<AMISStatusChip Status="@item.Status" Color="Color.Warning" Label="In Review" />
```

**Built-in Status Mappings:**

| Status Value | Color | Label |
|---|---|---|
| `Active`, `Approved`, `Posted`, `Completed` | `Success` | as-is |
| `Inactive`, `Rejected`, `Cancelled`, `Deleted` | `Error` | as-is |
| `Pending`, `Draft`, `ForReview`, `Submitted` | `Warning` | as-is |
| `Processing`, `InProgress` | `Info` | as-is |
| _(unknown)_ | `Default` | as-is |

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `Status` | `string` | The raw status string — drives auto color/label |
| `Color` | `Color?` | Override auto-mapped color |
| `Label` | `string?` | Override display text |
| `Size` | `Size` | Default: `Size.Small` |

**Acceptance criteria:**
- Mapping is case-insensitive and whitespace-tolerant
- Unknown statuses never throw; they render with `Color.Default`
- Optional tooltip can display raw backend status for diagnostics
- Styling remains consistent in both light and dark themes

---

### Priority 2 — High Value (Eliminate Per-Page Boilerplate)

---

#### 4. `AMISEmptyState` — Empty List Placeholder

**Location:** `Components/Display/AMISEmptyState.razor`

**Problem:** When a list is empty, each page renders its own ad-hoc message. No consistent visual treatment.

**Design:**

```razor
<AMISEmptyState Icon="@Icons.Material.Filled.Inbox"
                Title="No items found"
                Description="Start by adding a new record."
                ActionLabel="Add Item"
                OnAction="OpenCreate" />
```

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `Icon` | `string` | MudBlazor icon string |
| `Title` | `string` | Primary empty message |
| `Description` | `string?` | Secondary descriptive text |
| `ActionLabel` | `string?` | CTA button label (hidden if null) |
| `OnAction` | `EventCallback` | CTA button click handler |

**Acceptance criteria:**
- Empty state is centered, responsive, and keyboard accessible
- CTA button is omitted cleanly when no action is provided
- Supports optional custom content for advanced scenarios

---

#### 5. `AMISDeleteButton` — Inline Delete with Confirmation

**Location:** `Components/Button/AMISDeleteButton.razor`

**Problem:** Every table row wires up `AMISConfirmDialog` manually before deleting. The confirm-then-delete pattern is copy-pasted across all modules.

**Design:**

```razor
<!-- In a table row -->
<AMISDeleteButton OnConfirmed="() => DeleteAsync(item.Id)"
                  Message="@($"Delete {item.Name}? This cannot be undone.")" />
```

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `OnConfirmed` | `EventCallback` | Called only after user confirms |
| `Message` | `string?` | Confirmation dialog body text |
| `Title` | `string` | Default: `"Confirm Delete"` |
| `Disabled` | `bool` | Disables the button |
| `Size` | `Size` | Default: `Size.Small` |

**Behavior:** Internally uses `AMISConfirmDialog` via `IDialogService`. No dialog boilerplate in the consuming page.

**Acceptance criteria:**
- Uses destructive color and clear confirmation copy by default
- Accepts `EntityName` to auto-generate safer messages
- Supports disabled state during pending deletion requests
- Emits a single `OnConfirmed` event exactly once per confirmed interaction

---

#### 6. `AMISNumericField<T>` — Typed Numeric Input

**Location:** `Components/Inputs/AMISNumericField.razor`

**Problem:** `AMISTextField` can't bind to numeric types. Pages call `MudNumericField<T>` directly without consistent `Dense`/`Outlined` styling.

**Design:**

```razor
<AMISNumericField T="decimal" Label="Unit Price" @bind-Value="_model.UnitPrice"
                  Min="0" Format="N2" Required="true" />
<AMISNumericField T="int" Label="Quantity" @bind-Value="_model.Quantity" Min="1" />
```

**Acceptance criteria:**
- Supports `int`, `long`, `decimal`, `double`, and nullable variants
- Honors culture-aware formatting/parsing (server culture)
- Provides built-in min/max validation feedback

---

#### 7. `AMISDatePicker` — Date Input

**Location:** `Components/Inputs/AMISDatePicker.razor`

**Problem:** `MudDatePicker` is called directly on every form without consistent `Dense`/`Outlined` defaults.

**Design:**

```razor
<AMISDatePicker Label="Date Acquired" @bind-Date="_model.DateAcquired" Required="true" />
```

**Acceptance criteria:**
- Supports nullable and non-nullable date binding
- Supports min/max date boundaries
- Uses consistent display format across modules (single source of truth)

---

### Priority 3 — Situational / Module-Specific

---

#### 8. `AMISDrawerFilter` — Sidebar Filter Panel

**Location:** `Components/Layouts/AMISDrawerFilter.razor`

**Use case:** Heavy list pages (Asset Register, Procurement) that need multi-field filtering without cluttering the toolbar.

**Design:**

```razor
<AMISDrawerFilter @bind-Open="_filterOpen" OnApply="ApplyFilters" OnClear="ClearFilters">
    <AMISSelect T="string" Label="Status" @bind-Value="_filter.Status">...</AMISSelect>
    <AMISDatePicker Label="From Date" @bind-Date="_filter.FromDate" />
</AMISDrawerFilter>
```

**Behavior:** Slides in as a right-side `MudDrawer` on mobile; stays pinned on `md+` breakpoints.

**Acceptance criteria:**
- `Apply` and `Clear` callbacks are explicit and independently testable
- Drawer state survives navigation when configured by the host page
- Mobile layout does not overlap app bar actions

---

## Compatibility Rules

1. New reusable components are additive; no immediate breaking changes to existing pages.
2. Existing `Mud*` direct usage remains valid during migration.
3. For each new wrapper, expose `AdditionalAttributes` to avoid capability regressions.
4. Keep default visual behavior aligned with existing AMIS theme tokens.

---

## Migration Targets (Wave 1)

Prioritize pages with highest duplication and highest traffic in `src/Playground/Playground.Blazor/Components/Pages/`:

- `MasterData/*` list pages: migrate table + status chips + empty states
- `Expendable/*` list and approval pages: migrate table + status chips + delete flows
- `AssetRegister/*` list pages: migrate table + drawer filters
- Shared dialogs in `Components/Shared/`: migrate to `AMISFormDialog` where possible

---

## Testing Strategy

Test scope for each component:

1. Unit tests (bUnit): rendering, parameters, callbacks, edge cases
2. Interaction tests: keyboard navigation, disabled/loading states
3. Visual sanity checks: light/dark themes and responsive breakpoints
4. Integration tests (selected pages): verify API queries still match expected server contract

Minimum quality gate per component before rollout:

- bUnit coverage for happy path + 2 edge cases
- No analyzer warnings introduced in touched projects
- Build passes on `src/AMIS.Framework.slnx`

---

## Definition of Done (Per Component)

- Component created under the correct `BuildingBlocks/Blazor.UI/Components/*` folder
- API documented in XML comments on public parameters
- Included in `src/BuildingBlocks/Blazor.UI/_Imports.razor` when namespace is new
- At least one real page migrated in `Playground.Blazor` as a reference implementation
- Existing behavior preserved (no regression in paging, filtering, save/delete flows)

---

## Implementation Conventions

All components in `BuildingBlocks/Blazor.UI/Components/` must follow:

1. `@inherits AMISComponentBase` — provides `AdditionalAttributes` and base styling hooks
2. `[EditorRequired]` on mandatory parameters — build-time safety
3. `[Parameter(CaptureUnmatchedValues = true)] Dictionary<string, object>? AdditionalAttributes` — pass-through for MudBlazor extras
4. Exported in `BuildingBlocks/Blazor.UI/_Imports.razor` by namespace
5. Consistent defaults: `Dense="true"`, `Variant="Variant.Outlined"` for inputs, `Margin="Margin.Dense"`
6. Avoid JS interop unless there is no native Blazor/MudBlazor alternative
7. Prefer strongly typed parameters over stringly-typed options
8. Include cancellation token-aware async callbacks in all data-fetching components

---

## Rollout Order

| Phase | Components | Reason |
|---|---|---|
| 1 | `AMISDataTable`, `AMISStatusChip`, `AMISEmptyState` | Unblock all list pages |
| 2 | `AMISFormDialog`, `AMISDeleteButton` | Unblock create/edit/delete dialogs |
| 3 | `AMISNumericField`, `AMISDatePicker` | Complete the input set |
| 4 | `AMISDrawerFilter` | Only needed for complex filter pages |

Second-pass execution recommendation:

1. Implement `AMISDataTable<TItem>` and migrate one `MasterData` page as pilot.
2. Implement `AMISStatusChip` and replace status chips in pilot page.
3. Implement `AMISEmptyState` and wire in pilot page empty flow.
4. Validate pilot, then batch-migrate similar list pages.
5. Implement dialog/input wrappers in parallel with CRUD-heavy pages.

---

## Files to Update After Each Component

- `src/BuildingBlocks/Blazor.UI/_Imports.razor` — add namespace
- `src/Playground/Playground.Blazor/_Imports.razor` — verify cascaded imports
- Replace direct `MudTable`/`MudDialog`/`MudChip` usage in `Playground.Blazor/Components/Pages/**` with the new wrappers

---

## Open Decisions (Resolve Before Build Sprint)

1. Should `AMISDataTable<TItem>` own search state internally, or receive it from parent for URL query persistence?
2. Should `AMISStatusChip` mapping be centralized in a configurable service or static dictionary?
3. Should `AMISFormDialog` include built-in `EditForm` + `FluentValidation` integration, or remain content-only shell?
4. Which page becomes the official migration pilot (`MasterData` suggested)?
