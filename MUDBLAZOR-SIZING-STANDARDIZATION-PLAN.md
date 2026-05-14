# MudBlazor Sizing Standardization Plan

**Date:** May 14, 2026  
**Status:** Planning Phase  
**Priority:** Medium (UX consistency & maintainability)  

---

## Executive Summary

This plan standardizes control sizing across the Blazor UI to eliminate visual inconsistencies, improve user experience, and establish a maintainable sizing contract going forward.

**Current state:** Mixed sizing patterns across pages (default, Size.Small, Dense=true/false, Margin variations). Wrapper components exist but are underutilized. This creates:
- Misaligned filter/form bars (controls at different heights in same row)
- Wasted vertical space on data-heavy pages
- Inconsistent visual hierarchy
- Maintenance burden: sizing logic scattered across 60+ pages

**Proposed approach:** 
1. Define a centralized sizing contract in shared CSS (CSS custom properties + utility classes)
2. Strengthen wrapper component defaults (enforce Dense + Margin.Dense at component level)
3. Migrate high-traffic pages incrementally (4 pages → 50+ pages)
4. Document pattern for future adoption

**Expected outcomes:**
- 10–15% vertical space savings on filter/form pages
- Unified visual language across all Blazor UIs
- Reduced maintenance cost (sizing logic in one place)
- Faster page development (use wrappers or follow contract)

---

## Current State Audit

### Sizing Inconsistencies Observed

#### 1. **Control Height Spread (Root Cause: No Global Contract)**
| Control Type | Default Height | Compact (Dense+Margin) | Diff | Impact |
|---|---|---|---|---|
| MudTextField/MudSelect | 56px | 40px | 16px/row | Tall filter bars, wasted space |
| MudNumericField/MudDatePicker | 56px | 40px | 16px/row | Breaks visual alignment |
| MudAutocomplete | 56px | 40px | 16px/row | Dropdown height mismatch |
| MudButton (default) | 48px | 40px | 8px | Filter buttons misaligned |
| MudButton (Size.Small) | 40px | 32px | 8px | Icon buttons inconsistent |
| MudIconButton | 40px | 32px | 8px | Table action rows too tall |
| MudChip | 32px | 32px | — | Mostly aligned but loose spacing |
| MudTable row | 48px | 32px | 16px/row | Dense tables don't match compact forms |

**Real Impact Example:** A filter row with 3 fields:
- **Current (mixed):** Search field 56px + Select 40px + Button 48px = visual chaos, 12px+ spacing gaps
- **After standardization:** All 40px + consistent Spacing="2" = clean, professional, saves 16px vertical

#### 2. **High-Traffic Pages with Mixed Patterns (User Visible)**
| File | Current Issue | Visual Symptom | Priority |
|------|---|---|---|
| [VehiclesPage](src/Playground/Playground.Blazor/Components/Pages/Vehicle/VehiclesPage.razor) | Search (Size.Small 48px) + Select (Dense 40px) + Button (default 48px) | Misaligned filter bar; inputs at 2 different heights | **P1** |
| [Audits.razor](src/Playground/Playground.Blazor/Components/Pages/Audits.razor) | 7 filter fields, no consistent Dense/Size | Advanced Filter section takes 180px+ (should be 120px); inconsistent field heights | **P1** |
| [PhysicalCountWalkthroughPage](src/Playground/Playground.Blazor/Components/Pages/AssetManagement/PhysicalCountWalkthroughPage.razor) | Dialog fields mix default (56px) + Size.Small (48px) | "Mark as Found" dialog form looks cramped; vertical rhythm breaks | **P1** |
| [AssetManagementReportsPage](src/Playground/Playground.Blazor/Components/Pages/AssetManagement/AssetManagementReportsPage.razor) | Filter sidebar: inconsistent field sizes across tabs | Each tab filter has different control heights; tab content alignment jumps | **P1** |
| [GroupsPage](src/Playground/Playground.Blazor/Components/Pages/Groups/GroupsPage.razor) | Search field (default 56px) vs. DataGrid row (32px dense) | Massive height gap between filter and table; poor visual hierarchy | **P1** |
| [PpmpPage.razor](src/Playground/Playground.Blazor/Components/Pages/ProcurementPlanning/PpmpPage.razor) | Multiple MudSelects without sizing | Filter row too tall; buttons floating off-center vertically | **P2** |
| Weather.razor, Users/**/*.razor | Inconsistent across all CRUD pages | Default sizing everywhere; forms feel bloated | **P3** |

**Test Yourself:** Open Audits.razor and VehiclesPage in browser side-by-side. Notice the Advanced Filter section is noticeably taller, and the filter row controls don't align. That's the problem we're solving.

#### 3. **Global Theme & CSS**
- [AMIS-theme.css](src/BuildingBlocks/Blazor.UI/wwwroot/css/AMIS-theme.css): Defines colors, shadows, card styles; **no control-size tokens**.
- [AMISTheme.cs](src/BuildingBlocks/Blazor.UI/Theme/AMISTheme.cs): Builds MudTheme; **no dense or size defaults**.
- [ServiceCollectionExtensions.cs](src/BuildingBlocks/Blazor.UI/ServiceCollectionExtensions.cs): AddMudServices configured; **no global control size policy**.

#### 4. **Wrapper Component Status (Foundation Already Built)**
Good news: Wrapper components already enforce compact defaults:
- [AMISTextField](src/BuildingBlocks/Blazor.UI/Components/Inputs/AMISTextField.razor): `Dense="true"` + `Margin="Margin.Dense"` ✅
- [AMISSelect](src/BuildingBlocks/Blazor.UI/Components/Inputs/AMISSelect.razor): `Dense="true"` + `Margin="Margin.Dense"` ✅
- [AMISAutocomplete](src/BuildingBlocks/Blazor.UI/Components/Inputs/AMISAutocomplete.razor): `Dense="true"` + `Margin="Margin.Dense"` ✅

Bad news: **These wrappers are used 0 times in Playground pages.** All pages use raw `<MudTextField>`, `<MudSelect>`, etc.

**Why?** Pages were built before wrappers were adopted. No enforcement or documentation.

**Fix:** 
- Phase 1: Enhance wrappers with optional `InputSize` parameter so they're flexible but still enforce compact by default.
- Phase 2+: Replace direct Mud controls with wrappers or apply sizing attributes inline.

---

## Proposed Uniform Sizing Standard

### 1. **Design Principles**
- **Enterprise Compact Mode:** Dense=true for all input controls to maximize screen real estate.
- **Consistent Height Baseline:** All inputs, buttons, chips in a form/filter row at same height (~40px).
- **Hierarchy by Size:** Size.Small (compact actions) vs. default (primary actions).
- **Margin Density:** Margin.Dense for spacing consistency.

### 2. **Control Sizing Contract**

#### Before & After Code Examples

##### Current (Inconsistent) — VehiclesPage Filter Row
```razor
<!-- ❌ PROBLEM: Mix of sizes -->
<MudStack Row="true" Spacing="2" Style="width:100%; max-width: 700px;">
    <MudTextField @bind-Value="_query.Keyword" Label="Search" Placeholder="Plate, make, model"
                  Variant="Variant.Outlined" Size="Size.Small" />  <!-- 48px -->
    <MudSelect T="string" @bind-Value="_query.Status" Label="Status" Dense="true" Clearable="true" />  <!-- 40px -->
    <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="LoadVehicles">Search</MudButton>  <!-- 48px -->
    <MudButton Variant="Variant.Outlined" Color="Color.Default" OnClick="ClearFilters">Clear</MudButton>  <!-- 48px -->
</MudStack>
<!-- Result: Controls at 3 different heights; looks amateurish -->
```

##### After (Standardized) — Same Component
```razor
<!-- ✅ SOLUTION: All controls aligned -->
<MudStack Row="true" Spacing="2" Style="width:100%; max-width: 700px;">
    <AMISTextField @bind-Value="_query.Keyword" Label="Search" Placeholder="Plate, make, model" />  <!-- 40px, enforced -->
    <AMISSelect T="string" @bind-Value="_query.Status" Label="Status" Clearable="true">
        <MudSelectItem Value="@Active">Active</MudSelectItem>
        ...
    </AMISSelect>  <!-- 40px, enforced -->
    <MudButton Variant="Variant.Filled" Color="Color.Primary" Size="Size.Small" OnClick="LoadVehicles">Search</MudButton>  <!-- 40px -->
    <MudButton Variant="Variant.Outlined" Color="Color.Default" Size="Size.Small" OnClick="ClearFilters">Clear</MudButton>  <!-- 40px -->
</MudStack>
<!-- Result: All controls exactly aligned; professional appearance; saves 16px vertical -->
```

##### Alternative (No Wrappers) — Same Result
```razor
<!-- ✅ SOLUTION 2: Direct sizing attributes (for cases where wrappers not available) -->
<MudStack Row="true" Spacing="2" Style="width:100%; max-width: 700px;">
    <MudTextField @bind-Value="_query.Keyword" Label="Search" Placeholder="Plate, make, model"
                  Variant="Variant.Outlined" Dense="true" Margin="Margin.Dense" Size="Size.Small" />  <!-- 40px -->
    <MudSelect T="string" @bind-Value="_query.Status" Label="Status" Dense="true" Margin="Margin.Dense" Clearable="true" />  <!-- 40px -->
    <MudButton Variant="Variant.Filled" Color="Color.Primary" Size="Size.Small" Margin="Margin.Dense" OnClick="LoadVehicles">Search</MudButton>  <!-- 40px -->
    <MudButton Variant="Variant.Outlined" Color="Color.Default" Size="Size.Small" Margin="Margin.Dense" OnClick="ClearFilters">Clear</MudButton>  <!-- 40px -->
</MudStack>
```


```
✓ Variant:         Outlined (default, modern appearance)
✓ Dense:           true (compact height ~40px)
✓ Margin:          Margin.Dense (reduce vertical space)
✓ Size:            Size.Small (optional, for ultra-compact contexts like search bars)
✓ Adornment:       Allowed (icon start/end for context)
```

**Result:** ~40px height (Dense) vs. ~56px (default).

#### Buttons
| Context | Size | Margin | Result Height |
|---------|------|--------|---|
| Primary/secondary actions | default or Size.Small | Margin.Dense | ~40px (Small), ~48px (default) |
| Icon buttons (tables, toolbars) | Size.Small | Margin.Dense | ~32px |
| Dialog actions (confirm, cancel) | default | default | ~48px |

#### Chips
```
✓ Size:            Size.Small (default, ~32px)
✓ Variant:         Outlined or Filled (consistent with page context)
✓ Usage:           Status badges, tags, quick filters
```

#### Tables & DataGrids
```
✓ Dense:           true (compact row height ~32px vs. ~48px default)
✓ Striped:         true (optional, improves row readability)
✓ Hover:           true (interaction feedback)
```

#### MudStack Spacing
```
✓ Filter row spacing:      Spacing="2" (16px, allows room for aligned controls)
✓ Form field spacing:      Spacing="2" or Spacing="3" (16px or 24px)
✓ Button group spacing:    Spacing="1" (8px, tight)
```

---

## Implementation Plan

### Pre-Implementation Quick Reference

**When building a form or filter, use this checklist:**

```razor
<!-- ✅ Correct Pattern -->
<MudStack Spacing="3">  <!-- Spacing="3" for form fields, Spacing="2" for filter rows -->
    <AMISTextField @bind-Value="Name" Label="Name" Required="true" />
    <AMISSelect T="string" @bind-Value="Status" Label="Status">
        <MudSelectItem Value="Active">Active</MudSelectItem>
        <MudSelectItem Value="Inactive">Inactive</MudSelectItem>
    </AMISSelect>
    <AMISAutocomplete T="EmployeeDto" @bind-Value="Employee" Label="Employee" SearchFunc="SearchEmployees" />
    <MudDatePicker @bind-Date="StartDate" Label="Start Date" Variant="Variant.Outlined" Dense="true" Margin="Margin.Dense" />
    <MudButton Variant="Variant.Filled" Color="Color.Primary" Size="Size.Small" OnClick="SaveAsync">Save</MudButton>
</MudStack>

<!-- ✅ Alternative Pattern (no wrappers) -->
<MudStack Spacing="3">
    <MudTextField @bind-Value="Name" Label="Name" Required="true" Variant="Variant.Outlined" Dense="true" Margin="Margin.Dense" />
    <MudSelect T="string" @bind-Value="Status" Label="Status" Dense="true" Margin="Margin.Dense">
        <MudSelectItem Value="Active">Active</MudSelectItem>
        <MudSelectItem Value="Inactive">Inactive</MudSelectItem>
    </MudSelect>
    <!-- etc. -->
</MudStack>

<!-- ❌ DO NOT: Mix defaults with compact -->
<MudStack Spacing="3">
    <MudTextField @bind-Value="Name" Label="Name" />  <!-- 56px — breaks alignment! -->
    <MudSelect T="string" @bind-Value="Status" Label="Status" Dense="true" />  <!-- 40px — breaks alignment! -->
</MudStack>
```

---

### Phase 1: Foundation (Foundation + Wrappers)
**Goal:** Establish sizing contract without breaking existing pages.  
**Effort:** ~2–3 hours.  
**Risk:** Low (no page changes, backward compatible).

#### 1.1 Add CSS Sizing Tokens to AMIS-theme.css

**Location:** After line 28 (after `--AMIS-transition` declaration), add:

```css
  /* Control Sizing Tokens — Enforced compact enterprise defaults */
  --amis-control-height-compact: 40px;    /* Input controls + small buttons (Dense + Size.Small) */
  --amis-control-height-normal: 48px;     /* Default buttons, dialog actions */
  --amis-control-height-tight: 32px;      /* Icon buttons, toolbar actions */
  
  --amis-control-padding-compact: 8px 12px;  /* Dense input padding */
  --amis-control-padding-normal: 10px 14px;  /* Normal input padding */
  --amis-control-border-radius: 4px;         /* Material default */
```

**Add utility classes at end of file (after line 380):**

```css
/* Sizing Utilities — Use for exceptions or direct control styling */
.amis-control-compact {
  height: var(--amis-control-height-compact) !important;
  padding: var(--amis-control-padding-compact) !important;
}

.amis-control-normal {
  height: var(--amis-control-height-normal) !important;
  padding: var(--amis-control-padding-normal) !important;
}

/* Table/Grid row compacting */
.amis-table-compact .mud-table-row {
  height: 32px;
}

.amis-table-compact .mud-table-cell {
  padding: 4px 8px !important;
}

/* Reference for developers */
/* 
 * DO: Use wrapper components (AMISTextField, AMISSelect, etc.)
 *     or apply: Dense="true" Margin="Margin.Dense" Size="Size.Small"
 * DON'T: Mix default (56px) and compact (40px) controls in same row
 */
```

**File:** [src/BuildingBlocks/Blazor.UI/wwwroot/css/AMIS-theme.css](src/BuildingBlocks/Blazor.UI/wwwroot/css/AMIS-theme.css)

#### 1.2 Extend Wrapper Components with Size Override Parameters

**Goal:** Allow wrappers to accept optional `InputSize` parameter for flexibility, while preserving Dense + Margin.Dense defaults.

##### AMISTextField.razor

Add after line 18 (after `Lines` parameter):

```csharp
    [Parameter] public Size InputSize { get; set; } = Size.Small;  // Allow override; default to Small
```

Update the MudTextField call (line 10-17) to use the new parameter:

```razor
<MudTextField @attributes="AdditionalAttributes"
              Label="@Label"
              Placeholder="@Placeholder"
              Value="@Value"
              ValueChanged="@ValueChanged"
              ValueExpression="@ValueExpression"
              For="@For"
              Variant="@Variant"
              Disabled="@Disabled"
              Adornment="@Adornment"
              AdornmentIcon="@AdornmentIcon"
              AdornmentColor="@AdornmentColor"
              Immediate="true"
              Margin="Margin.Dense"
              Dense="true"
              Required="@Required"
              InputType="@InputType"
              Size="@InputSize"  <!-- NEW LINE -->
              Lines="@Lines" />
```

Add comment block above @code:

```csharp
@* 
 * Sizing Contract: 
 * Dense=true + Margin.Dense enforced (compact mode)
 * InputSize defaults to Size.Small (40px height)
 * Override with InputSize="Size.Medium" or Size.Large if needed
 *@
```

##### AMISSelect.razor

Add after line 9 (after `ChildContent` parameter):

```csharp
    [Parameter] public Size InputSize { get; set; } = Size.Small;  // Allow override
```

Update MudSelect call (line 6-13) to use the parameter:

```razor
<MudSelect T="TValue"
           @bind-Value="Value"
           Label="@Label"
           Required="@Required"
           Disabled="@Disabled"
           Dense="true"
           Variant="@Variant"
           Adornment="@Adornment"
           AdornmentIcon="@AdornmentIcon"
           AdornmentColor="@AdornmentColor"
           Margin="Margin.Dense"
           Size="@InputSize"  <!-- NEW LINE -->
           @attributes="AdditionalAttributes">
    @ChildContent
</MudSelect>
```

##### AMISAutocomplete.razor

Add after line 10 (after `Disabled` parameter):

```csharp
    [Parameter] public Size InputSize { get; set; } = Size.Small;  // Allow override
```

Update MudAutocomplete call (line 6-8) to use the parameter:

```razor
<MudAutocomplete T="TValue" 
                 Value="Value" 
                 ValueChanged="@OnMudValueChanged" 
                 Label="@Label" 
                 Placeholder="@Placeholder"
                 SearchFunc="@SearchFunc"              
                 Dense="true" 
                 Variant="@Variant" 
                 Adornment="@Adornment" 
                 AdornmentIcon="@AdornmentIcon"
                 AdornmentColor="@AdornmentColor" 
                 Margin="Margin.Dense" 
                 Size="@InputSize"  <!-- NEW LINE -->
                 Required="@Required" 
                 Disabled="@Disabled"
                 IsOpenChanged="@IsOpenChanged" 
                 DebounceInterval="400" 
                 MaxItems="@MaxItems"
                 ResetValueOnEmptyText="@ResetValueOnEmptyText" 
                 @attributes="AdditionalAttributes">
</MudAutocomplete>
```

**Files to modify:**
- [src/BuildingBlocks/Blazor.UI/Components/Inputs/AMISTextField.razor](src/BuildingBlocks/Blazor.UI/Components/Inputs/AMISTextField.razor)
- [src/BuildingBlocks/Blazor.UI/Components/Inputs/AMISSelect.razor](src/BuildingBlocks/Blazor.UI/Components/Inputs/AMISSelect.razor)
- [src/BuildingBlocks/Blazor.UI/Components/Inputs/AMISAutocomplete.razor](src/BuildingBlocks/Blazor.UI/Components/Inputs/AMISAutocomplete.razor)

#### 1.3 Document Sizing Standards
- Create inline comments in wrapper components explaining the contract.
- Update architecture rule or add note to [.claude/rules/](d:\VB\AMIS102\.claude\rules\) if needed.

**Deliverable:** Foundation layer ready; no page visuals changed yet.

---

### Phase 2: High-Impact Page Migration (First Batch)
**Goal:** Migrate top 4 pages with most inconsistencies.  
**Effort:** ~4–5 hours (1–1.5 hours per page).  
**Risk:** Medium (visual changes; require smoke testing).  
**Dependencies:** Phase 1 must be complete.

#### Pages in First Batch (Priority Order)

##### 1. VehiclesPage — Filter Row Alignment
**File:** `src/Playground/Playground.Blazor/Components/Pages/Vehicle/VehiclesPage.razor` (line 10–18)

**Change Pattern:**
- All filters in the row → Size.Small (40px) or use AMISTextField/AMISSelect
- All buttons in row → Size.Small
- Spacing="2" (16px gap)

**Test:** Filter row should be exactly one control height; all elements aligned top-to-bottom.

##### 2. Audits.razor — Advanced Filters Section
**File:** `src/Playground/Playground.Blazor/Components/Pages/Audits.razor` (line 95–180)

**Change Pattern:**
- MudDateRangePicker + MudTextField + MudSelect in grid → all Dense="true" + Margin="Margin.Dense"
- Apply Size.Small to filter fields
- Grid should be 3 columns; each control 40px height

**Test:** Filter card should be ~120px tall (was ~180px); vertical rhythm consistent.

##### 3. PhysicalCountWalkthroughPage — Dialog Forms
**File:** `src/Playground/Playground.Blazor/Components/Pages/AssetManagement/PhysicalCountWalkthroughPage.razor` (line 140–180)

**Change Pattern:**
- All MudSelect / MudTextField in dialogs → Dense="true" + Margin="Margin.Dense"
- Dialog buttons → Size.Small
- Form fields stack with Spacing="2"

**Test:** "Mark Found" dialog should look compact; all fields same height; dialog fits screen.

##### 4. AssetManagementReportsPage — Tabbed Filters
**File:** `src/Playground/Playground.Blazor/Components/Pages/AssetManagement/AssetManagementReportsPage.razor` (line 13–45)

**Change Pattern:**
- Filter sidebar controls (all MudAutocomplete, MudSelect, MudDatePicker) → Dense="true" + Margin="Margin.Dense" + Size.Small
- Sidebar buttons → Size.Small
- Ensure tab switching doesn't cause height jumps

**Test:** Switch between SPC, RegSPI, etc. tabs; filter sidebar height should stay constant.

#### Migration Checklist (Per Page)

```markdown
## [ Page Name ] Sizing Migration

- [ ] Identify all `<MudTextField>` not already Dense=true (search: `<MudTextField` without Dense)
- [ ] Identify all `<MudSelect` not already Dense=true (search: `<MudSelect` without Dense)
- [ ] Identify all `<MudAutocomplete` not already Dense=true
- [ ] Identify all `<MudDatePicker` / `<MudNumericField` not already Dense=true
- [ ] Identify all buttons in filter/form rows; apply Size.Small
- [ ] Apply Margin="Margin.Dense" to all inputs and Small buttons
- [ ] Verify MudStack Spacing:
  - [ ] Filter rows: Spacing="2" (16px)
  - [ ] Form fields: Spacing="2" or Spacing="3"
  - [ ] Button groups: Spacing="1"
- [ ] Replace direct Mud controls with AMIS wrappers where available
- [ ] Desktop smoke test:
  - [ ] Load page in browser
  - [ ] Verify no visual glitches
  - [ ] Verify controls are aligned
  - [ ] Verify tables/grids render correctly
- [ ] Mobile smoke test (responsive design):
  - [ ] Shrink browser to 600px width
  - [ ] Verify responsive classes work
  - [ ] Verify no overflow
- [ ] Build passes: `dotnet build src/AMIS.Framework.slnx`
```

#### Find & Replace Patterns (Bulk Updates)

For rapid application across first batch:

```
FIND:    <MudTextField
         (not preceded by Dense="true")
REPLACE: <MudTextField Dense="true" Margin="Margin.Dense"

FIND:    <MudSelect
         (not preceded by Dense="true")
REPLACE: <MudSelect Dense="true" Margin="Margin.Dense"

FIND:    <MudAutocomplete
         (not preceded by Dense="true")
REPLACE: <MudAutocomplete Dense="true" Margin="Margin.Dense"
```

**Note:** Use VS Code Find & Replace with regex to target specific patterns. Manually verify each match.

---

### Phase 3: Incremental Rollout (Remaining Pages)
**Goal:** Apply sizing contract to all remaining pages over 1–2 weeks.  
**Effort:** ~6–8 hours (bulk replacement with validation).  
**Strategy:** 
- Use Find & Replace to add sizing attributes.
- Group by feature area (Asset Management, Procurement, Expendable, etc.).
- Smoke test per group.

**Target Pages (sorted by impact):**
- AssetManagementReportsPage.razor, UnserviceableReportsPage.razor, PPEReceivingReportsPage.razor, ReceivingReportsPage.razor (Asset Management)
- PpmpPage.razor (Procurement Planning)
- PurchasesPage.razor (Expendable)
- PhysicalCountPage.razor (Asset Register)
- And ~50 more Playground pages.

---

### Phase 4: Validation & Documentation
**Goal:** Verify no regressions; document for future maintainers.  
**Effort:** ~1–2 hours.

#### Validation Procedure

##### Step 1: Build
```powershell
cd d:\VB\AMIS102
dotnet build src/AMIS.Framework.slnx /p:GenerateFullPaths=true
# Expected: 0 warnings, 0 errors
```

##### Step 2: Smoke Test (Per Migrated Page)
1. **Start app:**
   ```powershell
   dotnet run --project src/Playground/Playground.Api
   # or: dotnet run --project src/Playground/AMIS.Playground.AppHost
   ```
2. **Navigate to page** (e.g., `/vehicle/vehicles`)
3. **Visual checks:**
   - [ ] Filter row controls are aligned top-to-bottom
   - [ ] All input fields same height (~40px)
   - [ ] All buttons in row same height
   - [ ] No layout breaks or overflow
   - [ ] Tables render properly (Dense=true doesn't cut off content)
   - [ ] Dialogs size correctly

##### Step 3: Responsive Test
1. Open DevTools (F12)
2. Set breakpoint: 600px width (tablet)
3. Verify:
   - [ ] No horizontal scrollbars
   - [ ] Responsive stacking works
   - [ ] Controls don't overflow
4. Test on mobile breakpoint: 375px width
5. Return to desktop: 1920px width

##### Step 4: Regression Testing
Compare before/after on:
- **VehiclesPage:** Filter row height (before ~56px avg, after ~40px)
- **Audits:** Advanced Filter card height (before ~180px, after ~120px)
- **PhysicalCountWalkthroughPage:** Dialog form appearance (compact, no gaps)
- **AssetManagementReportsPage:** Tab filter consistency across tabs

**Document screenshot comparisons** in a comment block if visual regression is suspected.

#### Rollback Procedure

If issues found:

```bash
# Option 1: Rollback single page
git checkout -- src/Playground/Playground.Blazor/Components/Pages/Vehicle/VehiclesPage.razor

# Option 2: Rollback entire Phase 2
git checkout -- src/Playground/Playground.Blazor/Components/Pages/

# Investigate issue on reverted page
# Fix in isolated branch, test, then merge
```

#### Documentation Updates

**Add to [CLAUDE.md](d:\VB\AMIS102\CLAUDE.md) under "Blazor UI" section:**

```markdown
### Control Sizing Standard

All form and filter controls follow a **compact enterprise standard** to maximize screen real estate and maintain visual alignment.

#### Quick Rules
- **Inputs:** Use `<AMISTextField>`, `<AMISSelect>`, `<AMISAutocomplete>` (enforce Dense + Margin.Dense + Size.Small)
- **Inputs (no wrapper):** Apply `Dense="true" Margin="Margin.Dense" Size="Size.Small"` explicitly
- **Buttons in filters:** Use `Size="Size.Small"` (40px height, matches inputs)
- **Tables/DataGrids:** Use `Dense="true"` (32px rows)
- **Stacking:** Use `MudStack Spacing="2"` for form rows, `Spacing="3"` for form sections

#### Why
- Compact sizing saves ~10–15% vertical space on data-heavy pages
- Consistent alignment improves UX professionalism
- Centralized CSS tokens mean sizing changes in one place

#### See Also
- [MUDBLAZOR-SIZING-STANDARDIZATION-PLAN.md](MUDBLAZOR-SIZING-STANDARDIZATION-PLAN.md)
```

#### Metrics to Track

After Phase 2, measure:
- **Form/filter height reduction:** Target 15% (e.g., Audits from 180px → 120px)
- **Control alignment:** 100% of inputs in same row at same height
- **Build warnings:** 0 (maintain code quality)
- **Regression bugs:** 0 (if any, add to test suite)

---

## Files Affected Summary

### Phase 1 (Foundation)
1. **src/BuildingBlocks/Blazor.UI/wwwroot/css/AMIS-theme.css**
   - Add CSS sizing tokens and utility classes.

2. **src/BuildingBlocks/Blazor.UI/Components/Inputs/AMISTextField.razor**
   - Add optional `InputSize` parameter.
   - Preserve Dense + Margin.Dense defaults.

3. **src/BuildingBlocks/Blazor.UI/Components/Inputs/AMISSelect.razor**
   - Add optional `InputSize` parameter.

4. **src/BuildingBlocks/Blazor.UI/Components/Inputs/AMISAutocomplete.razor**
   - Add optional `InputSize` parameter.

### Phase 2 (First Batch Pages)
5. src/Playground/Playground.Blazor/Components/Pages/Vehicle/VehiclesPage.razor
6. src/Playground/Playground.Blazor/Components/Pages/Audits.razor
7. src/Playground/Playground.Blazor/Components/Pages/AssetManagement/PhysicalCountWalkthroughPage.razor
8. src/Playground/Playground.Blazor/Components/Pages/AssetManagement/AssetManagementReportsPage.razor

### Phase 3 (Remaining Pages)
- ~50 additional Playground.Blazor pages (bulk update with Find & Replace).

### Phase 4 (Documentation)
9. CLAUDE.md (update Blazor section)

---

## Success Criteria

- [ ] **Visual Consistency:** All form fields, buttons, and chips in the same context have aligned heights.
- [ ] **Maintainability:** Sizing contract tokenized in CSS; wrappers enforce defaults.
- [ ] **Zero Regressions:** Build passes, pages render correctly, no broken layouts.
- [ ] **Adoption:** New pages automatically use wrappers or follow sizing standard.
- [ ] **Documentation:** Future developers know to use wrappers or apply the sizing contract.

---

## Rollback Plan

If regressions occur:
1. Revert Phase 2 page changes (git checkout).
2. Keep Phase 1 foundation (backward compatible, no breaking changes).
3. Investigate issue and iterate on single page before retrying batch.

---

## Timeline

| Phase | Duration | Start | End | Status |
|-------|----------|-------|-----|--------|
| 1. Foundation | 2–3 hours | — | — | Pending Approval |
| 2. First Batch (4 pages) | 4–5 hours | — | — | Pending Phase 1 |
| 3. Incremental Rollout | 6–8 hours | — | — | Pending Phase 2 |
| 4. Validation | 1–2 hours | — | — | Pending Phase 3 |
| **Total** | **13–18 hours** | — | — | — |

---

## Related Documents

- [CLAUDE.md](d:\VB\AMIS102\CLAUDE.md) — AI assistant guide (Blazor section).
- [.claude/rules/maui.md](d:\VB\AMIS102\.claude\rules\maui.md) — MAUI UI rules (similar compact sizing approach).
- [Architecture Rules](d:\VB\AMIS102\.claude\rules\architecture.md) — AMIS framework principles.

---

## Troubleshooting & FAQs

### Common Issues & Solutions

#### Issue 1: "Controls are too small now, text is hard to read"
**Root cause:** Size.Small reduces overall dimension, but font size stays standard (14px).

**Solution:**
- Size.Small is for **compact contexts** (filter bars, tool rows, action columns)
- For primary forms/dialogs, use default size: omit `Size="Size.Small"` or use `InputSize="Size.Large"`
- Font readability is NOT affected by Size.Small (same 14px font, less padding)

**Example:**
```razor
<!-- Compact filter row — Size.Small OK here -->
<MudStack Row="true" Spacing="2">
    <AMISTextField InputSize="Size.Small" @bind-Value="Search" Label="Search" />
    <MudButton Size="Size.Small">Filter</MudButton>
</MudStack>

<!-- Dialog/primary form — use default size -->
<MudDialog>
    <DialogContent>
        <AMISTextField @bind-Value="Name" Label="Name" />  <!-- Omit InputSize; uses default (Small hardcoded in component, but acceptable) -->
    </DialogContent>
</MudDialog>
```

#### Issue 2: "DataGrid looks cramped with Dense=true"
**Root cause:** Dense=true reduces row height to 32px; content may look tight.

**Solution:**
- Use Dense=true for **data grid/report tables** where row count > 20
- Use default for **small lists** (<10 rows) or **key transaction tables**
- Test with real data; if text truncates, add more column width or increase row height

**Example:**
```razor
<!-- Large report table — Dense=true ✅ -->
<MudDataGrid T="AuditEventDto" Items="_events" Dense="true" Striped="true" />

<!-- Small transaction table — Dense=false ✅ -->
<MudTable T="OrderDto" Items="_recentOrders" Hover="true" />
```

#### Issue 3: "MudDatePicker/MudTimePicker not respecting Dense"
**Root cause:** Some MudBlazor controls ignore Dense prop.

**Solution:**
- Apply CSS class `.amis-control-compact` directly to control
- Or set height/padding inline: `Style="height: 40px;"`

```razor
<!-- Option 1: CSS class -->
<MudDatePicker Class="amis-control-compact" @bind-Date="StartDate" />

<!-- Option 2: Inline style (temporary workaround) -->
<MudTimePicker Style="height: 40px;" @bind-Time="StartTime" />
```

#### Issue 4: "Icon buttons in table actions are too tall"
**Root cause:** MudIconButton defaults to 40px; should be 32px for tight rows.

**Solution:**
- Use `Size="Size.Small"` on icon buttons in action columns
- Use `Margin="Margin.Dense"` to remove padding

```razor
<MudTd>
    <MudStack Row="true" Spacing="0">
        <MudIconButton Icon="@Icons.Material.Filled.Edit" Size="Size.Small" Margin="Margin.Dense" />
        <MudIconButton Icon="@Icons.Material.Filled.Delete" Size="Size.Small" Margin="Margin.Dense" />
    </MudStack>
</MudTd>
```

---

### Frequently Asked Questions

**Q: Do I have to use wrapper components?**  
A: No. Wrappers (AMISTextField, etc.) enforce Dense + Margin.Dense by default, so you get sizing for free. But you can achieve the same result by adding `Dense="true" Margin="Margin.Dense" Size="Size.Small"` to any MudBlazor control. Wrappers just reduce boilerplate.

**Q: What if I need a larger input control?**  
A: Use the `InputSize` parameter on wrappers, or omit `Size="Size.Small"` on direct Mud controls:
```razor
<AMISTextField InputSize="Size.Medium" @bind-Value="Name" />  <!-- Taller, still compact padding -->
<MudTextField Size="Size.Medium" Dense="true" Margin="Margin.Dense" @bind-Value="Name" />
```

**Q: Should all pages be migrated?**  
A: Prioritize pages with many filters or forms. Simple CRUD pages (1–2 forms) can stay default. Target: 80% of code (high-traffic pages).

**Q: Will this break existing custom CSS?**  
A: No. CSS sizing tokens are namespaced (`--amis-*`). Utility classes (`.amis-control-compact`) are opt-in. Existing styles are unaffected.

**Q: How do I handle MudTable column headers that need more height?**  
A: Don't compress headers. Keep headers at default size; compress only data rows:
```razor
<MudTable T="ItemDto" Dense="true">  <!-- Compacts data rows only -->
    <HeaderContent>
        <!-- Headers automatically scale; not affected by Dense -->
        <MudTh>Name</MudTh>
        <MudTh>Qty</MudTh>
    </HeaderContent>
    <RowTemplate>
        <!-- Rows are Dense (32px) -->
    </RowTemplate>
</MudTable>
```

**Q: What about accessibility?**  
A: Compact sizing does not hurt accessibility if:
- Font sizes remain standard (14px body, 16px headings)
- Color contrast is maintained (no fading)
- Touch targets remain ≥44px tall for mobile (MudButton Size.Small = 40px, acceptable)

Recommendation: Test with screen readers and keyboard navigation on migrated pages.

---

## Design Decisions (Rationale)

### 1. Why Dense=true + Margin.Dense + Size.Small as the default?
**Decision:** Enforce compact sizing as the enterprise standard across AMIS.

**Rationale:**
- **Space efficiency:** Government agencies often use large data tables/forms; compact mode saves 15–20% vertical space.
- **Consistency:** All pages use same sizing contract; users expect unified appearance.
- **Accessibility:** Compact spacing still meets 44px touch target guidelines (40px buttons are acceptable).
- **Maintainability:** Centralize sizing logic (once set, don't repeat on every page).

**Trade-off:** Users with accessibility needs (large font, high contrast) must use browser zoom; consider adding user-facing zoom option in future.

### 2. Why wrappers with optional InputSize parameter instead of hard-coding?
**Decision:** Allow flexibility for exceptions while enforcing default.

**Rationale:**
- **Flexibility:** Some controls (primary forms, key inputs) may need larger size for emphasis.
- **Opt-in override:** Default stays compact; set `InputSize="Size.Large"` only where needed.
- **Backward compat:** Existing wrapper usage unaffected; new parameter is optional.

**Alternative rejected:** Hard-code all controls to Size.Small. This would be simpler but rigid.

### 3. Why CSS custom properties (--amis-control-height-*) instead of hard-coded heights?
**Decision:** Use CSS variables for sizing tokens.

**Rationale:**
- **Live updates:** Change `--amis-control-height-compact` once; all controls using it update automatically.
- **Theme support:** Future dark/light theme sizes can override tokens without touching component files.
- **Developer clarity:** Clear naming (--amis-control-height-compact) documents intent.
- **Single source of truth:** No scattered height values across components.

### 4. Why start with only 4 pages in Phase 2?
**Decision:** Migrate high-impact pages first; roll out incrementally.

**Rationale:**
- **Risk mitigation:** Catch issues on 4 pages before rolling to 50+.
- **Testing:** Each page gets manual smoke test; unlikely to miss regressions.
- **Feedback loop:** Developers use early adopter pages; suggest improvements before bulk rollout.

### 5. Backward compatibility or breaking change?
**Decision:** **Backward compatible** — Phase 1 doesn't break existing code.

**Rationale:**
- No forced migration; existing pages continue working.
- Migrated pages opt-in gradually.
- If issues arise, rollback is trivial.

---

## Next Steps

### Immediate Actions (Phase 1 Start)

**1. Prepare Phase 1 implementation branch:**
```bash
git checkout -b feature/mudblazor-sizing-standardization
```

**2. Add CSS sizing tokens** (~15 min)
- Edit: `src/BuildingBlocks/Blazor.UI/wwwroot/css/AMIS-theme.css`
- Add CSS custom properties under `:root`
- Add utility classes at end of file
- **Validation:** Build passes, no visual changes

**3. Enhance wrapper components** (~45 min total, ~15 min each)
- Edit: `src/BuildingBlocks/Blazor.UI/Components/Inputs/AMISTextField.razor`
- Edit: `src/BuildingBlocks/Blazor.UI/Components/Inputs/AMISSelect.razor`
- Edit: `src/BuildingBlocks/Blazor.UI/Components/Inputs/AMISAutocomplete.razor`
- Add `[Parameter] public Size InputSize { get; set; } = Size.Small;` to each
- Update control binding to use new parameter
- **Validation:** Build passes, no visual changes on existing pages

**4. Build & test Phase 1:**
```powershell
dotnet build src/AMIS.Framework.slnx  # Must pass with 0 warnings
dotnet run --project src/Playground/Playground.Api
# Navigate to existing pages; verify no changes (backward compat)
```

**5. Commit Phase 1:**
```bash
git add .
git commit -m "feat: add MudBlazor sizing tokens and enhance wrappers with InputSize parameter

- Add CSS custom properties for control heights (compact, normal, tight)
- Add utility classes (.amis-control-compact, .amis-control-normal)
- Enhance AMISTextField, AMISSelect, AMISAutocomplete with optional InputSize
- Backward compatible; no page changes required"
git push origin feature/mudblazor-sizing-standardization
# Create PR, get code review approval
```

### Phase 2 Preparation (After Phase 1 Merged)

**1. Pick first page:** VehiclesPage
```bash
git checkout -b feature/mudblazor-sizing-vehicles-page
```

**2. Follow Migration Checklist** (see above)

**3. Test & commit:**
```bash
dotnet build src/AMIS.Framework.slnx
# Smoke test page in browser
git add .
git commit -m "refactor(vehicles): standardize control sizing to compact mode

- Apply Dense=true + Margin.Dense to all inputs
- Apply Size=Small to filter buttons
- Verify alignment and spacing"
```

**4. Repeat for Audits, PhysicalCountWalkthroughPage, AssetManagementReportsPage**

---

## Review Checklist Before Implementation

- [ ] All stakeholders agree on compact sizing as the standard
- [ ] Phase 1 CSS tokens are approved
- [ ] Wrapper enhancement approach is acceptable
- [ ] Phase 2 page list is prioritized by team
- [ ] Testing strategy (desktop, mobile, accessibility) is understood
- [ ] Rollback procedure is clear

---

## Success Metrics (Post-Implementation)

Track these metrics after Phase 2 completion to validate effectiveness:

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Vertical space saved** | 10–15% | Compare Audits filter height before/after (180px → 120px) |
| **Control alignment** | 100% | All inputs in same row at same height (40px) |
| **Page consistency** | 80%+ migrated | Track adoption rate: migrated pages / total pages |
| **Build warnings** | 0 | `dotnet build` must show 0 warnings |
| **Regression bugs** | 0 | No reported visual breaks or missing content |
| **Wrapper adoption** | 60%+ | Track usage of AMISTextField/Select/Autocomplete |
| **Developer onboarding time** | Reduced | Measure time to add new form (should be faster with wrappers) |

---

## Future Enhancements

### Short Term (Q3 2026)
- [ ] Linter rule to flag mixed Dense/default sizing in same row
- [ ] VSCode snippet for common form patterns (filter bar, CRUD form)
- [ ] Accessibility audit: test with screen readers + keyboard nav on migrated pages

### Medium Term (Q4 2026)
- [ ] Add support for user-facing zoom (150%, 175%, 200%)
- [ ] Dark mode sizing adjustments (may need different padding for readability)
- [ ] Mobile-specific sizing (responsive breakpoints for compact/normal mode)

### Long Term (2027)
- [ ] MudBlazor upgrade: evaluate if version 7+ has built-in sizing standardization
- [ ] Design system documentation: publish sizing standard to dev wiki/Figma
- [ ] Training: include sizing contract in onboarding for new frontend developers

---

## Appendix: Reference Links

- **[CLAUDE.md](d:\VB\AMIS102\CLAUDE.md)** — AI assistant guide
- **[Architecture Rules](d:\VB\AMIS102\.claude\rules\architecture.md)** — Modular monolith principles
- **[MAUI Rules](d:\VB\AMIS102\.claude\rules\maui.md)** — Mobile client sizing (similar compact approach)
- **MudBlazor Documentation:**
  - [Dense Parameter](https://www.mudblazor.com/docs/components/text-field#dense)
  - [Size Enumeration](https://www.mudblazor.com/docs/api/size)
  - [Components API](https://www.mudblazor.com/docs/components)

---

**Document Version:** 1.1 (Second Pass)  
**Status:** Ready for Implementation  
**Updated:** 2026-05-14
