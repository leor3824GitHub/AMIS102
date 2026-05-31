using Asp.Versioning;
using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Web.Modules;
using AMIS.Modules.QuestPdfReporting.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting;

public sealed class QuestPdfReportingModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(QuestPdfReportingModuleConstants.Permissions);

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var moduleGroup = endpoints
            .MapGroup("api/v{version:apiVersion}/quest-pdf-reporting")
            .WithTags("QuestPdfReporting")
            .WithApiVersionSet(apiVersionSet);

        moduleGroup.MapExpendableQuestPdfReports();
        moduleGroup.MapVehicleQuestPdfReports();
        moduleGroup.MapAssetManagementQuestPdfReports();
    }
}
