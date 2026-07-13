# UI Design — AMIS Assistant (RAG PoC)

> Status: **Planned — not yet implemented.** Approved UI design; **replaces Phase 6 (formerly Phase 5)** of
> [RAG-Integration-PoC-Expendable-Module.md](RAG-Integration-PoC-Expendable-Module.md).
> Backend phases 0–5 of that plan are unchanged and must land first.
> Date planned: 2026-07-14 · **Revised 2026-07-14** — assistant promoted out of Expendable into its own module
> and extended to **AssetRegister**, so it is now a **cross-cutting** surface, not an Expendable page.

## Context

The backend plan specifies an assistant that answers across **Expendable** (stock, products, supply requests) and **AssetRegister** (assets, custodians, book value), plus semantic search over Expendable products. Its original Phase 5 was one line deep — a hand-written API client, one chat page, one nav link. That left real gaps:

- Of the three endpoints the PoC builds (`/assistant/ask`, `/products/semantic-search`, `/products/embeddings/rebuild`), **only one gets a UI**. Semantic search — half the point of the PoC — would be invisible to users, and the rebuild endpoint would be reachable only via Scalar.
- The model answers in **markdown**. A stock-level or asset-ledger answer is a markdown table. Rendered as the plan implies (`pre-wrap` text), it arrives as raw `| pipes |` and literal `**asterisks**` — the assistant looks broken at exactly the moment it's doing its job.
- The assistant is most useful *while looking at inventory or the asset register*, but a dedicated page forces the user to navigate away from whatever they were doing.

Decisions confirmed with the user:

1. **Full scope** — assistant page + Products page hooks + a global launcher reachable from any page.
2. **Reuse the existing markdown renderer** rather than adding a dependency or shipping raw text.
3. **Assistant bubbles** for the transcript, not the Slack-style rows the Chat module uses.

---

## Design overview — three surfaces, one panel

The core move: build the conversation **once** as a self-contained component, then host it in two places. No chat markup is duplicated.

```
                        AssistantPanel.razor                ← the ONLY chat implementation
                 (transcript · thinking state · composer)
                              │
      ┌───────────────────────┼───────────────────────┐
      │                       │                       │
 Global launcher        Full page              (state shared via)
 MudFab → right         /assistant              IAssistantConversationState
 MudDrawer                                      (scoped, per circuit)
 [any page]             [top-level nav link]
```

Because both hosts read the same scoped `IAssistantConversationState`, a user can ask a question in the drawer, hit "Open full view", and **the conversation is still there**. That is the payoff for the shared-state indirection, and it mirrors the established `IUserProfileState` pattern (`.claude/rules/blazor.md` → "Shared Session State Pattern").

**The assistant is cross-cutting, not an Expendable feature.** It answers asset questions too, so it lives at `/assistant` with a **top-level nav link** — not buried in the Expendable nav group. The global FAB already assumed cross-cutting reach.

Semantic search is different: it is Expendable-only, so it surfaces where users already hunt for products — on `ProductsPage.razor` — rather than on a page of its own.

---

## 1. `AssistantPanel.razor` — the conversation

New: `src/Host/AMIS.Blazor/Components/Pages/Assistant/AssistantPanel.razor` (+ `.razor.css`).

Renders in whatever height its host gives it (`height:100%`), so the drawer and the page both work without a second layout.

### Transcript — bubbles

Reuse the **`flex-direction: column-reverse` + `.Reverse()`** trick from `ChatPage.razor` (~line 109): newest turn pins to the bottom with **zero JS interop**. Do not add a scroll-into-view JS call.

- **User turn** — right-aligned, `rgba(var(--mud-palette-primary-rgb), .10)` tint. This is the same formula `.hero-card` and `.AMIS-page-header` already use, so it survives dark mode with no override.
- **Assistant turn** — left-aligned, `var(--mud-palette-surface)` background, `1px solid var(--mud-palette-divider)` border, with a `MudAvatar` bot glyph.
- **Tool chips** — under each assistant turn, one `MudChip` (`Size.Small`, `Variant.Outlined`) per entry in the answer DTO's `ToolsUsed`. This is the PoC's honesty surface: it shows the answer came from `get_stock_levels` and `get_asset_accountability`, not from the model's imagination. On a cross-module answer the chips visibly come from **both** modules — that is the demo.
- All colors come from **MudBlazor palette vars**, never from `--AMIS-card-bg` / `--AMIS-text-primary` in `AMIS-theme.css` — those are hard-coded light values (`#ffffff`, `#1a1a2e`) and would produce white-on-white bubbles in dark mode.

### Thinking state — the 10–30 second problem

Non-streamed tool loops take 10–30 s, and **cross-module questions chain more tool calls** (`MaxToolIterations` is 8), so the tail is longer still. A static spinner for 30 s reads as a hang.

- A placeholder **assistant bubble** appears immediately, containing `MudProgressCircular` (`Size.Small`) + an **elapsed-seconds counter** driven by a `PeriodicTimer` — `Thinking… 12s`. Motion plus a rising number is what distinguishes "working" from "dead".
- Composer disabled while `IsThinking`.
- Streaming is an explicit follow-up in the backend plan; this design is what makes the non-streamed wait tolerable until then.

### Empty state

`AMISEmptyState` (`BuildingBlocks/Blazor.UI/Components/Feedback/AMISEmptyState.razor`) with `Icon="@Icons.Material.Outlined.SmartToy"`, and its `ChildContent` slot holding **suggested-prompt chips** that fill and send on click:

> "How many ballpens are in stock?" · "Find items for printing" · "Who is accountable for property no 2024-001?" · **"What has been issued to Juan Dela Cruz?"**

These are chosen deliberately, one per capability:
- *"Find items for printing"* has **no substring overlap** with "Bond Paper A4" — it only works via `semantic_product_search`, demonstrating the embedding thesis in one click.
- *"What has been issued to Juan Dela Cruz?"* spans **Expendable supply requests and AssetRegister property accountability** — the cross-module answer that two separate assistants could not produce.

### Composer

**Raw `MudTextField`**, not `AMISTextField` — the AMIS wrappers splat unmatched attributes into an untyped dictionary and `OnKeyDown` never binds to MudBlazor's `EventCallback<KeyboardEventArgs>`. `ChatPage.razor` (~line 228) already works around this. Set `Dense="true" Margin="Margin.Dense"` manually to hold the 40 px compact baseline.

Enter sends, Shift+Enter newlines — copy `OnComposerKeyDown` from ChatPage. Send button is `AMISButton` (wrappers are fine for buttons), disabled on `IsThinking || whitespace`.

A **"New conversation"** `AMISIconButton` clears the transcript. This matters because history is replayed to the model on every turn and the validator caps it at 10 turns — the user needs a way to drop stale context.

### Error handling

| Case | UI |
|---|---|
| 403 | Inline `MudAlert Severity.Warning` — "You don't have permission to use the assistant." Never a snackbar; it's a permanent state, not an event. |
| Network / 5xx | `Snackbar.Add(..., Severity.Error)` **and** the failed turn stays in the transcript with a retry affordance. |
| Model declines a tool it can't access | Render the model's own text. **Do not** special-case this into an error — a user lacking `Accountability.View` should simply be told the assistant can't answer, which is correct behaviour, not a failure. |
| Model returns empty | Render the assistant's own "I found nothing" text — the system prompt already guarantees it. |

`IsThinking` is cleared in `finally`, always.

---

## 2. Global launcher — `MudFab` + right drawer

Modify `src/Host/AMIS.Blazor/Components/Layout/AMISLayout.razor`. Inside the authenticated branch (`else`, ~line 60), within `<MudLayout>`:

- A `MudFab` (`Icons.Material.Filled.SmartToy`, `Color.Primary`), fixed bottom-right, `z-index` above content and clear of the `.AMIS-footer`.
- A right-anchored `MudDrawer` (`Anchor="Anchor.End"`, `Variant="DrawerVariant.Temporary"`, `Width="420px"`) hosting `<AssistantPanel />` plus a header with a close button and an "Open full view" link to `/assistant`.
- **Both gated** on `UserProfileState.Permissions.Contains(AssistantPermissions.Assistant.Use)`. `AMISLayout` already injects `IUserProfileState` and already re-renders on `OnProfileChanged` (~line 194), so the FAB appears as soon as permissions land — no new plumbing.

## 3. Shared state — `IAssistantConversationState`

New: `src/Host/AMIS.Blazor/Services/AssistantConversationState.cs`. Follows `IUserProfileState` exactly (scoped, event-based), per `.claude/rules/blazor.md`:

```csharp
internal interface IAssistantConversationState
{
    IReadOnlyList<AssistantTurn> Turns { get; }
    bool IsThinking { get; }
    event Action? OnChanged;
    void AddTurn(AssistantTurn turn);
    void SetThinking(bool thinking);
    void Clear();
}
```

Registered `AddScoped` in `Program.cs` alongside the other state services (~line 62). Holds the transcript for the circuit's lifetime; nothing is persisted server-side (the API is stateless single-turn with client-side history replay — the panel sends the **last 10 turns** to satisfy the validator).

## 4. Products page — semantic search + rebuild

Modify `src/Host/AMIS.Blazor/Components/Pages/Expendable/ProductsPage.razor`. This is the **only** Expendable-specific UI in the PoC — semantic search is Expendable-only by design.

**Smart-search toggle.** A `MudSwitch`/`MudToggleIconButton` ("Smart search", `Icons.Material.Outlined.AutoAwesome`) next to the existing search field in the `AMISPageHeader` `ActionContent` (~line 19).

The page already drives its table through `ServerData`. Keep that — **branch inside the existing delegate**:

```csharp
// _smartSearch on  → semantic endpoint, ranked, single page
// _smartSearch off → existing ILike SearchProductsAsync (unchanged)
```

Semantic hits come back ranked and un-paged, so return them as one page with `TotalItems = hits.Count`. This sidesteps `AMISDataTable`'s `Items`-XOR-`ServerData` constraint entirely — no runtime mode flip, no second table.

When smart search is on, reveal a **"Match"** column rendering `Score` as a percentage (`MudProgressLinear` or a chip). It is the only visible proof that ranking is semantic rather than lexical, and it hides again when the toggle is off. Status/category/supplier filters stay visible but are **disabled** while smart search is on (the endpoint doesn't accept them) — with a tooltip saying so, rather than silently ignoring them.

**Rebuild embeddings.** An overflow `MudMenu` (`MoreVert`) in the same header, gated on `ExpendablePermissions.SearchIndex.Rebuild`, with one item: **"Rebuild search index"**. It calls `AMISDialogService.ShowConfirmAsync` (already exists — `BuildingBlocks/Blazor.UI/Components/Dialogs/AMISDialogService.cs`; do **not** hand-roll another confirm dialog), then hits the rebuild endpoint and reports the returned counts via snackbar: *"Search index rebuilt — 142 updated, 8 removed."*

## 5. Markdown rendering — extract, don't add a dependency

`src/Host/AMIS.Blazor/Services/Help/HelpContentService.cs` (~line 124) already contains a complete markdown→HTML converter (headings, bold, italic, code, lists, fenced blocks, **tables**, blockquotes, hr) that HTML-escapes input first. It is `private` and hard-wired to rewrite links to `/help/...` URLs.

**Extract the core** into `src/Host/AMIS.Blazor/Services/Markdown/MarkdownRenderer.cs` (`IMarkdownRenderer`, registered singleton). Move `ToHtml`, `AppendTable`, `Inline`, `Escape`, and the `[GeneratedRegex]` members across, parameterized by:

- `RootClass` — `help-article` for Help, `assistant-md` for the assistant (so the assistant doesn't inherit the help article's 1.9 rem `h1`).
- `LinkResolver` delegate — Help passes its existing `ResolveHelpUrl`; the assistant passes a **restrictive** resolver.
- `AllowImages` — `true` for Help, **`false`** for the assistant.

`HelpContentService` then keeps only its file-loading and slug-normalizing logic and delegates rendering. **Its rendered output must be byte-identical** — the Help pages are the regression check.

### Hardening required for LLM output (do not skip)

The existing converter is safe for *authored* content but has two holes that only matter once the input is model-generated:

1. `Escape()` handles `&`, `<`, `>` but **not quotes**, and `Inline()` interpolates the href straight into `href="{href}"` — a `"` in a model-emitted URL breaks out of the attribute. Add an `EscapeAttribute()` that also escapes `"` and `'`.
2. Nothing restricts the URL scheme. Allow only `http:`, `https:`, and root-relative `/` in the assistant's `LinkResolver`; render anything else (notably `javascript:`) as inert text.

Images are disabled outright for the assistant — a model-emitted `<img src="http://…">` is a needless exfiltration vector, and the assistant has no legitimate reason to emit one.

Styling lives in `AssistantPanel.razor.css` using **`::deep`**, exactly as `HelpArticlePage.razor.css` already styles its `MarkupString` output — CSS isolation attributes don't reach raw-HTML children, so `::deep` is mandatory here. Tables get `overflow-x: auto` so a wide asset-ledger table scrolls inside the bubble instead of blowing out the drawer's 420 px.

## 6. Page, clients & nav

- **`AssistantPage.razor`** (`@page "/assistant"`) — thin: `AMISPageHeader Title="AMIS Assistant"` + a full-height `MudPaper` hosting `<AssistantPanel />`. No `@attribute [Authorize]` — no other Expendable or Chat page uses it; `Routes.razor`'s `AuthorizeRouteView` handles auth.
- **Two clients**, because they hit two modules' route groups:
  - `ApiClient/AssistantClient.cs` — `IAssistantClient.AskAsync(...)` → `api/v1/assistant`
  - `ApiClient/ExpendableSearchClient.cs` — `SemanticSearchAsync` / `RebuildEmbeddingsAsync` → `api/v1/expendable/products`

  Both modeled on `ApiClient/ChatClient.cs`: `public interface` + `internal sealed class`, bare `HttpClient`, `const string Root`, static `JsonSerializerOptions(JsonSerializerDefaults.Web)`, anonymous-object request bodies, responses deserialized into the Contracts DTOs. Registered `AddTransient` in `Services/Api/ApiClientRegistration.cs`.
- **`NavMenu.razor`** — "AMIS Assistant" `MudNavLink` (`Icons.Material.Outlined.SmartToy`) as a **top-level entry**, gated on `AssistantPermissions.Assistant.Use`. Do **not** put it in the Expendable group — it answers asset questions too. Add `@using AMIS.Modules.Assistant.Contracts.Permissions` (NavMenu already imports AssetRegister and Chat permissions).

---

## Files

**New**

- `src/Host/AMIS.Blazor/Components/Pages/Assistant/AssistantPanel.razor` + `.razor.css`
- `src/Host/AMIS.Blazor/Components/Pages/Assistant/AssistantPage.razor`
- `src/Host/AMIS.Blazor/Services/AssistantConversationState.cs`
- `src/Host/AMIS.Blazor/Services/Markdown/MarkdownRenderer.cs`
- `src/Host/AMIS.Blazor/ApiClient/AssistantClient.cs`, `ApiClient/ExpendableSearchClient.cs`

**Modified**

- `src/Host/AMIS.Blazor/Services/Help/HelpContentService.cs` — delegate to the extracted renderer
- `src/Host/AMIS.Blazor/Components/Layout/AMISLayout.razor` — FAB + right drawer
- `src/Host/AMIS.Blazor/Components/Layout/NavMenu.razor` — top-level nav link + `@using`
- `src/Host/AMIS.Blazor/Components/Pages/Expendable/ProductsPage.razor` — smart-search toggle, Match column, rebuild menu
- `src/Host/AMIS.Blazor/Services/Api/ApiClientRegistration.cs`, `Program.cs` — registrations
- `AMIS.Blazor.csproj` — project reference to `Modules.Assistant.Contracts` (for the DTOs + permission constants)

Depends on backend Phases 0–5 of the RAG plan — notably `AssistantPermissions.Assistant.Use` and `ExpendablePermissions.SearchIndex.Rebuild`, which do not exist yet.

---

## Verification

Build gate first: `dotnet build src/AMIS.Framework.slnx` (0 warnings) and `dotnet test src/AMIS.Framework.slnx`.

Then run the AppHost and drive the UI:

1. **Help pages did not regress** — open `/help` and an article; headings, tables, images, and inter-article links must render exactly as before. This is the guard on the renderer extraction and is the single most likely thing to break.
2. **Markdown in answers** — ask "How many ballpens are in stock?" and confirm the reply renders as a real HTML table with bold text, not raw pipes/asterisks.
3. **Thinking state** — confirm the elapsed counter ticks during the wait and the composer is disabled.
4. **Shared transcript** — ask in the FAB drawer, click "Open full view", confirm the conversation carries over; then ask a follow-up ("and how about folders?") and confirm history replay makes it coherent.
5. **Cross-module answer** — ask "What has been issued to {employee}?" and confirm the reply covers **both** expendable issuances and property accountabilities, with tool chips from **both** modules visible under the answer.
6. **Permission filtering is visible in the UI** — as a user with `Assets.View` but **not** `Accountability.View`, ask "who holds property no X?". The assistant must say it cannot answer; no custodian data, no crash, no login redirect. (The security gate itself is tested server-side — see the backend plan's checklist item 10.)
7. **Smart search** — toggle it on, search "paper for printing", confirm "Bond Paper A4" ranks first with a Match % and that the same query with the toggle off returns nothing (proving semantic ≠ `ILike`).
8. **Rebuild** — as a user with `SearchIndex.Rebuild`, run "Rebuild search index"; confirm the confirm-dialog appears and counts come back in the snackbar.
9. **Permissions** — without `Assistant.Use`: no FAB, no nav link, and `/assistant` shows the inline warning. Without `SearchIndex.Rebuild`: no rebuild menu item.
10. **Dark mode** — toggle it and re-read the transcript. Both bubbles, the tool chips, and the markdown tables must stay legible (this is where hard-coded `--AMIS-card-bg` would have failed).
11. **Escaping** — ask the assistant to repeat back a string containing `<script>` and a `javascript:` link; confirm both render as inert text.

Save every `.razor` as **UTF-8** — the transcript uses `₱`, `…`, and `—`, and an ANSI save silently corrupts them (`.claude/rules/blazor.md`).
