# Reporting Documentation

> Guide to FAST and RDLC reporting frameworks, features, and implementation patterns.

---

## Quick Links

- **[FAST-REPORTING.md](FAST-REPORTING.md)** — FAST (FastReport.NET) implementation guide
- **[RDLC-REPORTING.md](RDLC-REPORTING.md)** — RDLC (Report Definition Language Client-side) framework

---

## Overview

AMIS supports two reporting frameworks for different scenarios:

1. **FAST Reporting** — High-performance reporting with rich formatting, drill-down, and interactivity
2. **RDLC Reporting** — Microsoft Report Definition Language, familiar to SQL Server Reporting Services users

---

## FAST Reporting

### Features

- **Rich Document Format** — Professional-quality PDF, Excel, Word exports
- **Master-Detail Reports** — Hierarchical data display with drill-down
- **Parameterized Reports** — Dynamic filtering and customization
- **Interactive Viewer** — Real-time report navigation and export
- **Server-side Rendering** — Generate reports on the server for large datasets

### Components

- **Report Designer** — Visual design interface for ad-hoc reports
- **Report Viewer** — Blazor component for displaying reports
- **Report Engine** — Server-side rendering and caching
- **Export Handlers** — PDF, Excel, Word, HTML output formatters

### Example Usage

```csharp
// Create report instance
var report = new ReportBuilder()
	.WithName("AssetInventoryReport")
	.WithDataSource(assetData)
	.WithParameters(new { FromDate = DateTime.Now.AddMonths(-1) })
	.Build();

// Render to format
var pdf = await report.RenderAsync(ReportFormat.Pdf);
```

---

## RDLC Reporting

### Features

- **Familiar Design** — Similar to SSRS for experienced users
- **Expression-based Calculations** — VB.NET expressions in reports
- **Grouping & Aggregation** — Complex data grouping and summarization
- **Subreports** — Modular report composition
- **Pagination** — Print-friendly page breaks and layouts

### Components

- **Report Definition (.rdlc)** — XML-based report definition files
- **Report Processor** — Compiles and executes .rdlc files
- **Report Viewer** — ASP.NET Core Report Viewer control
- **Export Handlers** — PDF, Excel, CSV output formatters

### Example Usage

```csharp
// Load report definition
var report = new ReportDefinition("Reports/AssetInventory.rdlc");

// Set parameters and data sources
report.SetParameter("TenantId", tenantId);
report.SetDataSource("Assets", assetDataTable);

// Render
var pdf = await _reportProcessor.ExecuteAsync(report, ReportFormat.Pdf);
```

---

## Report Types by Module

### AssetManagement Reports

| Report | Type | Purpose |
|--------|------|---------|
| Inventory Custodian Slip (ICS) | RDLC | Government compliance form |
| Property Acknowledgement Receipt (PAR) | RDLC | Government compliance form |
| Semi-Expendable Issuance Record (SMIR) | RDLC | Government compliance form |
| PPE Issuance Report (PPEIR) | RDLC | Government compliance form |
| Asset Inventory Report | FAST | Dashboards & analytics |
| Asset Movement History | FAST | Audit trails |

### Procurement Reports

| Report | Type | Purpose |
|--------|------|---------|
| Purchase Request Summary | RDLC | Daily/weekly summary |
| Purchase Order Register | FAST | Budget tracking |
| Canvass Result | RDLC | Supplier comparison |

### Financial Reports

| Report | Type | Purpose |
|--------|------|---------|
| Budget Utilization | FAST | Financial tracking |
| Disbursement Summary | RDLC | Payment records |

---

## Implementation Patterns

### Report Service

```csharp
public interface IReportService
{
	Task<byte[]> GenerateAsync(string reportName, ReportParameters parameters);
	Task<Stream> StreamAsync(string reportName, ReportParameters parameters, ReportFormat format);
	Task<ReportMetadata> GetMetadataAsync(string reportName);
}
```

### Report Parameters

```csharp
public class ReportParameters
{
	public DateTime FromDate { get; set; }
	public DateTime ToDate { get; set; }
	public Guid? TenantId { get; set; }
	public string AssetType { get; set; }
	public Dictionary<string, object> Custom { get; set; }
}
```

### Report Format Support

```csharp
public enum ReportFormat
{
	Pdf,
	Excel,
	Word,
	Html,
	Csv,
	Xml
}
```

---

## Performance Considerations

### Caching Strategy

- **Definition Caching** — Cache compiled report definitions
- **Data Caching** — Cache frequently-accessed data (with TTL)
- **Output Caching** — Cache rendered reports for identical parameter sets

### Optimization Tips

- **Parameterized Queries** — Filter data at the source, not in the report
- **Pagination** — For large datasets, use page-based rendering
- **Async Generation** — Render reports asynchronously to avoid UI blocking
- **Background Jobs** — Schedule heavy reports for off-peak times

---

## Integration Points

### API Endpoints

```csharp
[HttpGet("/api/reports/{reportName}")]
public async Task<IActionResult> GenerateReport(
	string reportName,
	[FromQuery] ReportParameters parameters)
{
	var report = await _reportService.GenerateAsync(reportName, parameters);
	return File(report, "application/pdf", $"{reportName}.pdf");
}
```

### Blazor Component

```razor
<ReportViewer @ref="viewer"
			  ReportName="@selectedReport"
			  Parameters="@reportParams"
			  OnExport="@HandleExport" />
```

---

## Compliance & Standards

### Government Form Alignment

- Reports match official COA forms (ICS, PAR, SMIR, PPEIR)
- Field placement follows government templates
- Signature blocks positioned for notarization
- Form numbering per COA 2020-006 format

### Audit Trail

- Report generation logged with:
  - Generated by (employee)
  - Generated on (timestamp)
  - Report parameters
  - Output format
  - Recipient (if emailed)

---

## Testing Reports

### Unit Tests

```csharp
[Test]
public async Task GenerateInventoryReport_WithValidParameters_ReturnsValidPdf()
{
	var report = await _reportService.GenerateAsync(
		"InventoryReport",
		new ReportParameters { FromDate = DateTime.Now.AddMonths(-1) });

	Assert.That(report, Is.Not.Empty);
	Assert.That(report, Does.StartWith(new byte[] { 0x25, 0x50, 0x44, 0x46 })); // PDF signature
}
```

### Integration Tests

- Test with real data from staging database
- Verify report calculations match business rules
- Validate export format integrity
- Check performance under load

---

## Troubleshooting

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Blank report output | Missing or incorrect data source | Verify data query and parameters |
| Formatting issues | Style/template mismatch | Check report definition against template |
| Performance degradation | Large dataset processing | Add pagination or filter data before report |
| Export failures | Missing format handler | Install required export packages |

---

## Best Practices

- **Modular Design** — Keep reports focused on single purpose
- **Parameter Validation** — Validate parameters before rendering
- **Error Handling** — Graceful handling of missing data or rendering errors
- **Documentation** — Document each report's purpose, parameters, and output
- **Version Control** — Track report definition changes
- **Testing** — Regular testing of report accuracy and performance

---

## Related Documentation

- **[ASSETMANAGEMENT-DOCUMENTATION.md](ASSETMANAGEMENT-DOCUMENTATION.md)** — Asset-related reports
- **[PROCUREMENT-DOCUMENTATION.md](PROCUREMENT-DOCUMENTATION.md)** — Procurement reports
- **[CLAUDE.md](CLAUDE.md)** — Development patterns

---

## Support

For detailed information on reporting:
- **FAST Reporting Details** → [FAST-REPORTING.md](FAST-REPORTING.md)
- **RDLC Framework** → [RDLC-REPORTING.md](RDLC-REPORTING.md)

