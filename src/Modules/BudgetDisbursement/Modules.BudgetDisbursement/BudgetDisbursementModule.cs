using Asp.Versioning;
using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Web.Modules;
using AMIS.Modules.BudgetDisbursement.Data;
using AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.CreateDisbursementVoucher;
using AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.UpdateDisbursementVoucher;
using AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.GetDisbursementVoucherById;
using AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.SearchDisbursementVouchers;
using AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.GetStatusCounts;
using AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.ApproveDisbursementVoucher;
using AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.PayDisbursementVoucher;
using AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.ReturnDisbursementVoucher;
using AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.CancelDisbursementVoucher;
using AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.CreateBudgetUtilizationRequest;
using AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.DeleteBudgetUtilizationRequest;
using AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.DeleteDisbursementVoucher;
using AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.GetBudgetUtilizationRequestById;
using AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.SearchBudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.GetStatusCounts;
using AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.ObligateBudgetUtilizationRequest;
using AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.UtilizeBudgetUtilizationRequest;
using AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.CancelBudgetUtilizationRequest;
using AMIS.Modules.BudgetDisbursement.Features.v1.SignedDocuments.UploadSignedDocument;
using AMIS.Modules.BudgetDisbursement.Features.v1.SignedDocuments.GetSignedDocument;
using AMIS.Modules.BudgetDisbursement.Features.v1.SignedDocuments.DownloadSignedDocument;
using AMIS.Modules.BudgetDisbursement.Features.v1.Settings.GetBudgetDisbursementSettings;
using AMIS.Modules.BudgetDisbursement.Features.v1.Settings.UpdateBudgetDisbursementSettings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AMIS.Modules.BudgetDisbursement;

public class BudgetDisbursementModule : IModule
{
    private static readonly IReadOnlyList<AmisPermission> RegisteredPermissions =
    [
        new("View Disbursement Vouchers", "View", "BudgetDisbursement.DisbursementVouchers", IsBasic: false),
        new("Create Disbursement Vouchers", "Create", "BudgetDisbursement.DisbursementVouchers"),
        new("Delete Disbursement Vouchers", "Delete", "BudgetDisbursement.DisbursementVouchers"),
        new("Approve Disbursement Vouchers", "Approve", "BudgetDisbursement.DisbursementVouchers"),
        new("Pay Disbursement Vouchers", "Pay", "BudgetDisbursement.DisbursementVouchers"),
        new("Return Disbursement Vouchers", "Return", "BudgetDisbursement.DisbursementVouchers"),
        new("Cancel Disbursement Vouchers", "Cancel", "BudgetDisbursement.DisbursementVouchers"),

        new("View Budget Utilization Request and Status", "View", "BudgetDisbursement.BudgetUtilizationRequests", IsBasic: false),
        new("Create Budget Utilization Request and Status", "Create", "BudgetDisbursement.BudgetUtilizationRequests"),
        new("Delete Budget Utilization Request and Status", "Delete", "BudgetDisbursement.BudgetUtilizationRequests"),
        new("Obligate Budget Utilization Request and Status", "Obligate", "BudgetDisbursement.BudgetUtilizationRequests"),
        new("Utilize Budget Utilization Request and Status", "Utilize", "BudgetDisbursement.BudgetUtilizationRequests"),
        new("Cancel Budget Utilization Request and Status", "Cancel", "BudgetDisbursement.BudgetUtilizationRequests"),

        new("View Signed Document Copies", "View", "BudgetDisbursement.SignedDocuments", IsBasic: true),
        new("Upload Signed Document Copies", "Upload", "BudgetDisbursement.SignedDocuments"),

        new("View Budget Disbursement Settings", "View", "BudgetDisbursement.Settings"),
        new("Update Budget Disbursement Settings", "Update", "BudgetDisbursement.Settings")
    ];

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(RegisteredPermissions);
        builder.Services.AddHeroDbContext<BudgetDisbursementDbContext>();
        builder.Services.AddScoped<IDbInitializer, BudgetDisbursementDbInitializer>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var moduleGroup = endpoints
            .MapGroup("api/v{version:apiVersion}/budget-disbursement")
            .WithTags("BudgetDisbursement")
            .WithApiVersionSet(apiVersionSet);

        var dvGroup = moduleGroup.MapGroup("/disbursement-vouchers");
        var burGroup = moduleGroup.MapGroup("/budget-utilization-requests");
        var signedDocumentsGroup = moduleGroup.MapGroup("/signed-documents");
        var settingsGroup = moduleGroup.MapGroup("/settings");

        // Disbursement Vouchers
        CreateDisbursementVoucherEndpoint.Map(dvGroup);
        DeleteDisbursementVoucherEndpoint.Map(dvGroup);
        UpdateDisbursementVoucherEndpoint.Map(dvGroup);
        GetDisbursementVoucherByIdEndpoint.Map(dvGroup);
        SearchDisbursementVouchersEndpoint.Map(dvGroup);
        GetDisbursementVoucherStatusCountsEndpoint.Map(dvGroup);
        ApproveDisbursementVoucherEndpoint.Map(dvGroup);
        PayDisbursementVoucherEndpoint.Map(dvGroup);
        ReturnDisbursementVoucherEndpoint.Map(dvGroup);
        CancelDisbursementVoucherEndpoint.Map(dvGroup);

        // Budget Utilization Requests
        CreateBudgetUtilizationRequestEndpoint.Map(burGroup);
        DeleteBudgetUtilizationRequestEndpoint.Map(burGroup);
        GetBudgetUtilizationRequestByIdEndpoint.Map(burGroup);
        SearchBudgetUtilizationRequestsEndpoint.Map(burGroup);
        GetBudgetUtilizationRequestStatusCountsEndpoint.Map(burGroup);
        ObligateBudgetUtilizationRequestEndpoint.Map(burGroup);
        UtilizeBudgetUtilizationRequestEndpoint.Map(burGroup);
        CancelBudgetUtilizationRequestEndpoint.Map(burGroup);

        // Signed Documents (wet-signed scanned copies of records)
        UploadSignedDocumentEndpoint.Map(signedDocumentsGroup);
        GetSignedDocumentEndpoint.Map(signedDocumentsGroup);
        DownloadSignedDocumentEndpoint.Map(signedDocumentsGroup);

        // Budget Disbursement module admin settings
        GetBudgetDisbursementSettingsEndpoint.Map(settingsGroup);
        UpdateBudgetDisbursementSettingsEndpoint.Map(settingsGroup);
    }
}


