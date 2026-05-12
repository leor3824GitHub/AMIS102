using Asp.Versioning;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using FSH.Modules.AssetRegister.Data;
using FSH.Modules.AssetRegister.Integration;
using FSH.Modules.AssetRegister.Provisioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FSH.Modules.AssetRegister;

public class AssetRegisterModule : IModule
{
    private static readonly IReadOnlyList<FshPermission> RegisteredPermissions =
    [
        new("View Assets",     "View",     "AssetRegister.Assets", IsBasic: true),
        new("Register Assets", "Register", "AssetRegister.Assets"),
        new("Update Assets",   "Update",   "AssetRegister.Assets"),
        new("Retire Assets",   "Retire",   "AssetRegister.Assets"),

        new("View Accountability",     "View",     "AssetRegister.Accountability", IsBasic: true),
        new("Issue Accountability",    "Issue",    "AssetRegister.Accountability"),
        new("Transfer Accountability", "Transfer", "AssetRegister.Accountability"),
        new("Return Accountability",   "Return",   "AssetRegister.Accountability"),

        new("View Issuance Reports", "View", "AssetRegister.Issuance", IsBasic: true),
        new("Post Issuance Reports", "Post", "AssetRegister.Issuance"),

        new("View Physical Count",   "View",   "AssetRegister.Count", IsBasic: true),
        new("Create Physical Count", "Create", "AssetRegister.Count"),
        new("Record Physical Count", "Record", "AssetRegister.Count"),
        new("Submit Physical Count", "Submit", "AssetRegister.Count"),
        new("Close Physical Count",  "Close",  "AssetRegister.Count"),

        new("View Incident Reports",    "View",    "AssetRegister.Incident", IsBasic: true),
        new("File Incident Reports",    "File",    "AssetRegister.Incident"),
        new("Resolve Incident Reports", "Resolve", "AssetRegister.Incident"),

        new("View Unserviceable Reports",    "View",    "AssetRegister.Unserviceable", IsBasic: true),
        new("File Unserviceable Reports",    "File",    "AssetRegister.Unserviceable"),
        new("Dispose Unserviceable Reports", "Dispose", "AssetRegister.Unserviceable"),

        new("View Property Catalog",   "View",   "AssetRegister.Catalog", IsBasic: true),
        new("Create Property Catalog", "Create", "AssetRegister.Catalog"),
        new("Update Property Catalog", "Update", "AssetRegister.Catalog"),
        new("Delete Property Catalog", "Delete", "AssetRegister.Catalog"),
    ];

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;

        PermissionConstants.Register(RegisteredPermissions);

        services.AddHeroDbContext<AssetRegisterDbContext>();
        services.AddScoped<IDbInitializer, AssetRegisterDbInitializer>();
        services.AddHostedService<AssetRegisterDbInitializerHostedService>();

        // Phase 1: register the consumer in DI; actual materialization logic lands in Phase 3.
        services.AddScoped<IIntegrationEventHandler<AssetIARAcceptedEvent>, AssetIARAcceptedEventConsumer>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        // Group exists so OpenAPI lists the module; Phase 1 has no routes.
        _ = endpoints
            .MapGroup("api/v{version:apiVersion}/asset-register")
            .WithTags("Asset Register")
            .WithApiVersionSet(apiVersionSet);
    }
}
