using Asp.Versioning;
using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Web.Modules;
using AMIS.Modules.Finance.Data;
using AMIS.Modules.Finance.Features.v1.DisbursementVouchers.CreateDisbursementVoucher;
using AMIS.Modules.Finance.Features.v1.DisbursementVouchers.UpdateDisbursementVoucher;
using AMIS.Modules.Finance.Features.v1.DisbursementVouchers.GetDisbursementVoucherById;
using AMIS.Modules.Finance.Features.v1.DisbursementVouchers.SearchDisbursementVouchers;
using AMIS.Modules.Finance.Features.v1.DisbursementVouchers.ApproveDisbursementVoucher;
using AMIS.Modules.Finance.Features.v1.DisbursementVouchers.PayDisbursementVoucher;
using AMIS.Modules.Finance.Features.v1.DisbursementVouchers.ReturnDisbursementVoucher;
using AMIS.Modules.Finance.Features.v1.DisbursementVouchers.CancelDisbursementVoucher;
using AMIS.Modules.Finance.Features.v1.BudgetUtilizationRecords.CreateBudgetUtilizationRecord;
using AMIS.Modules.Finance.Features.v1.BudgetUtilizationRecords.GetBudgetUtilizationRecordById;
using AMIS.Modules.Finance.Features.v1.BudgetUtilizationRecords.SearchBudgetUtilizationRecords;
using AMIS.Modules.Finance.Features.v1.BudgetUtilizationRecords.ObligateBudgetUtilizationRecord;
using AMIS.Modules.Finance.Features.v1.BudgetUtilizationRecords.UtilizeBudgetUtilizationRecord;
using AMIS.Modules.Finance.Features.v1.BudgetUtilizationRecords.CancelBudgetUtilizationRecord;
using AMIS.Modules.Finance.Features.v1.SignedDocuments.UploadSignedDocument;
using AMIS.Modules.Finance.Features.v1.SignedDocuments.GetSignedDocument;
using AMIS.Modules.Finance.Features.v1.SignedDocuments.DownloadSignedDocument;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AMIS.Modules.Finance;

public class FinanceModule : IModule
{
    private static readonly IReadOnlyList<AmisPermission> RegisteredPermissions =
    [
        new("View Disbursement Vouchers", "View", "Finance.DisbursementVouchers", IsBasic: false),
        new("Create Disbursement Vouchers", "Create", "Finance.DisbursementVouchers"),
        new("Approve Disbursement Vouchers", "Approve", "Finance.DisbursementVouchers"),
        new("Pay Disbursement Vouchers", "Pay", "Finance.DisbursementVouchers"),
        new("Return Disbursement Vouchers", "Return", "Finance.DisbursementVouchers"),
        new("Cancel Disbursement Vouchers", "Cancel", "Finance.DisbursementVouchers"),

        new("View Budget Utilization Request and Status", "View", "Finance.BudgetUtilizationRecords", IsBasic: false),
        new("Create Budget Utilization Request and Status", "Create", "Finance.BudgetUtilizationRecords"),
        new("Obligate Budget Utilization Request and Status", "Obligate", "Finance.BudgetUtilizationRecords"),
        new("Utilize Budget Utilization Request and Status", "Utilize", "Finance.BudgetUtilizationRecords"),
        new("Cancel Budget Utilization Request and Status", "Cancel", "Finance.BudgetUtilizationRecords"),

        new("View Signed Document Copies", "View", "Finance.SignedDocuments", IsBasic: true),
        new("Upload Signed Document Copies", "Upload", "Finance.SignedDocuments")
    ];

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(RegisteredPermissions);
        builder.Services.AddHeroDbContext<FinanceDbContext>();
        builder.Services.AddScoped<IDbInitializer, FinanceDbInitializer>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var moduleGroup = endpoints
            .MapGroup("api/v{version:apiVersion}/finance")
            .WithTags("Finance")
            .WithApiVersionSet(apiVersionSet);

        var dvGroup = moduleGroup.MapGroup("/disbursement-vouchers");
        var burGroup = moduleGroup.MapGroup("/budget-utilization-records");
        var signedDocumentsGroup = moduleGroup.MapGroup("/signed-documents");

        // Disbursement Vouchers
        CreateDisbursementVoucherEndpoint.Map(dvGroup);
        UpdateDisbursementVoucherEndpoint.Map(dvGroup);
        GetDisbursementVoucherByIdEndpoint.Map(dvGroup);
        SearchDisbursementVouchersEndpoint.Map(dvGroup);
        ApproveDisbursementVoucherEndpoint.Map(dvGroup);
        PayDisbursementVoucherEndpoint.Map(dvGroup);
        ReturnDisbursementVoucherEndpoint.Map(dvGroup);
        CancelDisbursementVoucherEndpoint.Map(dvGroup);

        // Budget Utilization Records
        CreateBudgetUtilizationRecordEndpoint.Map(burGroup);
        GetBudgetUtilizationRecordByIdEndpoint.Map(burGroup);
        SearchBudgetUtilizationRecordsEndpoint.Map(burGroup);
        ObligateBudgetUtilizationRecordEndpoint.Map(burGroup);
        UtilizeBudgetUtilizationRecordEndpoint.Map(burGroup);
        CancelBudgetUtilizationRecordEndpoint.Map(burGroup);

        // Signed Documents (wet-signed scanned copies of records)
        UploadSignedDocumentEndpoint.Map(signedDocumentsGroup);
        GetSignedDocumentEndpoint.Map(signedDocumentsGroup);
        DownloadSignedDocumentEndpoint.Map(signedDocumentsGroup);
    }
}


