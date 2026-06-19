# AMIS Dynamic Report Designer Implementation Plan

Integrating a dynamic report builder into AMIS using your existing Vertical Slice and Clean Architecture stack requires treating the report designer as a core domain feature. Because the system tracks government property and requires strict accountability, the forms generated must be highly predictable.

This blueprint replaces the traditional band-oriented layout (like FastReport) with a modern CSS Grid/Flexbox-inspired box model (Rows, Columns, Stacks, and Tables), mapped directly to QuestPDF's Fluent API.

## Architecture Blueprint

```text
[ Blazor UI Designer Canvas ]  <--->  [ Dynamic JSON Schema (AST) ]
                                                   │
                                                   ▼
                                      [ Core Domain AST Models ]
                                                   │
                                                   ▼
                                      [ QuestPDF Recursive Parser ]
                                                   │
                                                   ▼
                                      [ Rendered PDF Byte Stream ]
```

## Phase 1: Core Domain (The Abstract Syntax Tree / JSON Schema)

Before any PDF generation or UI rendering happens, you need a shared domain model. This model acts as the single source of truth for both the database, the Blazor client, and the QuestPDF engine. We build an Abstract Syntax Tree (AST) using strongly typed C# classes.

```csharp
namespace AMIS.Domain.Features.Reports;

public enum NodeType
{
    Page,
    Row,
    Column,
    Text,
    Table,
    Image,
    Spacer
}

public class ReportTemplate
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } // Multi-tenant scoping
    public string Name { get; set; }
    public ReportNode RootNode { get; set; }
}

public class ReportNode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public NodeType Type { get; set; }
    
    // Layout & Presentation Properties
    public string BackgroundColor { get; set; }
    public float? MarginTop { get; set; }
    public float? MarginBottom { get; set; }
    public float? MarginLeft { get; set; }
    public float? MarginRight { get; set; }
    public float? Padding { get; set; }
    
    // Text Specific Properties
    public string Content { get; set; } // Can contain tokens: "System Property No: {{PropertyNumber}}"
    public int FontSize { get; set; } = 11;
    public bool IsBold { get; set; }
    public string Alignment { get; set; } // Left, Center, Right, Justify

    // Table Specific Properties
    public List<TableColumnDefinition> TableColumns { get; set; } = new();
    public string BindSourceCollection { get; set; } // e.g., "Assets"

    // Hierarchy
    public List<ReportNode> Children { get; set; } = new();
}

public class TableColumnDefinition
{
    public string HeaderText { get; set; }
    public string DataToken { get; set; } // e.g., "{{Asset.Description}}"
    public float WidthRatio { get; set; } = 1f; // Relative sizing
}
```

## Phase 2: Application Layer (Vertical Slices)

In a vertical slice architecture, the report designer operations should be modeled as distinct features.

```csharp
namespace AMIS.Application.Features.Reports;

using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public record GenerateReportQuery(Guid TemplateId, Dictionary<string, object> ReportData) : IRequest<byte[]>;

public class GenerateReportHandler : IRequestHandler<GenerateReportQuery, byte[]>
{
    private readonly IApplicationDbContext _context; // Access your Multi-tenant DbContext

    public GenerateReportHandler(IApplicationDbContext context) => _context = context;

    public async Task<byte[]> Handle(GenerateReportQuery request, CancellationToken cancellationToken)
    {
        // 1. Fetch template from multi-tenant DB
        var template = await _context.ReportTemplates
            .FirstOrDefaultAsync(x => x.Id == request.TemplateId, cancellationToken);
            
        if (template == null) throw new Exception("Template Not Found");

        // 2. Instantiate our Engine
        var document = new QuestReportEngine(template.RootNode, request.ReportData);

        // 3. Output raw PDF bytes instantly
        return document.GeneratePdf();
    }
}
```

## Phase 3: Infrastructure (The QuestPDF Interpreter Engine)

This layer acts exactly like FastReport's runtime generator. It recursively walks through the JSON tree and translates the schema into actual QuestPDF layout commands.

```csharp
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using AMIS.Domain.Features.Reports;
using System.Collections.Generic;

namespace AMIS.Infrastructure.Reporting;

public class QuestReportEngine : IDocument
{
    private readonly ReportNode _rootNode;
    private readonly Dictionary<string, object> _dataContext;

    public QuestReportEngine(ReportNode rootNode, Dictionary<string, object> dataContext)
    {
        _rootNode = rootNode;
        _dataContext = dataContext;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            // Set standard global rules
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11));

            page.Content().Column(column =>
            {
                foreach (var child in _rootNode.Children)
                {
                    RenderNode(column.Item(), child);
                }
            });
        });
    }

    private void RenderNode(IContainer container, ReportNode node)
    {
        // Apply universal padding/margins adjustments
        var adjustedContainer = container;
        if (node.Padding.HasValue) adjustedContainer = adjustedContainer.Padding(node.Padding.Value);
        
        switch (node.Type)
        {
            case NodeType.Row:
                adjustedContainer.Row(row =>
                {
                    foreach (var child in node.Children)
                    {
                        RenderNode(row.RelativeItem(), child);
                    }
                });
                break;

            case NodeType.Column:
                adjustedContainer.Column(col =>
                {
                    foreach (var child in node.Children)
                    {
                        RenderNode(col.Item(), child);
                    }
                });
                break;

            case NodeType.Text:
                var parsedText = BindTokens(node.Content);
                var textElement = adjustedContainer.Text(parsedText)
                    .FontSize(node.FontSize);
                
                if (node.IsBold) textElement.Bold();
                break;

            case NodeType.Table:
                RenderDynamicTable(adjustedContainer, node);
                break;

            case NodeType.Spacer:
                adjustedContainer.Height(node.MarginTop ?? 10);
                break;
        }
    }

    private void RenderDynamicTable(IContainer container, ReportNode node)
    {
        if (!_dataContext.TryGetValue(node.BindSourceCollection, out var collectionObj) || 
            collectionObj is not IEnumerable<object> rows) return;

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var col in node.TableColumns)
                {
                    columns.RelativeColumn(col.WidthRatio);
                }
            });

            table.Header(header =>
            {
                foreach (var col in node.TableColumns)
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                          .Text(col.HeaderText).Bold();
                }
            });

            foreach (var rowData in rows)
            {
                foreach (var col in node.TableColumns)
                {
                    var cellValue = ExtractValueFromData(rowData, col.DataToken);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                          .Padding(5).Text(cellValue);
                }
            }
        });
    }

    private string BindTokens(string templateText)
    {
        if (string.IsNullOrEmpty(templateText)) return string.Empty;
        foreach (var key in _dataContext.Keys)
        {
            if (templateText.Contains($"{{{{{key}}}}}"))
            {
                templateText = templateText.Replace($"{{{{{key}}}}}", _dataContext[key]?.ToString());
            }
        }
        return templateText;
    }

    private string ExtractValueFromData(object obj, string token)
    {
        if (obj == null || string.IsNullOrEmpty(token)) return string.Empty;
        var cleanToken = token.Replace("{", "").Replace("}", "").Trim();
        var prop = obj.GetType().GetProperty(cleanToken);
        return prop?.GetValue(obj, null)?.ToString() ?? string.Empty;
    }
}
```

## Phase 4: The Blazor Client (The Visual Designer)

With the contract established and the engine tested, build the interactive HTML5 Drag-and-Drop tree editor workspace.

Create a recursive component `<ReportCanvasNode.razor>` that reads the JSON state and updates the tree reactively.

```razor
@using AMIS.Domain.Features.Reports

<div class="report-node-wrapper @(IsSelected ? "selected-border" : "")" 
     @onclick:stopPropagation="true" 
     @onclick="SelectNode"
     draggable="true"
     @ondragstart="HandleDragStart"
     @ondragover:preventDefault
     @ondrop="HandleDrop">

    @switch (Node.Type)
    {
        case NodeType.Row:
            <div class="flex-row-layout">
                @foreach (var child in Node.Children)
                {
                    <ReportCanvasNode Node="child" OnSelected="OnSelected" SelectedNode="SelectedNode" />
                }
            </div>
            break;

        case NodeType.Column:
            <div class="flex-column-layout">
                @foreach (var child in Node.Children)
                {
                    <ReportCanvasNode Node="child" OnSelected="OnSelected" SelectedNode="SelectedNode" />
                }
            </div>
            break;

        case NodeType.Text:
            <div style="font-size: @(Node.FontSize)px; font-weight: @(Node.IsBold ? "bold" : "normal")">
                @(string.IsNullOrWhiteSpace(Node.Content) ? "[Empty Text Block - Click to Edit]" : Node.Content)
            </div>
            break;

        case NodeType.Table:
            <table class="mock-designer-table">
                <thead>
                    <tr>
                        @foreach (var col in Node.TableColumns)
                        {
                            <th>@col.HeaderText</th>
                        }
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        @foreach (var col in Node.TableColumns)
                        {
                            <td>@col.DataToken</td>
                        }
                    </tr>
                </tbody>
            </table>
            break;
    }
</div>

@code {
    [Parameter] public ReportNode Node { get; set; }
    [Parameter] public ReportNode SelectedNode { get; set; }
    [Parameter] public EventCallback<ReportNode> OnSelected { get; set; }

    private bool IsSelected => SelectedNode?.Id == Node.Id;

    private void SelectNode() => OnSelected.InvokeAsync(Node);

    private void HandleDragStart()
    {
        // Native state-management store pointer tracking what element is being dragged
    }

    private void HandleDrop()
    {
        // Mutate the tree model structure: push dragged child node onto Node.Children
    }
}
```

## Phase 5: Integration and Execution

1. **Mock the Data Context:** To allow users to see what they are building, expose a dictionary of available data fields (e.g., `Asset.Description`, `Asset.AcquisitionDate`) that they can select from a dropdown in the properties panel.
2. **Live Preview Loop:** Add a "Preview" button that sends the current JSON state to the `GenerateReportPreview` feature via the API, returning the rendered QuestPDF byte array to be displayed in a Blazor `<object data="data:application/pdf;base64,...">` tag.
