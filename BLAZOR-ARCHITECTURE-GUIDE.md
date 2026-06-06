# Blazor Architecture & Implementation Guide

> Comprehensive guide to Blazor API integration, client generation, architectural alignment, and component reusability.

---

## Quick Links

- **[BLAZOR-API-CLIENT-GENERATION.md](BLAZOR-API-CLIENT-GENERATION.md)** — OpenAPI client generation, tooling, automation
- **[BLAZOR-API-CONNECTION-ARCHITECTURE.md](BLAZOR-API-CONNECTION-ARCHITECTURE.md)** — API consumption patterns, HttpClient setup
- **[BLAZOR-API-CONSUMPTION-ALIGNMENT-ANALYSIS.md](BLAZOR-API-CONSUMPTION-ALIGNMENT-ANALYSIS.md)** — Contract alignment, HATEOAS, pagination
- **[BLAZOR-CLIENT-CONFORMANCE-AUDIT.md](BLAZOR-CLIENT-CONFORMANCE-AUDIT.md)** — Quality assurance, conformance testing
- **[BLAZOR-REUSABLE-COMPONENTS-PLAN.md](BLAZOR-REUSABLE-COMPONENTS-PLAN.md)** — Component library design, shared patterns

---

## Overview

The Blazor module documentation covers the complete lifecycle of building UI clients for AMIS:

1. **API Client Generation** — Automated tooling to create typed clients from OpenAPI specs
2. **Architecture & Connection** — Patterns for consuming APIs reliably and efficiently
3. **Alignment & Analysis** — Ensuring API contracts match UI expectations
4. **Conformance** — Quality gates and validation before production deployment
5. **Component Reusability** — Building a shared component library for faster development

---

## Architecture Snapshot

### Layered Client Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Blazor Pages (UI)                    │
├─────────────────────────────────────────────────────────┤
│            Reusable Components & Dialogs                │
├─────────────────────────────────────────────────────────┤
│          Service Layer / API Client Facade              │
├─────────────────────────────────────────────────────────┤
│    HttpClient / HttpClientFactory (message pipeline)    │
├─────────────────────────────────────────────────────────┤
│         OpenAPI Generated Clients (Typed)               │
├─────────────────────────────────────────────────────────┤
│              HTTP Transport / Networking                │
└─────────────────────────────────────────────────────────┘
```

### API Client Generation

**Tools & Automation:**
- OpenAPI / Swagger specification as source of truth
- NSwag or similar for code generation
- Build-time or pre-commit generation
- Automatic update on API changes

**Versioning Strategy:**
- Client version tracked with API version
- v1 clients regenerated on v1 API changes
- v2 clients support parallel consumption
- Migration guides for version upgrades

---

## API Consumption Patterns

### HttpClient Setup

```csharp
services.AddHttpClient<IAssetServiceClient, AssetServiceClient>(client =>
{
	client.BaseAddress = new Uri("https://api.example.com");
	client.DefaultRequestHeaders.Add("User-Agent", "AMIS-Blazor-Client/1.0");
})
.ConfigureHttpMessagePipeline()  // auth, logging, retry, etc.
```

### Request/Response Handling

- **Strongly-typed DTO contracts** from OpenAPI generation
- **Error mapping** — Convert HTTP status codes to domain exceptions
- **Pagination** — Implement cursor-based or offset-based patterns
- **Caching** — Strategic use of `CacheControl` headers
- **Retry logic** — Exponential backoff for transient failures

### Authentication & Authorization

- **Bearer token (JWT)** from Identity module
- **Refresh token** for seamless session continuation
- **Permission checking** at component and page level
- **Claim-based authorization** for feature flags

---

## Component Reusability

### Shared Component Library

**Categories:**
- **Data Display** — Tables, cards, lists with sorting/filtering
- **Forms** — Input fields, dropdowns, date pickers with validation
- **Dialogs** — Confirmation, edit, create modals
- **Navigation** — Breadcrumbs, menus, tabs
- **Feedback** — Toasts, spinners, error messages

**Design Principles:**
- Single responsibility per component
- Prop-based configuration (avoid hardcoding)
- Slot-based composition (e.g., `ChildContent`)
- Event callbacks for parent communication
- CSS class encapsulation (CSS isolation)

### Component Patterns

**Data Grid Example:**
```razor
<DataGrid Items="@products" 
		  Columns="@columns"
		  OnRowClick="@HandleRowClick"
		  IsSortable="true"
		  PageSize="20"
		  SearchTerm="@searchTerm">
</DataGrid>
```

**Form Dialog Example:**
```razor
<FormDialog @ref="dialog"
			Title="Add Product"
			OnSubmit="@HandleFormSubmit"
			OnCancel="@HandleCancel">
	<Fields>
		<TextField @bind-Value="model.Name" Label="Product Name" Required="true" />
		<NumericField @bind-Value="model.Price" Label="Price" />
	</Fields>
</FormDialog>
```

---

## API Contract Alignment

### Contract Types

**Standard REST Response:**
```csharp
public class ApiResponse<T>
{
	public bool Success { get; set; }
	public T Data { get; set; }
	public string Message { get; set; }
	public Dictionary<string, object> Links { get; set; }  // HATEOAS
}
```

**Pagination Contract:**
```csharp
public class PaginatedResponse<T>
{
	public IEnumerable<T> Items { get; set; }
	public int PageNumber { get; set; }
	public int PageSize { get; set; }
	public int TotalCount { get; set; }
	public bool HasNextPage { get; set; }
	public bool HasPreviousPage { get; set; }
}
```

**Error Response:**
```csharp
public class ErrorResponse
{
	public string Code { get; set; }
	public string Message { get; set; }
	public Dictionary<string, string[]> ValidationErrors { get; set; }
}
```

---

## Implementation Checklist

### API Client Generation ✅
- [ ] Export OpenAPI spec from API
- [ ] Configure NSwag client generator
- [ ] Generate typed clients for each module (v1, v2, etc.)
- [ ] Integrate into build pipeline
- [ ] Version control generated clients (or .gitignore)

### Service Layer ✅
- [ ] Create facade services wrapping generated clients
- [ ] Implement error handling and mapping
- [ ] Add logging and diagnostics
- [ ] Wire up dependency injection

### Component Library ✅
- [ ] Identify reusable UI patterns
- [ ] Create components for each pattern
- [ ] Document component props and usage
- [ ] Add CSS isolation
- [ ] Set up component showcase/storybook

### Integration & Testing ✅
- [ ] Unit tests for service facades
- [ ] Integration tests for API calls
- [ ] UI component tests (Bunit or similar)
- [ ] End-to-end tests for critical workflows

### Deployment ✅
- [ ] Configure environment-specific API endpoints
- [ ] Set up authentication middleware
- [ ] Implement feature flags
- [ ] Monitor API consumption metrics

---

## Best Practices

### Client Generation
- Regenerate clients automatically on API changes
- Keep generated code separate from custom code (use partial classes)
- Document code generation configuration in README
- Tag generated clients with version number

### API Consumption
- Cache responses where appropriate (readonly data)
- Implement retry logic for transient failures
- Log all API errors for debugging
- Use typed DTOs, never raw JSON strings

### Component Development
- Make components stateless where possible
- Use `@bind-Value` for two-way binding
- Emit `EventCallback` for parent communication
- Document component props with XML comments

### Performance
- Use virtual scrolling for large lists
- Lazy-load components and dialogs
- Debounce search/filter input
- Batch API requests where possible

---

## Related Documentation

- **[CLAUDE.md](CLAUDE.md)** — Development patterns and conventions
- **[ASSETMANAGEMENT-DOCUMENTATION.md](ASSETMANAGEMENT-DOCUMENTATION.md)** — Module-specific APIs
- **[EXPENDABLE-DOCUMENTATION.md](EXPENDABLE-DOCUMENTATION.md)** — Expendable module APIs
- **[POS-INDUSTRY-STANDARD-ALIGNMENT.md](POS-INDUSTRY-STANDARD-ALIGNMENT.md)** — POS/Blazor alignment

---

## Support

For detailed information on Blazor topics:
- **Client Generation** → [BLAZOR-API-CLIENT-GENERATION.md](BLAZOR-API-CLIENT-GENERATION.md)
- **Architecture** → [BLAZOR-API-CONNECTION-ARCHITECTURE.md](BLAZOR-API-CONNECTION-ARCHITECTURE.md)
- **Contract Alignment** → [BLAZOR-API-CONSUMPTION-ALIGNMENT-ANALYSIS.md](BLAZOR-API-CONSUMPTION-ALIGNMENT-ANALYSIS.md)
- **Quality Assurance** → [BLAZOR-CLIENT-CONFORMANCE-AUDIT.md](BLAZOR-CLIENT-CONFORMANCE-AUDIT.md)
- **Components** → [BLAZOR-REUSABLE-COMPONENTS-PLAN.md](BLAZOR-REUSABLE-COMPONENTS-PLAN.md)

