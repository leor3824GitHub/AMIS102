using Asp.Versioning;
using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Web.Modules;
using AMIS.Modules.ProcurementPlanning.Data;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.CreatePpmp;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.UpdatePpmp;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.SubmitPpmp;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.ApprovePpmp;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.RecallPpmp;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.ReturnPpmp;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.CreateUpdatePpmp;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.PromoteToFinalPpmp;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.GetPpmp;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.GetPpmpVersions;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.SearchPpmps;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.CreateAnnualProcurementPlan;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ConsolidatePpmps;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.PublishAnnualProcurementPlan;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ApproveAnnualProcurementPlan;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.RecallAnnualProcurementPlan;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ReturnAnnualProcurementPlan;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.CreateUpdateApp;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.PromoteToFinalApp;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.DeleteAnnualProcurementPlan;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAnnualProcurementPlan;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAppVersions;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAvailablePpmps;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.SearchAnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Provisioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AMIS.Modules.ProcurementPlanning;

public class ProcurementPlanningModule : IModule
{
    private static readonly IReadOnlyList<AmisPermission> RegisteredPermissions =
    [
        new("View PPMPs",    "View",    "ProcurementPlanning.Ppmps", IsBasic: true),
        new("Create PPMPs",  "Create",  "ProcurementPlanning.Ppmps"),
        new("Update PPMPs",  "Update",  "ProcurementPlanning.Ppmps"),
        new("Submit PPMPs",  "Submit",  "ProcurementPlanning.Ppmps"),
        new("Approve PPMPs", "Approve", "ProcurementPlanning.Ppmps"),
        new("Return PPMPs",  "Return",  "ProcurementPlanning.Ppmps"),
        new("Promote PPMPs to Final", "PromoteToFinal", "ProcurementPlanning.Ppmps"),
        new("Create Updated PPMPs",   "CreateUpdate",   "ProcurementPlanning.Ppmps"),

        new("View APPs",        "View",        "ProcurementPlanning.Apps", IsBasic: true),
        new("Create APPs",      "Create",      "ProcurementPlanning.Apps"),
        new("Delete APPs",      "Delete",      "ProcurementPlanning.Apps"),
        new("Consolidate APPs", "Consolidate", "ProcurementPlanning.Apps"),
        new("Publish APPs",     "Publish",     "ProcurementPlanning.Apps"),
        new("Approve APPs",     "Approve",     "ProcurementPlanning.Apps"),
        new("Return APPs",      "Return",      "ProcurementPlanning.Apps"),
        new("Promote APPs to Final", "PromoteToFinal", "ProcurementPlanning.Apps"),
        new("Create Updated APPs",   "CreateUpdate",   "ProcurementPlanning.Apps"),
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
        var appGroup = moduleGroup.MapGroup("/apps");

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


