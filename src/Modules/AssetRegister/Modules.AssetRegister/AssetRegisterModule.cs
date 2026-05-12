using Asp.Versioning;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using FSH.Modules.AssetRegister.Data;
using FSH.Modules.AssetRegister.Data.Services;
using FSH.Modules.AssetRegister.Domain.Events;
using FSH.Modules.AssetRegister.Domain.Services;
using FSH.Modules.AssetRegister.Integration;
using FSH.Modules.AssetRegister.Provisioning;
using Mediator;
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

        // Number generator + counter allocator wiring (Phase 3a).
        services.AddScoped<CounterAllocator>();
        services.AddScoped<IPropertyNumberGenerator, PropertyNumberGenerator>();
        services.AddScoped<IAccountabilityNumberGenerator, AccountabilityNumberGenerator>();
        services.AddScoped<IInventoryTransferNumberGenerator, InventoryTransferNumberGenerator>();
        services.AddScoped<IIncidentNumberGenerator, IncidentNumberGenerator>();
        services.AddScoped<IIssuanceReportNumberGenerator, IssuanceReportNumberGenerator>();
        services.AddScoped<IUnserviceableReportNumberGenerator, UnserviceableReportNumberGenerator>();
        services.AddScoped<ICurrentReplacementCostCalculator, CurrentReplacementCostCalculator>();

        // Inbound integration consumer (Phase 3f) — materializes accepted IAR lines.
        services.AddScoped<IIntegrationEventHandler<AssetIARAcceptedEvent>, AssetIARAcceptedEventConsumer>();

        // Outbound: domain-event → integration-event publishers (Phase 3g).
        services.AddScoped<INotificationHandler<AssetRegisteredEvent>, AssetRegisteredIntegrationPublisher>();
        services.AddScoped<INotificationHandler<AssetIssuedEvent>, AssetIssuedIntegrationPublisher>();
        services.AddScoped<INotificationHandler<AssetDisposedEvent>, AssetDisposedIntegrationPublisher>();
        services.AddScoped<INotificationHandler<IssuanceReportPostedEvent>, IssuanceReportPostedIntegrationPublisher>();
        services.AddScoped<INotificationHandler<IncidentReportFiledEvent>, IncidentReportFiledIntegrationPublisher>();

        // Phase 4: log when a count session reports an asset missing.
        services.AddScoped<INotificationHandler<AssetReportedMissingFromCountEvent>, AssetReportedMissingFromCountHandler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var moduleGroup = endpoints
            .MapGroup("api/v{version:apiVersion}/asset-register")
            .WithTags("Asset Register")
            .WithApiVersionSet(apiVersionSet);

        // Catalog
        var catalog = moduleGroup.MapGroup("/catalog");
        Features.v1.Catalog.CreatePropertyItemCatalog.CreatePropertyItemCatalogEndpoint.Map(catalog);
        Features.v1.Catalog.UpdatePropertyItemCatalog.UpdatePropertyItemCatalogEndpoint.Map(catalog);
        Features.v1.Catalog.DeletePropertyItemCatalog.DeletePropertyItemCatalogEndpoint.Map(catalog);
        Features.v1.Catalog.SetPropertyItemCatalogActivation.SetPropertyItemCatalogActivationEndpoint.Map(catalog);
        Features.v1.Catalog.GetPropertyItemCatalog.GetPropertyItemCatalogEndpoint.Map(catalog);
        Features.v1.Catalog.SearchPropertyItemCatalogs.SearchPropertyItemCatalogsEndpoint.Map(catalog);

        // Assets
        var assets = moduleGroup.MapGroup("/assets");
        Features.v1.Assets.RegisterAsset.RegisterAssetEndpoint.Map(assets);
        Features.v1.Assets.UpdateAssetCondition.UpdateAssetConditionEndpoint.Map(assets);
        Features.v1.Assets.GetAssetRegistry.GetAssetRegistryEndpoint.Map(assets);
        Features.v1.Assets.GetAssetByPropertyNo.GetAssetByPropertyNoEndpoint.Map(assets);
        Features.v1.Assets.SearchAssets.SearchAssetsEndpoint.Map(assets);

        // Accountability (ICS / PAR)
        var accountability = moduleGroup.MapGroup("/accountability");
        Features.v1.Accountability.IssueAccountability.IssueAccountabilityEndpoint.Map(accountability);
        Features.v1.Accountability.RenewAccountability.RenewAccountabilityEndpoint.Map(accountability);
        Features.v1.Accountability.ReturnAccountabilityLines.ReturnAccountabilityLinesEndpoint.Map(accountability);
        Features.v1.Accountability.CancelAccountability.CancelAccountabilityEndpoint.Map(accountability);
        Features.v1.Accountability.GetAccountability.GetAccountabilityEndpoint.Map(accountability);
        Features.v1.Accountability.SearchAccountabilities.SearchAccountabilitiesEndpoint.Map(accountability);

        // Issuance reports (RSPI / PPEIR) — Phase 4
        var issuance = moduleGroup.MapGroup("/issuance");
        Features.v1.Issuance.CreateIssuanceReportDraft.CreateIssuanceReportDraftEndpoint.Map(issuance);
        Features.v1.Issuance.AddIssuanceReportLines.AddIssuanceReportLinesEndpoint.Map(issuance);
        Features.v1.Issuance.RemoveIssuanceReportLine.RemoveIssuanceReportLineEndpoint.Map(issuance);
        Features.v1.Issuance.PostIssuanceReport.PostIssuanceReportEndpoint.Map(issuance);
        Features.v1.Issuance.GetIssuanceReport.GetIssuanceReportEndpoint.Map(issuance);
        Features.v1.Issuance.SearchIssuanceReports.SearchIssuanceReportsEndpoint.Map(issuance);

        // Physical count sessions — Phase 4
        var count = moduleGroup.MapGroup("/count");
        Features.v1.Counting.StartPhysicalCount.StartPhysicalCountEndpoint.Map(count);
        Features.v1.Counting.RecordPhysicalCountEntry.RecordPhysicalCountEntryEndpoint.Map(count);
        Features.v1.Counting.AddFoundAtStationEntry.AddFoundAtStationEntryEndpoint.Map(count);
        Features.v1.Counting.MarkPhysicalCountMissing.MarkPhysicalCountMissingEndpoint.Map(count);
        Features.v1.Counting.ReconcilePhysicalCount.ReconcilePhysicalCountEndpoint.Map(count);
        Features.v1.Counting.ClosePhysicalCount.ClosePhysicalCountEndpoint.Map(count);
        Features.v1.Counting.GetPhysicalCountSession.GetPhysicalCountSessionEndpoint.Map(count);
        Features.v1.Counting.SearchPhysicalCountSessions.SearchPhysicalCountSessionsEndpoint.Map(count);

        // Property incident reports (RLSDDSP) — Phase 4
        var incidents = moduleGroup.MapGroup("/incidents");
        Features.v1.Incidents.FileIncidentReport.FileIncidentReportEndpoint.Map(incidents);
        Features.v1.Incidents.NotifyIncidentPolice.NotifyIncidentPoliceEndpoint.Map(incidents);
        Features.v1.Incidents.NotarizeIncidentReport.NotarizeIncidentReportEndpoint.Map(incidents);
        Features.v1.Incidents.IncidentResolutionEndpoints.MapResolutionEndpoints(incidents);
        Features.v1.Incidents.GetIncidentReport.GetIncidentReportEndpoint.Map(incidents);
        Features.v1.Incidents.SearchIncidentReports.SearchIncidentReportsEndpoint.Map(incidents);

        // Unserviceable property reports (IIRUSP / IIRUP) — Phase 4
        var unserviceable = moduleGroup.MapGroup("/unserviceable");
        Features.v1.Unserviceable.UnserviceableEndpoints.MapUnserviceableEndpoints(unserviceable);

        // Report rendering endpoints (ICS/PAR, RSPI/PPEIR, RPCSEMEX/RPCPPE, RegSPI, RLSDDSP, IIRUSP/IIRUP) — Phase 5
        var reports = moduleGroup.MapGroup("/reports");
        Features.v1.Reports.ReportEndpoints.MapReportEndpoints(reports);
    }
}
