using Asp.Versioning;
using AMIS.Framework.Persistence;
using AMIS.Framework.Web.Modules;
using AMIS.Modules.Auditing.Contracts;
using AMIS.Modules.Auditing.Features.v1.GetAuditById;
using AMIS.Modules.Auditing.Features.v1.GetAudits;
using AMIS.Modules.Auditing.Features.v1.GetAuditsByCorrelation;
using AMIS.Modules.Auditing.Features.v1.GetAuditsByTrace;
using AMIS.Modules.Auditing.Features.v1.GetAuditSummary;
using AMIS.Modules.Auditing.Features.v1.GetExceptionAudits;
using AMIS.Modules.Auditing.Features.v1.GetSecurityAudits;
using AMIS.Modules.Auditing.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace AMIS.Modules.Auditing;

public class AuditingModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var httpOpts = builder.Configuration.GetSection("Auditing").Get<AuditHttpOptions>() ?? new AuditHttpOptions();
        builder.Services.AddSingleton(httpOpts);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IAuditClient, DefaultAuditClient>();
        builder.Services.AddScoped<ISecurityAudit, SecurityAudit>();
        builder.Services.AddHeroDbContext<AuditDbContext>();
        builder.Services.AddScoped<IDbInitializer, AuditDbInitializer>();
        builder.Services.AddSingleton<IAuditSerializer, SystemTextJsonAuditSerializer>();
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<AuditDbContext>(
                name: "db:auditing",
                failureStatus: HealthStatus.Unhealthy);

        // Enrichers used by Audit.Configure (scoped, run on request thread)
        builder.Services.AddScoped<IAuditMaskingService, JsonMaskingService>();
        builder.Services.AddHostedService<AuditingConfigurator>();
        builder.Services.AddScoped<IAuditScope, HttpAuditScope>();

        builder.Services.AddSingleton<ChannelAuditPublisher>();
        builder.Services.AddSingleton<IAuditPublisher>(sp => sp.GetRequiredService<ChannelAuditPublisher>());

        builder.Services.AddSingleton<IAuditSink, SqlAuditSink>();
        builder.Services.AddHostedService<AuditBackgroundWorker>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/audits")
            .WithTags("Audits")
            .WithApiVersionSet(apiVersionSet);

        group.MapGetAuditsEndpoint();
        group.MapGetAuditByIdEndpoint();
        group.MapGetAuditsByCorrelationEndpoint();
        group.MapGetAuditsByTraceEndpoint();
        group.MapGetSecurityAuditsEndpoint();
        group.MapGetExceptionAuditsEndpoint();
        group.MapGetAuditSummaryEndpoint();
    }
}

