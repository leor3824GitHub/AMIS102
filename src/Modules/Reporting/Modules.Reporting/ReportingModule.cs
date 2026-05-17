using Asp.Versioning;
using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Web.Modules;
using AMIS.Modules.Reporting.Features.v1.PurchaseRequests.PrintPurchaseRequestFast;
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

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var moduleGroup = endpoints
            .MapGroup("api/v{version:apiVersion}/reporting")
            .WithTags("Reporting")
            .WithApiVersionSet(apiVersionSet);

        var prGroup = moduleGroup.MapGroup("/procurement/purchase-requests");
        PrintPurchaseRequestFastEndpoint.Map(prGroup);
    }
}
