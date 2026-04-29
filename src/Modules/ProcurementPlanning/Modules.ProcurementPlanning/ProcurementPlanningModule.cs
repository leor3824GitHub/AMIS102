using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.ProcurementPlanning.Data;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.CreatePpmp;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.UpdatePpmp;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.SubmitPpmp;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.ApprovePpmp;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.RecallPpmp;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.ReturnPpmp;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.AmendPpmp;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.PromoteToFinalPpmp;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.GetPpmp;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.GetPpmpVersions;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.SearchPpmps;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.CreateAnnualProcurementPlan;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ConsolidatePpmps;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.PublishAnnualProcurementPlan;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ApproveAnnualProcurementPlan;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.RecallAnnualProcurementPlan;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ReturnAnnualProcurementPlan;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.AmendAnnualProcurementPlan;
<<<<<<< HEAD
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.PromoteToFinalApp;
=======
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.DeleteAnnualProcurementPlan;
>>>>>>> d63aec54a5aea0527fd07e545543a98aceae4138
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAnnualProcurementPlan;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAppVersions;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAvailablePpmps;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.SearchAnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Provisioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FSH.Modules.ProcurementPlanning;

public class ProcurementPlanningModule : IModule
{
    private static readonly IReadOnlyList<FshPermission> RegisteredPermissions =
    [
        new("View PPMPs",    "View",    "ProcurementPlanning.Ppmps", IsBasic: true),
        new("Create PPMPs",  "Create",  "ProcurementPlanning.Ppmps"),
        new("Update PPMPs",  "Update",  "ProcurementPlanning.Ppmps"),
        new("Submit PPMPs",  "Submit",  "ProcurementPlanning.Ppmps"),
        new("Approve PPMPs", "Approve", "ProcurementPlanning.Ppmps"),
        new("Return PPMPs",  "Return",  "ProcurementPlanning.Ppmps"),
        new("Amend PPMPs",   "Amend",   "ProcurementPlanning.Ppmps"),

        new("View APPs",        "View",        "ProcurementPlanning.Apps", IsBasic: true),
        new("Create APPs",      "Create",      "ProcurementPlanning.Apps"),
        new("Consolidate APPs", "Consolidate", "ProcurementPlanning.Apps"),
        new("Publish APPs",     "Publish",     "ProcurementPlanning.Apps"),
        new("Approve APPs",     "Approve",     "ProcurementPlanning.Apps"),
        new("Return APPs",      "Return",      "ProcurementPlanning.Apps"),
        new("Amend APPs",       "Amend",       "ProcurementPlanning.Apps"),
    ];

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;

        PermissionConstants.Register(RegisteredPermissions);

        services.AddHeroDbContext<ProcurementPlanningDbContext>();
        services.AddScoped<IDbInitializer, ProcurementPlanningDbInitializer>();
        services.AddHostedService<ProcurementPlanningDbInitializerHostedService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var moduleGroup = endpoints
            .MapGroup("api/v{version:apiVersion}/procurement-planning")
            .WithTags("Procurement Planning")
            .WithApiVersionSet(apiVersionSet);

        var ppmpGroup = moduleGroup.MapGroup("/ppmps");
        var appGroup  = moduleGroup.MapGroup("/apps");

        // PPMPs
        CreatePpmpEndpoint.Map(ppmpGroup);
        UpdatePpmpEndpoint.Map(ppmpGroup);
        SubmitPpmpEndpoint.Map(ppmpGroup);
        ApprovePpmpEndpoint.Map(ppmpGroup);
        RecallPpmpEndpoint.Map(ppmpGroup);
        ReturnPpmpEndpoint.Map(ppmpGroup);
        CreateUpdatePpmpEndpoint.Map(ppmpGroup);
        PromoteToFinalPpmpEndpoint.Map(ppmpGroup);
        GetPpmpEndpoint.Map(ppmpGroup);
        GetPpmpVersionsEndpoint.Map(ppmpGroup);
        SearchPpmpsEndpoint.Map(ppmpGroup);

        // APPs
        CreateAnnualProcurementPlanEndpoint.Map(appGroup);
        ConsolidatePpmpsEndpoint.Map(appGroup);
        PublishAnnualProcurementPlanEndpoint.Map(appGroup);
        ApproveAnnualProcurementPlanEndpoint.Map(appGroup);
        RecallAnnualProcurementPlanEndpoint.Map(appGroup);
        ReturnAnnualProcurementPlanEndpoint.Map(appGroup);
        CreateUpdateAppEndpoint.Map(appGroup);
        PromoteToFinalAppEndpoint.Map(appGroup);
        GetAnnualProcurementPlanEndpoint.Map(appGroup);
        DeleteAnnualProcurementPlanEndpoint.Map(appGroup);
        GetAppVersionsEndpoint.Map(appGroup);
        GetAvailablePpmpsEndpoint.Map(appGroup);
        SearchAnnualProcurementPlansEndpoint.Map(appGroup);
    }
}
