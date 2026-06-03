# AMIS AI Implementation Guide: Safe Function Calling with Entity Framework Core

This guide provides a comprehensive technical blueprint for integrating a self-hosted Large Language Model (LLM) directly into the **Asset Management and Inventory System (AMIS)** using **Blazor Server/WebAssembly**, **Microsoft.Extensions.AI**, and **Entity Framework Core**.

Instead of exposing raw database endpoints or relying on unreliable text-to-SQL generation, this implementation uses **AI Function Calling (Tools)**. This design ensures that the local LLM determines *when* a database query is necessary, while your compiled C# code controls *how* that query safely runs, preventing security issues like prompt injection and unauthorized access.

---

## 🏗️ Architectural Overview

To prevent tight coupling between layers, the AI orchestration logic resides within the **Infrastructure** layer, while domain rules are safely contained inside the **Core/Application** layer.

```
[ Blazor UI Layout ] ──(Natural Language)──▶ [ AI Tool Orchestrator ]
                                                     │
                                         (Validates & Invokes Method)
                                                     │
                                                     ▼
[ SQL Database ] ◀───(Compiled LINQ)──── [ EF Core Asset Repository ]
```

### Key Security & Design Safeguards
1. **No Text-to-SQL:** The LLM is never allowed to write raw SQL commands. It can only execute explicit C# repository methods.
2. **Strict Schema Type-Safety:** Arguments extracted by the LLM are parsed and strongly typed before hitting the database.
3. **Scoped Lifetime Isolation:** The database context remains tightly bound to the current user's request context, enforcing system accountability.

---

## 🛠️ Step 1: Define the Asset Repository & Domain Interfaces

Define your asset contracts within your core application logic. This repository handles the physical execution of your database inquiries safely via compiled LINQ.

```csharp
// Core/Interfaces/IAssetRepository.cs
using System.Threading.Tasks;
using System.Collections.Generic;

public interface IAssetRepository
{
    Task<int> GetCountByLocationAndStatusAsync(string location, string? status);
    Task<decimal> GetTotalValueByClassificationAsync(string classification);
}
```

Implement the interface using your system's `DbContext`. This layer isolates data processing from the AI execution engine.

```csharp
// Infrastructure/Persistence/Repositories/AssetRepository.cs
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

public class AssetRepository : IAssetRepository
{
    private readonly AmisDbContext _context;

    public AssetRepository(AmisDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetCountByLocationAndStatusAsync(string location, string? status)
    {
        var query = _context.Assets.AsNoTracking();

        // Enforce basic fuzzy cleaning on location inputs
        if (!string.IsNullOrWhiteSpace(location))
        {
            query = query.Where(a => EF.Functions.Like(a.LocationName, $"%{location}%"));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status.ToLower() == status.ToLower());
        }

        return await query.CountAsync();
    }

    public async Task<decimal> GetTotalValueByClassificationAsync(string classification)
    {
        return await _context.Assets
            .AsNoTracking()
            .Where(a => EF.Functions.Like(a.Classification, $"%{classification}%"))
            .SumAsync(a => a.AcquisitionCost);
    }
}
```

---

## ⚙️ Step 2: Create the AI Native Tools Layer

The AI model requires semantic descriptions to understand when to fire native operations. Use the `Description` attribute to document your application APIs for the local engine.

```csharp
// Infrastructure/Ai/Tools/AmisInventoryTools.cs
using System.ComponentModel;
using System.Threading.Tasks;

public class AmisInventoryTools
{
    private readonly IAssetRepository _assetRepository;

    public AmisInventoryTools(IAssetRepository assetRepository)
    {
        _assetRepository = assetRepository;
    }

    [Description("Gets the numeric count of active physical inventory assets located within a specific facility, regional office, or branch location.")]
    public async Task<string> GetAssetCount(
        [Description("The name of the office, warehouse, or geographic region (e.g., 'Caraga', 'Butuan City', 'Warehouse 1')")] string location,
        [Description("Optional status filter such as 'Functional', 'Unserviceable', or 'Disposed'")] string? status = null)
    {
        try
        {
            int count = await _assetRepository.GetCountByLocationAndStatusAsync(location, status);
            
            return string.IsNullOrEmpty(status) 
                ? $"There are currently {count} total assets registered in {location}."
                : $"There are currently {count} assets flagged as '{status}' in {location}.";
        }
        catch (Exception ex)
        {
            return $"Error retrieving data from the repository: {ex.Message}";
        }
    }

    [Description("Calculates the total financial book value or acquisition cost of assets categorized under a specific classification group.")]
    public async Task<string> GetFinancialValue(
        [Description("The asset classification class (e.g., 'IT Equipment', 'Machinery', 'Office Furniture')")] string classification)
    {
        try
        {
            decimal totalValue = await _assetRepository.GetTotalValueByClassificationAsync(classification);
            return $"The total calculated ledger value for {classification} is PHP {totalValue:N2}.";
        }
        catch (Exception ex)
        {
            return $"Error calculating financial aggregates: {ex.Message}";
        }
    }
}
```

---

## ⛓️ Step 3: Configure Dependency Injection & Middleware

Wire up your local **Ollama** engine and connect your tools pipeline into the .NET runtime infrastructure inside `Program.cs`.

```csharp
// Program.cs
using Microsoft.Extensions.AI;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Setup EF Core DB Context
builder.Services.AddDbContext<AmisDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Register Repositories
builder.Services.AddScoped<IAssetRepository, AssetRepository>();

// 3. Register Tool Class definitions
builder.Services.AddScoped<AmisInventoryTools>();

// 4. Configure the Self-Hosted Local AI Engine with Function Calling Capabilities
builder.Services.AddChatClient(services =>
{
    // Point to your local self-hosted Ollama deployment
    IChatClient client = new OllamaChatClient(new Uri("http://localhost:11434"), "llama3");

    // Wrap the client to inject functional execution capabilities transparently
    return client.AsBuilder()
                 .UseFunctionCalling()
                 .Build();
});

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();
```

---

## 🎨 Step 4: Build the Interactive Streaming Chat UI

This responsive Razor component uses **Asynchronous Token Streaming** to push real-time incremental output to the screen, eliminating awkward system delays.

```razor
<!-- Components/Pages/AiDashboard.razor -->
@page "/amis-ai"
@rendermode InteractiveServer
@using Microsoft.Extensions.AI
@using System.Text
@inject IChatClient ChatClient
@inject AmisInventoryTools InventoryTools

<div class="container-fluid mt-4" style="max-width: 900px; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;">
    <div class="card shadow-sm border-0">
        <div class="card-header bg-dark text-white p-3">
            <h5 class="mb-0">📊 AMIS Intelligent Query Assistant</h5>
            <small class="text-muted text-light opacity-75">Self-Hosted Enterprise Local AI Engine Active</small>
        </div>
        
        <div class="card-body bg-light" style="height: 450px; overflow-y: auto; border-bottom: 1px solid #dee2e6;">
            @foreach (var msg in _conversationHistory.Where(m => m.Role != ChatRole.System))
            {
                <div class="d-flex flex-column mb-3 @(msg.Role == ChatRole.User ? "align-items-end" : "align-items-start")">
                    <span class="badge mb-1 @(msg.Role == ChatRole.User ? "bg-primary text-white" : "bg-secondary text-white")">
                        @(msg.Role == ChatRole.User ? "Operational Request" : "System AI")
                    </span>
                    <div class="p-3 rounded-3 shadow-sm @(msg.Role == ChatRole.User ? "bg-info text-white text-end" : "bg-white text-dark")" 
                         style="max-width: 75%; white-space: pre-wrap; font-size: 14px;">
                        @msg.Text
                    </div>
                </div>
            }

            @if (_isProcessing)
            {
                <div class="d-flex align-items-center text-muted gap-2">
                    <div class="spinner-border spinner-border-sm text-primary" role="status"></div>
                    <span class="fst-italic">Evaluating request context and checking database indices...</span>
                </div>
            }
        </div>

        <div class="card-footer bg-white p-3">
            <div class="input-group">
                <input type="text" class="form-control" 
                       placeholder="e.g., How many functional assets do we have in Butuan City right now?" 
                       @bind="_userInput" 
                       @onkeyup="HandleKeyPress" 
                       disabled="@_isProcessing" />
                <button class="btn btn-dark px-4" @onclick="ProcessUserQuery" disabled="@_isProcessing">
                    Execute Query
                </button>
            </div>
        </div>
    </div>
</div>

@code {
    private string _userInput = string.Empty;
    private bool _isProcessing = false;

    private readonly List<ChatMessage> _conversationHistory = new()
    {
        new ChatMessage(ChatRole.System, 
            "You are the internal AI automation core for AMIS (Asset Management and Inventory System). " +
            "You possess real-time functional tools to access corporate storage records directly. " +
            "Always utilize the proper provided tool whenever asked about asset quantities, counts, locations, or valuations. " +
            "Be succinct, objective, and provide numbers exclusively sourced from your native tools.")
    };

    private async Task HandleKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await ProcessUserQuery();
        }
    }

    private async Task ProcessUserQuery()
    {
        if (string.IsNullOrWhiteSpace(_userInput)) return;

        _isProcessing = true;
        var capturedInput = _userInput;
        _conversationHistory.Add(new ChatMessage(ChatRole.User, capturedInput));
        _userInput = string.Empty;

        // Initialize a placeholder message for the incoming stream response
        var streamingAssistantMessage = new ChatMessage(ChatRole.Assistant, string.Empty);
        _conversationHistory.Add(streamingAssistantMessage);

        try
        {
            // Configure execution options and inject runtime tool references
            var options = new ChatOptions
            {
                Tools = [ ChatTool.CreateFromMethod(InventoryTools, nameof(AmisInventoryTools.GetAssetCount)),
                          ChatTool.CreateFromMethod(InventoryTools, nameof(AmisInventoryTools.GetFinancialValue)) ]
            };

            var responseStream = ChatClient.CompleteStreamingAsync(_conversationHistory, options);
            var responseAccumulator = new StringBuilder();

            await foreach (var chunk in responseStream)
            {
                if (chunk.Text is not null)
                {
                    responseAccumulator.Append(chunk.Text);
                    streamingAssistantMessage.Text = responseAccumulator.ToString();
                    
                    // Force Blazor to update the UI on every token chunk received
                    StateHasChanged();
                }
            }
        }
        catch (Exception ex)
        {
            streamingAssistantMessage.Text = $"System Error processing transaction pipeline: {ex.Message}";
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }
}
```

---

## 🚀 Execution Lifecycle Flow Chart

When a user submits a query, the internal ecosystem coordinates the interaction seamlessly:

1. **User Request:** *"What is the financial value of our IT Equipment?"* is sent to the application layer.
2. **Analysis:** The `OllamaChatClient` checks the available options and notices a matching description: `GetFinancialValue(classification: "IT Equipment")`.
3. **Execution:** The runtime pauses execution, safely calls `AmisInventoryTools.GetFinancialValue("IT Equipment")`, and executes the underlying safe LINQ query against SQL Server.
4. **Synthesis:** The raw database result string is passed back to Ollama.
5. **Streaming:** Ollama formats the data into natural language and streams it chunk-by-chunk directly back to your Blazor UI dashboard.