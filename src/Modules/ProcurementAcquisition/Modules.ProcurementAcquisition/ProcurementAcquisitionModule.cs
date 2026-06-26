using Asp.Versioning;
using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Web.Modules;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CreatePurchaseRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.UpdatePurchaseRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.SubmitPurchaseRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CertifyFundsAvailable;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.ApprovePurchaseRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.ReturnForRevision;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.RejectPurchaseRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CancelPurchaseRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.GetPurchaseRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.SearchPurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.GetStatusCounts;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.CreateCanvassRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.AddQuotation;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.UpdateQuotation;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.AwardCanvass;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.GetCanvassRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.SearchCanvassRequests;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.GetStatusCounts;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.GetCanvassablePrLines;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.GetAwardedPrLines;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrdersFromCanvass;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.UpdatePurchaseOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.SubmitPurchaseOrder;
using PoCertifyFundsAvailable = AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CertifyFundsAvailable;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.IssuePurchaseOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CancelPurchaseOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.GetPurchaseOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.SearchPurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CreateJobOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.UpdateJobOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.SubmitJobOrder;
using JoCertifyFundsAvailable = AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CertifyFundsAvailable;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.IssueJobOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.InspectJobOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.AcceptJobOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CancelJobOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.GetJobOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.SearchJobOrders;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.CreateInspectionAcceptanceReport;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.UpdateInspectionAcceptanceReport;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.AcceptInspectionAcceptanceReport;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.CancelInspectionAcceptanceReport;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.GetInspectionAcceptanceReport;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.SearchInspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.GetStatusCounts;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.SearchAcceptedIARLineItems;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.SubmitForInspection;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.ReassignInspector;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.RecordInspection;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.AssignPropertyNo;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.ExpandLineByQuantity;
using AMIS.Modules.ProcurementAcquisition.Features.v1.SignedDocuments.UploadSignedDocument;
using AMIS.Modules.ProcurementAcquisition.Features.v1.SignedDocuments.GetSignedDocument;
using AMIS.Modules.ProcurementAcquisition.Features.v1.SignedDocuments.DownloadSignedDocument;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AMIS.Modules.ProcurementAcquisition;

public class ProcurementAcquisitionModule : IModule
{
    private static readonly IReadOnlyList<AmisPermission> RegisteredPermissions =
    [
        new("View Purchase Requests", "View", "Procurement.PurchaseRequests", IsBasic: true),
        new("Create Purchase Requests", "Create", "Procurement.PurchaseRequests"),
        new("Update Purchase Requests", "Update", "Procurement.PurchaseRequests"),
        new("Submit Purchase Requests", "Submit", "Procurement.PurchaseRequests"),
        new("Certify Funds Available on Purchase Requests", "CertifyFundsAvailable", "Procurement.PurchaseRequests"),
        new("Approve Purchase Requests", "Approve", "Procurement.PurchaseRequests"),
        new("Return Purchase Requests for Revision", "ReturnForRevision", "Procurement.PurchaseRequests"),
        new("Reject Purchase Requests", "Reject", "Procurement.PurchaseRequests"),
        new("Cancel Purchase Requests", "Cancel", "Procurement.PurchaseRequests"),

        new("View Canvass Requests", "View", "Procurement.CanvassRequests", IsBasic: true),
        new("Create Canvass Requests", "Create", "Procurement.CanvassRequests"),
        new("Update Canvass Requests", "Update", "Procurement.CanvassRequests"),
        new("Award Canvass Requests", "Award", "Procurement.CanvassRequests"),
        new("Cancel Canvass Requests", "Cancel", "Procurement.CanvassRequests"),

        new("View Purchase Orders", "View", "Procurement.PurchaseOrders", IsBasic: true),
        new("Create Purchase Orders", "Create", "Procurement.PurchaseOrders"),
        new("Update Purchase Orders", "Update", "Procurement.PurchaseOrders"),
        new("Submit Purchase Orders", "Submit", "Procurement.PurchaseOrders"),
        new("Certify Funds Available on Purchase Orders", "CertifyFundsAvailable", "Procurement.PurchaseOrders"),
        new("Issue Purchase Orders", "Issue", "Procurement.PurchaseOrders"),
        new("Cancel Purchase Orders", "Cancel", "Procurement.PurchaseOrders"),

        new("View Job Orders", "View", "Procurement.JobOrders", IsBasic: true),
        new("Create Job Orders", "Create", "Procurement.JobOrders"),
        new("Update Job Orders", "Update", "Procurement.JobOrders"),
        new("Submit Job Orders", "Submit", "Procurement.JobOrders"),
        new("Certify Funds Available on Job Orders", "CertifyFundsAvailable", "Procurement.JobOrders"),
        new("Issue Job Orders", "Issue", "Procurement.JobOrders"),
        new("Inspect Job Orders", "Inspect", "Procurement.JobOrders"),
        new("Accept Job Orders", "Accept", "Procurement.JobOrders"),
        new("Cancel Job Orders", "Cancel", "Procurement.JobOrders"),

        new("View Signed Document Copies", "View", "Procurement.SignedDocuments", IsBasic: true),
        new("Upload Signed Document Copies", "Upload", "Procurement.SignedDocuments"),

        new("View Asset IARs",                "View",                "Procurement.InspectionAcceptanceReports", IsBasic: true),
        new("Create Asset IARs",              "Create",              "Procurement.InspectionAcceptanceReports"),
        new("Update Asset IARs",              "Update",              "Procurement.InspectionAcceptanceReports"),
        new("Accept Asset IARs",              "Accept",              "Procurement.InspectionAcceptanceReports"),
        new("Submit Asset IARs For Inspection","SubmitForInspection","Procurement.InspectionAcceptanceReports"),
        new("Inspect Asset IARs",             "Inspect",             "Procurement.InspectionAcceptanceReports"),
        new("Assign Property No",             "AssignPropertyNo",    "Procurement.InspectionAcceptanceReports"),
        new("Expand IAR Lines",               "ExpandLine",          "Procurement.InspectionAcceptanceReports"),
        new("Cancel Asset IARs",              "Cancel",              "Procurement.InspectionAcceptanceReports"),
    ];

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;

        PermissionConstants.Register(RegisteredPermissions);

        services.AddHeroDbContext<ProcurementDbContext>();
        services.AddScoped<IDbInitializer, ProcurementDbInitializer>();
        services.AddHostedService<AMIS.Modules.ProcurementAcquisition.Provisioning.ProcurementDbInitializerHostedService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var moduleGroup = endpoints
            .MapGroup("api/v{version:apiVersion}/procurement")
            .WithTags("Procurement")
            .WithApiVersionSet(apiVersionSet);

        var purchaseRequestsGroup = moduleGroup.MapGroup("/purchase-requests");
        var canvassRequestsGroup = moduleGroup.MapGroup("/canvass-requests");
        var purchaseOrdersGroup = moduleGroup.MapGroup("/purchase-orders");
        var jobOrdersGroup = moduleGroup.MapGroup("/job-orders");
        var iarGroup = moduleGroup.MapGroup("/inspection-acceptance-reports");
        var signedDocumentsGroup = moduleGroup.MapGroup("/signed-documents");

        // Purchase Requests
        CreatePurchaseRequestEndpoint.Map(purchaseRequestsGroup);
        UpdatePurchaseRequestEndpoint.Map(purchaseRequestsGroup);
        SubmitPurchaseRequestEndpoint.Map(purchaseRequestsGroup);
        CertifyFundsAvailableEndpoint.Map(purchaseRequestsGroup);
        ApprovePurchaseRequestEndpoint.Map(purchaseRequestsGroup);
        ReturnPurchaseRequestForRevisionEndpoint.Map(purchaseRequestsGroup);
        RejectPurchaseRequestEndpoint.Map(purchaseRequestsGroup);
        CancelPurchaseRequestEndpoint.Map(purchaseRequestsGroup);
        GetPurchaseRequestEndpoint.Map(purchaseRequestsGroup);
        SearchPurchaseRequestsEndpoint.Map(purchaseRequestsGroup);
        GetPurchaseRequestStatusCountsEndpoint.Map(purchaseRequestsGroup);

        // Canvass Requests
        CreateCanvassRequestEndpoint.Map(canvassRequestsGroup);
        AddQuotationEndpoint.Map(canvassRequestsGroup);
        UpdateQuotationEndpoint.Map(canvassRequestsGroup);
        AwardCanvassEndpoint.Map(canvassRequestsGroup);
        GetCanvassRequestEndpoint.Map(canvassRequestsGroup);
        SearchCanvassRequestsEndpoint.Map(canvassRequestsGroup);
        GetCanvassRequestStatusCountsEndpoint.Map(canvassRequestsGroup);
        GetCanvassablePrLinesEndpoint.Map(canvassRequestsGroup);
        GetAwardedPrLinesEndpoint.Map(canvassRequestsGroup);

        // Purchase Orders
        CreatePurchaseOrderEndpoint.Map(purchaseOrdersGroup);
        CreatePurchaseOrdersFromCanvassEndpoint.Map(purchaseOrdersGroup);
        UpdatePurchaseOrderEndpoint.Map(purchaseOrdersGroup);
        SubmitPurchaseOrderEndpoint.Map(purchaseOrdersGroup);
        PoCertifyFundsAvailable.CertifyPurchaseOrderFundsAvailableEndpoint.Map(purchaseOrdersGroup);
        IssuePurchaseOrderEndpoint.Map(purchaseOrdersGroup);
        CancelPurchaseOrderEndpoint.Map(purchaseOrdersGroup);
        GetPurchaseOrderEndpoint.Map(purchaseOrdersGroup);
        SearchPurchaseOrdersEndpoint.Map(purchaseOrdersGroup);

        // Job Orders (works: renovation / repair / fabrication)
        CreateJobOrderEndpoint.Map(jobOrdersGroup);
        UpdateJobOrderEndpoint.Map(jobOrdersGroup);
        SubmitJobOrderEndpoint.Map(jobOrdersGroup);
        JoCertifyFundsAvailable.CertifyJobOrderFundsAvailableEndpoint.Map(jobOrdersGroup);
        IssueJobOrderEndpoint.Map(jobOrdersGroup);
        InspectJobOrderEndpoint.Map(jobOrdersGroup);
        AcceptJobOrderEndpoint.Map(jobOrdersGroup);
        CancelJobOrderEndpoint.Map(jobOrdersGroup);
        GetJobOrderEndpoint.Map(jobOrdersGroup);
        SearchJobOrdersEndpoint.Map(jobOrdersGroup);

        // Asset IARs
        CreateInspectionAcceptanceReportEndpoint.Map(iarGroup);
        UpdateInspectionAcceptanceReportEndpoint.Map(iarGroup);
        AcceptInspectionAcceptanceReportEndpoint.Map(iarGroup);
        CancelInspectionAcceptanceReportEndpoint.Map(iarGroup);
        GetInspectionAcceptanceReportEndpoint.Map(iarGroup);
        SearchInspectionAcceptanceReportsEndpoint.Map(iarGroup);
        GetIARStatusCountsEndpoint.Map(iarGroup);
        SearchAcceptedIARLineItemsEndpoint.Map(iarGroup);
        SubmitIARForInspectionEndpoint.Map(iarGroup);
        ReassignInspectorEndpoint.Map(iarGroup);
        RecordIARInspectionEndpoint.Map(iarGroup);
        AssignPropertyNoEndpoint.Map(iarGroup);
        ExpandLineByQuantityEndpoint.Map(iarGroup);

        // Signed Documents (wet-signed scanned copies of records)
        UploadSignedDocumentEndpoint.Map(signedDocumentsGroup);
        GetSignedDocumentEndpoint.Map(signedDocumentsGroup);
        DownloadSignedDocumentEndpoint.Map(signedDocumentsGroup);
    }
}


