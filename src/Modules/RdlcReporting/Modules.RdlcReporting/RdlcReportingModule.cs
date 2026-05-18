using Asp.Versioning;
using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Web.Modules;
using AMIS.Modules.RdlcReporting.Features.v1.PurchaseRequests.PrintPurchaseRequest;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace AMIS.Modules.RdlcReporting;

public sealed class RdlcReportingModule : IModule
{
    private static readonly IReadOnlyList<AmisPermission> RegisteredPermissions =
    [
        new("View RDLC Asset Reports",       "View", "RdlcReporting.Assets"),
        new("View RDLC Expendable Reports",  "View", "RdlcReporting.Expendable"),
        new("View RDLC Vehicle Reports",     "View", "RdlcReporting.Vehicle"),
        new("View RDLC Procurement Reports", "View", "RdlcReporting.Procurement"),
        new("View RDLC Finance Reports",     "View", "RdlcReporting.Finance"),
    ];

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(RegisteredPermissions);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var moduleGroup = endpoints
            .MapGroup("api/v{version:apiVersion}/rdlc-reporting")
            .WithTags("RdlcReporting")
            .WithApiVersionSet(apiVersionSet);

        var prGroup = moduleGroup.MapGroup("/procurement/purchase-requests");
        PrintPurchaseRequestEndpoint.Map(prGroup);
    }
}
