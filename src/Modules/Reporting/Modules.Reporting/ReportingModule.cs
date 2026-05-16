using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Web.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace AMIS.Modules.Reporting;

public sealed class ReportingModule : IModule
{
    private static readonly IReadOnlyList<AmisPermission> RegisteredPermissions =
    [
        new("View Asset Reports",       "View", "Reporting.Assets"),
        new("View Expendable Reports",  "View", "Reporting.Expendable"),
        new("View Vehicle Reports",     "View", "Reporting.Vehicle"),
        new("View Procurement Reports", "View", "Reporting.Procurement"),
        new("View Finance Reports",     "View", "Reporting.Finance"),
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

        // Report endpoints are mapped here as each phase is completed.
        // See REPORTING-MODULE-PLAN.md for the full implementation roadmap.
    }
}
