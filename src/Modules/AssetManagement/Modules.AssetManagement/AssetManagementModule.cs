using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.CreateICS;
using FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.GetExpiringICS;
using FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.GetICSById;
using FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.GetICSList;
using FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.RenewICS;
using FSH.Modules.AssetManagement.Provisioning;
using FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.CreateSemiExpendableItem;
using FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.GetSemiExpendableItemById;
using FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.GetSemiExpendableItems;
using FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.UpdateSemiExpendableItem;
using FSH.Modules.AssetManagement.Features.v1.Reclassification.GetReclassificationHistory;
using FSH.Modules.AssetManagement.Features.v1.Reclassification.ReclassifyProperties;
using FSH.Modules.AssetManagement.Features.v1.PropertyIncidentReports.CreatePropertyIncidentReport;
using FSH.Modules.AssetManagement.Features.v1.Reports.PropertyHistory;
using FSH.Modules.AssetManagement.Features.v1.Reports.RegistryOfSPIssued;
using FSH.Modules.AssetManagement.Features.v1.Reports.ReportOfSPIssued;
using FSH.Modules.AssetManagement.Features.v1.Reports.SemiExpendablePropertyCard;
using FSH.Modules.AssetManagement.Features.v1.UnserviceablePropertyReports.CreateUnserviceablePropertyReport;
using FSH.Modules.AssetManagement.Features.v1.UnserviceablePropertyReports.GetUnserviceablePropertyReportById;
using FSH.Modules.AssetManagement.Features.v1.UnserviceablePropertyReports.GetUnserviceablePropertyReportList;
using FSH.Modules.AssetManagement.Features.v1.PropertyIncidentReports.GetPropertyIncidentReportById;
using FSH.Modules.AssetManagement.Features.v1.PropertyIncidentReports.GetPropertyIncidentReportList;
using FSH.Modules.AssetManagement.Features.v1.ReceiptForReturnedProperties.CreateRRSP;
using FSH.Modules.AssetManagement.Features.v1.ReceiptForReturnedProperties.GetRRSPById;
using FSH.Modules.AssetManagement.Features.v1.ReceiptForReturnedProperties.GetRRSPList;
using FSH.Modules.AssetManagement.Features.v1.SemiExpendableIssuanceRecords.CreateSMIR;
using FSH.Modules.AssetManagement.Features.v1.SemiExpendableIssuanceRecords.GetSMIRById;
using FSH.Modules.AssetManagement.Features.v1.SemiExpendableIssuanceRecords.GetSMIRList;
using FSH.Modules.AssetManagement.Features.v1.PropertyAcknowledgementReceipts.CreatePAR;
using FSH.Modules.AssetManagement.Features.v1.PropertyAcknowledgementReceipts.GetPARById;
using FSH.Modules.AssetManagement.Features.v1.PropertyAcknowledgementReceipts.GetPARList;
using FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.CreatePPEIR;
using FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.GetPPEIRById;
using FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.GetPPEIRList;
using FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.GetPTR;
using FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.UpdatePPEIRDepreciation;
using FSH.Modules.AssetManagement.Features.v1.ReceiptsForReturnedPPE.CreateRRP;
using FSH.Modules.AssetManagement.Features.v1.ReceiptsForReturnedPPE.GetRRPById;
using FSH.Modules.AssetManagement.Features.v1.ReceiptsForReturnedPPE.GetRRPList;
using FSH.Modules.AssetManagement.Features.v1.PhysicalCount.CreatePhysicalCountSession;
using FSH.Modules.AssetManagement.Features.v1.PhysicalCount.RecordPhysicalCountEntry;
using FSH.Modules.AssetManagement.Features.v1.PhysicalCount.AddFoundAtStationEntry;
using FSH.Modules.AssetManagement.Features.v1.PhysicalCount.SubmitPhysicalCountSession;
using FSH.Modules.AssetManagement.Features.v1.PhysicalCount.GetPhysicalCountSessionList;
using FSH.Modules.AssetManagement.Features.v1.PhysicalCount.GetPhysicalCountSessionById;
using FSH.Modules.AssetManagement.Features.v1.PhysicalCount.GetICF;
using FSH.Modules.AssetManagement.Features.v1.PhysicalCount.GetRPCPPE;
using FSH.Modules.AssetManagement.Features.v1.TangibleItems.RegisterTangibleItem;
using FSH.Modules.AssetManagement.Features.v1.TangibleItems.GetTangibleItems;
using FSH.Modules.AssetManagement.Features.v1.TangibleItems.GetTangibleItemById;
using FSH.Modules.AssetManagement.Features.v1.TangibleItems.UpdateTangibleItem;
using FSH.Modules.AssetManagement.Features.v1.TangibleItems.DeleteTangibleItem;
using FSH.Modules.AssetManagement.Features.v1.TangibleItems.GetNextTangibleItemSequence;
using FSH.Modules.AssetManagement.Features.v1.TangibleInventory.CreateTangibleInventory;
using FSH.Modules.AssetManagement.Features.v1.TangibleInventory.GetTangibleInventories;
using FSH.Modules.AssetManagement.Features.v1.TangibleInventory.GetTangibleInventoryById;
using Hangfire;
using Hangfire.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.AssetManagement;

public class AssetManagementModule : IModule
{
    private static readonly IReadOnlyList<FshPermission> RegisteredPermissions =
    [
        new("View Semi-Expendable Items",   "View",   "AssetManagement.SemiExpendableItems",   IsBasic: true),
        new("Create Semi-Expendable Items", "Create", "AssetManagement.SemiExpendableItems"),
        new("Update Semi-Expendable Items", "Update", "AssetManagement.SemiExpendableItems"),
        new("Delete Semi-Expendable Items", "Delete", "AssetManagement.SemiExpendableItems"),

        new("View Tangible Inventory",   "View",   "AssetManagement.TangibleInventory",   IsBasic: true),
        new("Create Tangible Inventory", "Create", "AssetManagement.TangibleInventory"),

        new("View Inventory Custodian Slips",   "View",   "AssetManagement.InventoryCustodianSlips",   IsBasic: true),
        new("Create Inventory Custodian Slips", "Create", "AssetManagement.InventoryCustodianSlips"),
        new("Update Inventory Custodian Slips", "Update", "AssetManagement.InventoryCustodianSlips"),
        new("Delete Inventory Custodian Slips", "Delete", "AssetManagement.InventoryCustodianSlips"),

        new("View Reclassification History", "View",   "AssetManagement.Reclassification", IsBasic: true),
        new("Reclassify Properties",         "Create", "AssetManagement.Reclassification"),

        new("View Semi-Expendable Issuance Records",   "View",   "AssetManagement.SemiExpendableIssuanceRecords", IsBasic: true),
        new("Create Semi-Expendable Issuance Records", "Create", "AssetManagement.SemiExpendableIssuanceRecords"),

        new("View Receipts for Returned Properties",   "View",   "AssetManagement.ReceiptForReturnedProperties", IsBasic: true),
        new("Create Receipt for Returned Property",    "Create", "AssetManagement.ReceiptForReturnedProperties"),

        new("View Property Incident Reports",   "View",   "AssetManagement.PropertyIncidentReports", IsBasic: true),
        new("Create Property Incident Report",  "Create", "AssetManagement.PropertyIncidentReports"),

        new("View Unserviceable Property Reports",   "View",   "AssetManagement.UnserviceablePropertyReports", IsBasic: true),
        new("Create Unserviceable Property Report",  "Create", "AssetManagement.UnserviceablePropertyReports"),

        new("View Semi-Expendable Property Reports", "View",   "AssetManagement.Reports", IsBasic: true),

        // PPE track
        new("View PPE Items",   "View",   "AssetManagement.PPEItems", IsBasic: true),
        new("Create PPE Items", "Create", "AssetManagement.PPEItems"),
        new("Update PPE Items", "Update", "AssetManagement.PPEItems"),
        new("Delete PPE Items", "Delete", "AssetManagement.PPEItems"),

        new("View PPE Receiving Reports",   "View",   "AssetManagement.PPEReceivingReports", IsBasic: true),
        new("Create PPE Receiving Report",  "Create", "AssetManagement.PPEReceivingReports"),

        new("View Property Acknowledgement Receipts",   "View",   "AssetManagement.PropertyAcknowledgementReceipts", IsBasic: true),
        new("Create Property Acknowledgement Receipt",  "Create", "AssetManagement.PropertyAcknowledgementReceipts"),

        new("View PPE Issuance Reports",              "View",   "AssetManagement.PPEIssuanceReports", IsBasic: true),
        new("Create PPE Issuance Report",             "Create", "AssetManagement.PPEIssuanceReports"),
        new("Update PPE Issuance Report Depreciation","Update", "AssetManagement.PPEIssuanceReports"),

        new("View Receipts for Returned PPE",   "View",   "AssetManagement.ReceiptsForReturnedPPE", IsBasic: true),
        new("Create Receipt for Returned PPE",  "Create", "AssetManagement.ReceiptsForReturnedPPE"),

        // Physical Count
        new("View Physical Count Sessions",   "View",   "AssetManagement.PhysicalCount", IsBasic: true),
        new("Create Physical Count Session",  "Create", "AssetManagement.PhysicalCount"),
        new("Update Physical Count Session",  "Update", "AssetManagement.PhysicalCount"),

        // Tangible Items
        new("View Tangible Items",   "View",   "AssetManagement.TangibleItems",   IsBasic: true),
        new("Create Tangible Item",  "Create", "AssetManagement.TangibleItems"),
        new("Update Tangible Item",  "Update", "AssetManagement.TangibleItems"),
        new("Delete Tangible Item",  "Delete", "AssetManagement.TangibleItems"),
    ];

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(RegisteredPermissions);
        builder.Services.AddHeroDbContext<AssetManagementDbContext>();
        builder.Services.AddScoped<IDbInitializer, AssetManagementDbInitializer>();
        builder.Services.AddScoped<ICSExpiryJob>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var moduleGroup = endpoints
            .MapGroup("api/v{version:apiVersion}/asset-management")
            .WithTags("AssetManagement")
            .WithApiVersionSet(apiVersionSet);

        var semiExpendableItemsGroup      = moduleGroup.MapGroup("/item-catalog");
        var icsGroup                      = moduleGroup.MapGroup("/inventory-custodian-slips");
        var reclassificationGroup         = moduleGroup.MapGroup("/reclassification");
        var smirGroup                     = moduleGroup.MapGroup("/semi-expendable-issuance-records");
        var rrspGroup                     = moduleGroup.MapGroup("/receipt-for-returned-properties");
        var pirGroup                      = moduleGroup.MapGroup("/property-incident-reports");
        var unserviceableGroup            = moduleGroup.MapGroup("/unserviceable-property-reports");
        var reportsGroup                  = moduleGroup.MapGroup("/reports");
        var parGroup                      = moduleGroup.MapGroup("/property-acknowledgement-receipts");
        var ppeirGroup                    = moduleGroup.MapGroup("/ppe-issuance-reports");
        var rrpGroup                      = moduleGroup.MapGroup("/receipts-for-returned-ppe");
        var physicalCountGroup            = moduleGroup.MapGroup("/physical-count");
        var tangibleItemsGroup            = moduleGroup.MapGroup("/tangible-items");
        var tangibleInventoryGroup        = moduleGroup.MapGroup("/tangible-inventories");

        // Semi-Expendable Items (Item Catalog)
        CreateSemiExpendableItemEndpoint.Map(semiExpendableItemsGroup);
        GetSemiExpendableItemsEndpoint.Map(semiExpendableItemsGroup);
        GetSemiExpendableItemByIdEndpoint.Map(semiExpendableItemsGroup);
        UpdateSemiExpendableItemEndpoint.Map(semiExpendableItemsGroup);

        // Tangible Inventory (unified receiving — SE and PPE)
        CreateTangibleInventoryEndpoint.Map(tangibleInventoryGroup);
        GetTangibleInventoriesEndpoint.Map(tangibleInventoryGroup);
        GetTangibleInventoryByIdEndpoint.Map(tangibleInventoryGroup);

        // Inventory Custodian Slips (ICS)
        CreateICSEndpoint.Map(icsGroup);
        GetICSListEndpoint.Map(icsGroup);
        GetICSByIdEndpoint.Map(icsGroup);
        GetExpiringICSEndpoint.Map(icsGroup);
        RenewICSEndpoint.Map(icsGroup);

        // Reclassification
        ReclassifyPropertiesEndpoint.Map(reclassificationGroup);
        GetReclassificationHistoryEndpoint.Map(reclassificationGroup);

        // Semi-Expendable Issuance Records (SMIR)
        CreateSMIREndpoint.Map(smirGroup);
        GetSMIRListEndpoint.Map(smirGroup);
        GetSMIRByIdEndpoint.Map(smirGroup);

        // Receipt for Returned Semi-Expendable Properties (RRSP)
        CreateRRSPEndpoint.Map(rrspGroup);
        GetRRSPListEndpoint.Map(rrspGroup);
        GetRRSPByIdEndpoint.Map(rrspGroup);

        // Property Incident Reports (RLSDDSP — Lost/Stolen/Damaged/Destroyed)
        CreatePropertyIncidentReportEndpoint.Map(pirGroup);
        GetPropertyIncidentReportListEndpoint.Map(pirGroup);
        GetPropertyIncidentReportByIdEndpoint.Map(pirGroup);

        // Unserviceable Property Reports (IIRUSP — Inspection and Inventory Report)
        CreateUnserviceablePropertyReportEndpoint.Map(unserviceableGroup);
        GetUnserviceablePropertyReportListEndpoint.Map(unserviceableGroup);
        GetUnserviceablePropertyReportByIdEndpoint.Map(unserviceableGroup);

        // Property Acknowledgement Receipts (PAR)
        CreatePAREndpoint.Map(parGroup);
        GetPARListEndpoint.Map(parGroup);
        GetPARByIdEndpoint.Map(parGroup);

        // PPE Issuance Reports (PPEIR) + PTR report
        CreatePPEIREndpoint.Map(ppeirGroup);
        GetPPEIRListEndpoint.Map(ppeirGroup);
        GetPPEIRByIdEndpoint.Map(ppeirGroup);
        GetPTREndpoint.Map(ppeirGroup);
        UpdatePPEIRDepreciationEndpoint.Map(ppeirGroup);

        // Receipts for Returned PPE (RRP)
        CreateRRPEndpoint.Map(rrpGroup);
        GetRRPListEndpoint.Map(rrpGroup);
        GetRRPByIdEndpoint.Map(rrpGroup);

        // Physical Count (ICF / RPCPPE)
        CreatePhysicalCountSessionEndpoint.Map(physicalCountGroup);
        GetPhysicalCountSessionListEndpoint.Map(physicalCountGroup);
        GetPhysicalCountSessionByIdEndpoint.Map(physicalCountGroup);
        RecordPhysicalCountEntryEndpoint.Map(physicalCountGroup);
        AddFoundAtStationEntryEndpoint.Map(physicalCountGroup);
        SubmitPhysicalCountSessionEndpoint.Map(physicalCountGroup);
        GetICFEndpoint.Map(physicalCountGroup);
        GetRPCPPEEndpoint.Map(physicalCountGroup);

        // Reports (SPC, RegSPI, RSPI, Property History)
        GetSPCEndpoint.Map(reportsGroup);
        GetRegSPIEndpoint.Map(reportsGroup);
        GetRSPIEndpoint.Map(reportsGroup);
        GetPropertyHistoryEndpoint.Map(reportsGroup);

        // Tangible Items
        RegisterTangibleItemEndpoint.Map(tangibleItemsGroup);
        GetTangibleItemsEndpoint.Map(tangibleItemsGroup);
        GetTangibleItemByIdEndpoint.Map(tangibleItemsGroup);
        UpdateTangibleItemEndpoint.Map(tangibleItemsGroup);
        DeleteTangibleItemEndpoint.Map(tangibleItemsGroup);
        GetNextTangibleItemSequenceEndpoint.Map(tangibleItemsGroup);

        // Hangfire recurring jobs
        RegisterRecurringJobs(endpoints);
    }

    private static void RegisterRecurringJobs(IEndpointRouteBuilder endpoints)
    {
        var jobManager  = endpoints.ServiceProvider.GetService<IRecurringJobManager>();
        var loggerFactory = endpoints.ServiceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger<AssetManagementModule>();

        if (jobManager is null)
        {
            return;
        }

        try
        {
            // Daily at midnight UTC: expire ICS records past their ExpiresOn date.
            jobManager.AddOrUpdate(
                "asset-management-ics-expiry",
                Job.FromExpression<ICSExpiryJob>(j => j.RunAsync(CancellationToken.None)),
                Cron.Daily(),
                new RecurringJobOptions());
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex,
                "Skipping AssetManagement Hangfire recurring job registration due to storage connectivity issue. API startup will continue.");
        }
    }
}
