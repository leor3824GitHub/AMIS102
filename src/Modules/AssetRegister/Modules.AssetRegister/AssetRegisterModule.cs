using Asp.Versioning;
using AMIS.Framework.Eventing.Abstractions;
using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Web.Modules;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Data.Services;
using AMIS.Modules.AssetRegister.Domain.Events;
using AMIS.Modules.AssetRegister.Domain.Services;
using AMIS.Modules.AssetRegister.Integration;
using AMIS.Modules.AssetRegister.Provisioning;
using Hangfire;
using Hangfire.Common;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetRegister;

public class AssetRegisterModule : IModule
{
    private static readonly IReadOnlyList<AmisPermission> RegisteredPermissions =
    [
        new("View Assets",     "View",     "AssetRegister.Assets", IsBasic: true),
        new("Register Assets", "Register", "AssetRegister.Assets"),
        new("Update Assets",   "Update",   "AssetRegister.Assets"),

        new("View Accountability",   "View",   "AssetRegister.Accountability", IsBasic: true),
        new("Issue Accountability",  "Issue",  "AssetRegister.Accountability"),
        new("Update Accountability", "Update", "AssetRegister.Accountability"),
        new("Delete Accountability", "Delete", "AssetRegister.Accountability"),
        new("Return Accountability", "Return", "AssetRegister.Accountability"),
        new("Cancel Accountability", "Cancel", "AssetRegister.Accountability"),

        // Employee self-service (My Accountability) — scoped to the user's own ICS/PAR.
        new("View My Accountability",           "View",           "AssetRegister.MyAccountability", IsBasic: true),
        new("Acknowledge My Accountability",    "Acknowledge",    "AssetRegister.MyAccountability"),
        new("Return My Accountability",         "Return",         "AssetRegister.MyAccountability"),
        new("Report My Accountability Incident","ReportIncident", "AssetRegister.MyAccountability"),
        new("Confirm My Accountability Count",  "ConfirmCount",   "AssetRegister.MyAccountability"),

        new("View Issuance Reports",   "View",   "AssetRegister.Issuance", IsBasic: true),
        new("Create Issuance Reports", "Create", "AssetRegister.Issuance"),
        new("Update Issuance Reports", "Update", "AssetRegister.Issuance"),

        new("View Physical Count",   "View",   "AssetRegister.Count", IsBasic: true),
        new("Create Physical Count", "Create", "AssetRegister.Count"),
        new("Freeze Physical Count", "Freeze", "AssetRegister.Count"),
        new("Record Physical Count", "Record", "AssetRegister.Count"),
        new("Submit Physical Count", "Submit", "AssetRegister.Count"),
        new("Close Physical Count",  "Close",  "AssetRegister.Count"),

        new("View Incident Reports",    "View",    "AssetRegister.Incident", IsBasic: true),
        new("File Incident Reports",    "File",    "AssetRegister.Incident"),
        new("Resolve Incident Reports", "Resolve", "AssetRegister.Incident"),

        new("View Unserviceable Reports",    "View",    "AssetRegister.Unserviceable", IsBasic: true),
        new("File Unserviceable Reports",    "File",    "AssetRegister.Unserviceable"),
        new("Dispose Unserviceable Reports", "Dispose", "AssetRegister.Unserviceable"),

        // PPE repair (RPRI / Exhibit 6) — repairs are PPE-wide, owned by AssetRegister.
        new("View Repairs",    "View",    "AssetRegister.Repair", IsBasic: true),
        new("Request Repairs", "Request", "AssetRegister.Repair", IsBasic: true),
        new("Inspect Repairs", "Inspect", "AssetRegister.Repair"),
        new("Accept Repairs",  "Accept",  "AssetRegister.Repair"),

        new("View Property Catalog",   "View",   "AssetRegister.Catalog", IsBasic: true),
        new("Create Property Catalog", "Create", "AssetRegister.Catalog"),
        new("Update Property Catalog", "Update", "AssetRegister.Catalog"),
        new("Delete Property Catalog", "Delete", "AssetRegister.Catalog"),

        new("View Receiving Reports",   "View",   "AssetRegister.Receiving", IsBasic: true),
        new("Create Receiving Reports", "Create", "AssetRegister.Receiving"),
        new("Delete Receiving Reports", "Delete", "AssetRegister.Receiving"),

        new("View Returned Property Receipts",    "View",    "AssetRegister.ReturnedProperty", IsBasic: true),
        new("Create Returned Property Receipts",  "Create",  "AssetRegister.ReturnedProperty"),
        new("Inspect Returned Property Receipts", "Inspect", "AssetRegister.ReturnedProperty"),
        new("Accept Returned Property Receipts",  "Accept",  "AssetRegister.ReturnedProperty"),

        new("View Signed Document Copies",   "View",   "AssetRegister.SignedDocuments", IsBasic: true),
        new("Upload Signed Document Copies", "Upload", "AssetRegister.SignedDocuments"),

        new("View Locations",   "View",   "AssetRegister.Locations", IsBasic: true),
        new("Create Locations", "Create", "AssetRegister.Locations"),
        new("Update Locations", "Update", "AssetRegister.Locations"),
        new("Delete Locations", "Delete", "AssetRegister.Locations"),
    ];

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;

        PermissionConstants.Register(RegisteredPermissions);

        services.AddHeroDbContext<AssetRegisterDbContext>();
        services.AddScoped<IDbInitializer, AssetRegisterDbInitializer>();
        services.AddHostedService<AssetRegisterDbInitializerHostedService>();

        // Document-number generator + counter allocator wiring.
        // (PropertyNo is operator-assigned per NFA policy — no system generator.)
        services.AddScoped<CounterAllocator>();
        services.AddScoped<IAccountabilityNumberGenerator, AccountabilityNumberGenerator>();
        services.AddScoped<IInventoryTransferNumberGenerator, InventoryTransferNumberGenerator>();
        services.AddScoped<IIncidentNumberGenerator, IncidentNumberGenerator>();
        services.AddScoped<IIssuanceReportNumberGenerator, IssuanceReportNumberGenerator>();
        services.AddScoped<IUnserviceableReportNumberGenerator, UnserviceableReportNumberGenerator>();
        services.AddScoped<IReceivingReportNumberGenerator, ReceivingReportNumberGenerator>();
        services.AddScoped<ICurrentReplacementCostCalculator, CurrentReplacementCostCalculator>();

        // Physical-count ledger freeze: blocks covered asset movements while a count is active.
        services.AddScoped<ICountFreezeGuard, CountFreezeGuard>();

        // PPE depreciation engine (COA GAM straight-line) + monthly recurring job.
        services.AddScoped<DepreciationPostingService>();
        services.AddScoped<DepreciationRecurringJob>();

        // Inbound integration consumer (Phase 3f) — materializes accepted IAR lines.
        services.AddScoped<IIntegrationEventHandler<AssetIARAcceptedEvent>, AssetIARAcceptedEventConsumer>();

        // Outbound: domain-event → integration-event publishers (Phase 3g).
        services.AddScoped<INotificationHandler<AssetRegisteredEvent>, AssetRegisteredIntegrationPublisher>();
        services.AddScoped<INotificationHandler<AssetIssuedEvent>, AssetIssuedIntegrationPublisher>();
        services.AddScoped<INotificationHandler<AssetDisposedEvent>, AssetDisposedIntegrationPublisher>();
        services.AddScoped<INotificationHandler<IssuanceReportPostedEvent>, IssuanceReportPostedIntegrationPublisher>();
        services.AddScoped<INotificationHandler<IncidentReportFiledEvent>, IncidentReportFiledIntegrationPublisher>();
        services.AddScoped<INotificationHandler<UnserviceableReportSubmittedEvent>, UnserviceableReportClosedIntegrationPublisher>();

        // Phase 4: log when a count session reports an asset missing.
        services.AddScoped<INotificationHandler<AssetReportedMissingFromCountEvent>, AssetReportedMissingFromCountHandler>();

        // Internal domain event handlers (Phase 3g) — track state changes but don't publish integration events.
        services.AddScoped<INotificationHandler<AssetReturnedEvent>, AssetReturnedEventHandler>();
        services.AddScoped<INotificationHandler<AssetTransferredEvent>, AssetTransferredEventHandler>();
        services.AddScoped<INotificationHandler<AssetTransferredOutEvent>, AssetTransferredOutEventHandler>();
        services.AddScoped<INotificationHandler<AssetFoundAtStationEvent>, AssetFoundAtStationEventHandler>();
        services.AddScoped<INotificationHandler<AssetLostEvent>, AssetLostEventHandler>();
        services.AddScoped<INotificationHandler<AssetRecoveredEvent>, AssetRecoveredEventHandler>();
        services.AddScoped<INotificationHandler<AssetUnserviceableEvent>, AssetUnserviceableEventHandler>();
        services.AddScoped<INotificationHandler<AccountabilityCancelledEvent>, AccountabilityCancelledEventHandler>();
        services.AddScoped<INotificationHandler<PhysicalCountSessionClosedEvent>, PhysicalCountSessionClosedEventHandler>();
        services.AddScoped<INotificationHandler<PhysicalCountFrozenEvent>, PhysicalCountFrozenEventHandler>();
        services.AddScoped<INotificationHandler<PhysicalCountRecountRequestedEvent>, PhysicalCountRecountRequestedEventHandler>();
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
        Features.v1.Assets.UpdateAssetDepreciation.UpdateAssetDepreciationEndpoint.Map(assets);
        Features.v1.Assets.GetAssetRegistry.GetAssetRegistryEndpoint.Map(assets);
        Features.v1.Assets.GetAssetByPropertyNo.GetAssetByPropertyNoEndpoint.Map(assets);
        Features.v1.Assets.GetAssetScanDetailByPropertyNo.GetAssetScanDetailByPropertyNoEndpoint.Map(assets);
        Features.v1.Assets.GetNextPropertyNoSequence.GetNextPropertyNoSequenceEndpoint.Map(assets);
        Features.v1.Assets.SearchAssets.SearchAssetsEndpoint.Map(assets);

        // Accountability (ICS / PAR)
        var accountability = moduleGroup.MapGroup("/accountability");
        Features.v1.Accountability.IssueAccountability.IssueAccountabilityEndpoint.Map(accountability);
        Features.v1.Accountability.PeekAccountabilityNumber.PeekAccountabilityNumberEndpoint.Map(accountability);
        Features.v1.Accountability.UpdateAccountability.UpdateAccountabilityEndpoint.Map(accountability);
        Features.v1.Accountability.DeleteAccountability.DeleteAccountabilityEndpoint.Map(accountability);
        Features.v1.Accountability.RenewAccountability.RenewAccountabilityEndpoint.Map(accountability);
        Features.v1.Accountability.ReturnAccountabilityLines.ReturnAccountabilityLinesEndpoint.Map(accountability);
        Features.v1.Accountability.CancelAccountability.CancelAccountabilityEndpoint.Map(accountability);
        Features.v1.Accountability.GetAccountability.GetAccountabilityEndpoint.Map(accountability);
        Features.v1.Accountability.SearchAccountabilities.SearchAccountabilitiesEndpoint.Map(accountability);
        // Employee self-service (My Accountability) — server-scoped to the current employee.
        Features.v1.Accountability.GetMyAccountabilities.GetMyAccountabilitiesEndpoint.Map(accountability);
        Features.v1.Accountability.GetMyAccountableAssets.GetMyAccountableAssetsEndpoint.Map(accountability);
        Features.v1.Accountability.GetMyAccountabilityDetail.GetMyAccountabilityDetailEndpoint.Map(accountability);
        Features.v1.Accountability.AcceptAccountability.AcceptAccountabilityEndpoint.Map(accountability);

        // Issuance reports (SMIR / PPEIR) — atomic transfer document
        var issuance = moduleGroup.MapGroup("/issuance");
        Features.v1.Issuance.CreateIssuanceReport.CreateIssuanceReportEndpoint.Map(issuance);
        Features.v1.Issuance.UpdateIssuanceReportDepreciation.UpdateIssuanceReportDepreciationEndpoint.Map(issuance);
        Features.v1.Issuance.GetIssuanceReport.GetIssuanceReportEndpoint.Map(issuance);
        Features.v1.Issuance.SearchIssuanceReports.SearchIssuanceReportsEndpoint.Map(issuance);
        Features.v1.Issuance.PeekIssuanceReportNumber.PeekIssuanceReportNumberEndpoint.Map(issuance);

        // PPEIR Form Series (pre-printed accountable form management)
        var ppEirSeries = moduleGroup.MapGroup("/ppeir-series");
        Features.v1.Issuance.CreatePPEIRFormSeries.CreatePPEIRFormSeriesEndpoint.Map(ppEirSeries);
        Features.v1.Issuance.UpdatePPEIRFormSeries.UpdatePPEIRFormSeriesEndpoint.Map(ppEirSeries);
        Features.v1.Issuance.DeletePPEIRFormSeries.DeletePPEIRFormSeriesEndpoint.Map(ppEirSeries);
        Features.v1.Issuance.SearchPPEIRFormSeries.SearchPPEIRFormSeriesEndpoint.Map(ppEirSeries);
        Features.v1.Issuance.GetActivePPEIRFormSeries.GetActivePPEIRFormSeriesEndpoint.Map(ppEirSeries);
        Features.v1.Issuance.ActivatePPEIRFormSeries.ActivatePPEIRFormSeriesEndpoint.Map(ppEirSeries);
        Features.v1.Issuance.ActivatePPEIRFormSeries.ActivatePPEIRFormSeriesEndpoint.MapDeactivate(ppEirSeries);

        // Physical count sessions — Phase 4
        var count = moduleGroup.MapGroup("/count");
        Features.v1.Counting.StartPhysicalCount.StartPhysicalCountEndpoint.Map(count);
        Features.v1.Counting.FreezePhysicalCount.FreezePhysicalCountEndpoint.Map(count);
        Features.v1.Counting.RecordPhysicalCountEntry.RecordPhysicalCountEntryEndpoint.Map(count);
        Features.v1.Counting.AddFoundAtStationEntry.AddFoundAtStationEntryEndpoint.Map(count);
        Features.v1.Counting.MarkPhysicalCountMissing.MarkPhysicalCountMissingEndpoint.Map(count);
        Features.v1.Counting.RequestPhysicalCountRecount.RequestPhysicalCountRecountEndpoint.Map(count);
        Features.v1.Counting.ReconcilePhysicalCount.ReconcilePhysicalCountEndpoint.Map(count);
        Features.v1.Counting.ClosePhysicalCount.ClosePhysicalCountEndpoint.Map(count);
        Features.v1.Counting.GetPhysicalCountSession.GetPhysicalCountSessionEndpoint.Map(count);
        Features.v1.Counting.GetPhysicalCountChecklist.GetPhysicalCountChecklistEndpoint.Map(count);
        Features.v1.Counting.GetReconciliationReport.GetReconciliationReportEndpoint.Map(count);
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

        // PPE repairs (RPRI / Exhibit 6) — PPE-wide, keyed by AssetRegistryId
        var repairs = moduleGroup.MapGroup("/repairs");
        Features.v1.Repairs.RepairEndpoints.MapRepairEndpoints(repairs);

        // Receiving reports (PPERR / SMRR)
        var receiving = moduleGroup.MapGroup("/receiving");
        Features.v1.Receiving.CreateReceivingReport.CreateReceivingReportEndpoint.Map(receiving);
        Features.v1.Receiving.DeleteReceivingReport.DeleteReceivingReportEndpoint.Map(receiving);
        Features.v1.Receiving.GetReceivingReport.GetReceivingReportEndpoint.Map(receiving);
        Features.v1.Receiving.SearchReceivingReports.SearchReceivingReportsEndpoint.Map(receiving);
        Features.v1.Receiving.GetReceivedPropertyNumbers.GetReceivedPropertyNumbersEndpoint.Map(receiving);
        Features.v1.Receiving.PeekReceivingReportNumber.PeekReceivingReportNumberEndpoint.Map(receiving);

        // PPERR Form Series (pre-printed accountable form management)
        var ppErrSeries = moduleGroup.MapGroup("/pperr-series");
        Features.v1.Receiving.CreatePPERRFormSeries.CreatePPERRFormSeriesEndpoint.Map(ppErrSeries);
        Features.v1.Receiving.UpdatePPERRFormSeries.UpdatePPERRFormSeriesEndpoint.Map(ppErrSeries);
        Features.v1.Receiving.DeletePPERRFormSeries.DeletePPERRFormSeriesEndpoint.Map(ppErrSeries);
        Features.v1.Receiving.SearchPPERRFormSeries.SearchPPERRFormSeriesEndpoint.Map(ppErrSeries);
        Features.v1.Receiving.GetActivePPERRFormSeries.GetActivePPERRFormSeriesEndpoint.Map(ppErrSeries);
        Features.v1.Receiving.ActivatePPERRFormSeries.ActivatePPERRFormSeriesEndpoint.Map(ppErrSeries);
        Features.v1.Receiving.ActivatePPERRFormSeries.ActivatePPERRFormSeriesEndpoint.MapDeactivate(ppErrSeries);

        // Returned property receipts (RRSP / RRP)
        var returnedProperty = moduleGroup.MapGroup("/returned-property");
        Features.v1.ReturnedProperty.ReturnedPropertyEndpoints.MapReturnedPropertyEndpoints(returnedProperty);

        // Unified inspection worklist — returned-property requests awaiting the current user (self-scoped)
        var inspections = moduleGroup.MapGroup("/inspections");
        Features.v1.Inspections.GetMyPendingReturnedPropertyInspectionsEndpoint.Map(inspections);

        // Signed document copies (scanned wet-signed RRSP / RRP)
        var signedDocuments = moduleGroup.MapGroup("/signed-documents");
        Features.v1.SignedDocuments.UploadSignedDocument.UploadSignedDocumentEndpoint.Map(signedDocuments);
        Features.v1.SignedDocuments.GetSignedDocument.GetSignedDocumentEndpoint.Map(signedDocuments);
        Features.v1.SignedDocuments.DownloadSignedDocument.DownloadSignedDocumentEndpoint.Map(signedDocuments);

        // Locations (asset placement + accountability)
        var locations = moduleGroup.MapGroup("/locations");
        Features.v1.Locations.GetLocations.GetLocationsEndpoint.Map(locations);
        Features.v1.Locations.GetLocationById.GetLocationByIdEndpoint.Map(locations);
        Features.v1.Locations.CreateLocation.CreateLocationEndpoint.Map(locations);
        Features.v1.Locations.UpdateLocation.UpdateLocationEndpoint.Map(locations);
        Features.v1.Locations.DeleteLocation.DeleteLocationEndpoint.Map(locations);

        // Report rendering endpoints (ICS/PAR, RSPI/PPEIR, RPCSEMEX/RPCPPE, RegSPI, RLSDDSP, IIRUSP/IIRUP) — Phase 5
        var reports = moduleGroup.MapGroup("/reports");
        Features.v1.Reports.ReportEndpoints.MapReportEndpoints(reports);
        Features.v1.Reports.GetPropertyCard.GetPropertyCardEndpoint.Map(reports);

        // PPE depreciation (COA GAM straight-line) — post monthly charges + PPE Ledger Card (PPELC)
        var depreciation = moduleGroup.MapGroup("/depreciation");
        Features.v1.Depreciation.RunDepreciation.RunDepreciationEndpoint.Map(depreciation);
        Features.v1.Depreciation.GetPpeLedgerCard.GetPpeLedgerCardEndpoint.Map(depreciation);

        RegisterRecurringJobs(endpoints);
    }

    private static void RegisterRecurringJobs(IEndpointRouteBuilder endpoints)
    {
        var jobManager = endpoints.ServiceProvider.GetService<IRecurringJobManager>();
        var logger = endpoints.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger<AssetRegisterModule>();

        if (jobManager is null)
        {
            return;
        }

        try
        {
            // Drop the retired AssetManagement module's recurring job. Its assembly no longer exists,
            // so Hangfire throws a JobLoadException every schedule tick trying to deserialize it.
            jobManager.RemoveIfExists("asset-management-ics-expiry");

            // Monthly: post COA straight-line depreciation for all PPE assets across every tenant.
            jobManager.AddOrUpdate(
                "asset-register-monthly-depreciation",
                Job.FromExpression<DepreciationRecurringJob>(j => j.RunAsync(CancellationToken.None)),
                Cron.Monthly(),
                new RecurringJobOptions());
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex,
                "Skipping AssetRegister Hangfire recurring job registration due to storage connectivity issue. API startup will continue.");
        }
    }
}


