using Asp.Versioning;
using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Web.Modules;
using AMIS.Modules.FastReporting.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace AMIS.Modules.FastReporting;

public sealed class FastReportingModule : IModule
{
    private static readonly IReadOnlyList<AmisPermission> RegisteredPermissions =
    [
        new("View FastReport Asset Reports",       "View", "FastReporting.Assets"),
        new("View FastReport Expendable Reports",  "View", "FastReporting.Expendable"),
        new("View FastReport Vehicle Reports",     "View", "FastReporting.Vehicle"),
        new("View FastReport Procurement Reports", "View", "FastReporting.Procurement"),
        new("View FastReport Finance Reports",     "View", "FastReporting.Finance"),
    ];

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(RegisteredPermissions);

        FastReport.Utils.Config.WebMode = true;
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var moduleGroup = endpoints
            .MapGroup("api/v{version:apiVersion}/fast-reporting")
            .WithTags("FastReporting")
            .WithApiVersionSet(apiVersionSet);

        // One line per area. Add the new area's extension method and the rest stays untouched.
        moduleGroup.MapProcurementFastReports();
        // moduleGroup.MapAssetManagementFastReports();
        // moduleGroup.MapExpendableFastReports();
        // moduleGroup.MapVehicleFastReports();
        // moduleGroup.MapFinanceFastReports();
        // moduleGroup.MapProcurementPlanningFastReports();
    }
}
