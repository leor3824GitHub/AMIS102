using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Finance.Data;
using FSH.Modules.Finance.Features.v1.DisbursementVouchers.CreateDisbursementVoucher;
using FSH.Modules.Finance.Features.v1.DisbursementVouchers.GetDisbursementVoucherById;
using FSH.Modules.Finance.Features.v1.DisbursementVouchers.SearchDisbursementVouchers;
using FSH.Modules.Finance.Features.v1.DisbursementVouchers.ApproveDisbursementVoucher;
using FSH.Modules.Finance.Features.v1.DisbursementVouchers.PayDisbursementVoucher;
using FSH.Modules.Finance.Features.v1.DisbursementVouchers.CancelDisbursementVoucher;
using FSH.Modules.Finance.Features.v1.BudgetUtilizationRecords.CreateBudgetUtilizationRecord;
using FSH.Modules.Finance.Features.v1.BudgetUtilizationRecords.GetBudgetUtilizationRecordById;
using FSH.Modules.Finance.Features.v1.BudgetUtilizationRecords.SearchBudgetUtilizationRecords;
using FSH.Modules.Finance.Features.v1.BudgetUtilizationRecords.ObligateBudgetUtilizationRecord;
using FSH.Modules.Finance.Features.v1.BudgetUtilizationRecords.CancelBudgetUtilizationRecord;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FSH.Modules.Finance;

public class FinanceModule : IModule
{
    private static readonly IReadOnlyList<FshPermission> RegisteredPermissions =
    [
        new("View Disbursement Vouchers", "View", "Finance.DisbursementVouchers", IsBasic: false),
        new("Create Disbursement Vouchers", "Create", "Finance.DisbursementVouchers"),
        new("Approve Disbursement Vouchers", "Approve", "Finance.DisbursementVouchers"),
        new("Pay Disbursement Vouchers", "Pay", "Finance.DisbursementVouchers"),
        new("Return Disbursement Vouchers", "Return", "Finance.DisbursementVouchers"),
        new("Cancel Disbursement Vouchers", "Cancel", "Finance.DisbursementVouchers"),

        new("View Budget Utilization Records", "View", "Finance.BudgetUtilizationRecords", IsBasic: false),
        new("Create Budget Utilization Records", "Create", "Finance.BudgetUtilizationRecords"),
        new("Obligate Budget Utilization Records", "Obligate", "Finance.BudgetUtilizationRecords"),
        new("Utilize Budget Utilization Records", "Utilize", "Finance.BudgetUtilizationRecords"),
        new("Cancel Budget Utilization Records", "Cancel", "Finance.BudgetUtilizationRecords")
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

        // Disbursement Vouchers
        CreateDisbursementVoucherEndpoint.Map(dvGroup);
        GetDisbursementVoucherByIdEndpoint.Map(dvGroup);
        SearchDisbursementVouchersEndpoint.Map(dvGroup);
        ApproveDisbursementVoucherEndpoint.Map(dvGroup);
        PayDisbursementVoucherEndpoint.Map(dvGroup);
        CancelDisbursementVoucherEndpoint.Map(dvGroup);

        // Budget Utilization Records
        CreateBudgetUtilizationRecordEndpoint.Map(burGroup);
        GetBudgetUtilizationRecordByIdEndpoint.Map(burGroup);
        SearchBudgetUtilizationRecordsEndpoint.Map(burGroup);
        ObligateBudgetUtilizationRecordEndpoint.Map(burGroup);
        CancelBudgetUtilizationRecordEndpoint.Map(burGroup);
    }
}
