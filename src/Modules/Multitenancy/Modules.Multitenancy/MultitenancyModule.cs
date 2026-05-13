using Asp.Versioning;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;
using Finbuckle.MultiTenant.Extensions;
using Finbuckle.MultiTenant.Stores;
using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Web.Modules;
using AMIS.Modules.Multitenancy.Contracts;
using AMIS.Modules.Multitenancy.Data;
using AMIS.Modules.Multitenancy.Features.v1.ChangeTenantActivation;
using AMIS.Modules.Multitenancy.Features.v1.CreateTenant;
using AMIS.Modules.Multitenancy.Features.v1.GetTenants;
using AMIS.Modules.Multitenancy.Features.v1.GetTenantStatus;
using AMIS.Modules.Multitenancy.Features.v1.GetTenantTheme;
using AMIS.Modules.Multitenancy.Features.v1.ResetTenantTheme;
using AMIS.Modules.Multitenancy.Features.v1.TenantProvisioning.GetTenantProvisioningStatus;
using AMIS.Modules.Multitenancy.Features.v1.TenantProvisioning.RetryTenantProvisioning;
using AMIS.Modules.Multitenancy.Features.v1.UpdateTenantTheme;
using AMIS.Modules.Multitenancy.Features.v1.UpgradeTenant;
using AMIS.Modules.Multitenancy.Provisioning;
using AMIS.Modules.Multitenancy.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Multitenancy;

public sealed class MultitenancyModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var multitenancyOptions = builder.Configuration
            .GetSection(nameof(MultitenancyOptions))
            .Get<MultitenancyOptions>() ?? new MultitenancyOptions();

        builder.Services.AddOptions<MultitenancyOptions>()
            .Bind(builder.Configuration.GetSection(nameof(MultitenancyOptions)));

        builder.Services.AddScoped<ITenantService, TenantService>();
        builder.Services.AddScoped<ITenantThemeService, TenantThemeService>();
        builder.Services.AddTransient<IConnectionStringValidator, ConnectionStringValidator>();
        builder.Services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        builder.Services.AddHostedService<TenantStoreInitializerHostedService>();
        builder.Services.AddTransient<TenantProvisioningJob>();
        builder.Services.AddHostedService<TenantAutoProvisioningHostedService>();

        builder.Services.AddHeroDbContext<TenantDbContext>();

        var multiTenantBuilder = builder.Services
            .AddMultiTenant<AppTenantInfo>(options =>
            {
                options.Events.OnTenantResolveCompleted = async context =>
                {
                    if (context.MultiTenantContext.StoreInfo is null) return;
                    if (context.MultiTenantContext.StoreInfo.StoreType != typeof(DistributedCacheStore<AppTenantInfo>))
                    {
                        var sp = ((HttpContext)context.Context!).RequestServices;
                        var distributedStore = sp
                            .GetRequiredService<IEnumerable<IMultiTenantStore<AppTenantInfo>>>()
                            .FirstOrDefault(s => s.GetType() == typeof(DistributedCacheStore<AppTenantInfo>));

                        var tenantInfo = context.MultiTenantContext.TenantInfo;
                        if (distributedStore is not null && tenantInfo is not null)
                        {
                            try
                            {
                                await distributedStore.AddAsync(tenantInfo);
                            }
                            catch (Exception ex)
                            {
                                // Cache warm-up is best effort; tenant resolution can still proceed from EF store.
                                var logger = sp.GetService<ILogger<MultitenancyModule>>();
                                logger?.LogWarning(ex, "Failed to cache tenant '{TenantIdentifier}' in distributed tenant store.", tenantInfo.Identifier);
                            }
                        }
                    }
                };
            })
            .WithClaimStrategy(ClaimConstants.Tenant)
            .WithHeaderStrategy(MultitenancyConstants.Identifier)
            .WithDelegateStrategy(async context =>
            {
                if (context is not HttpContext httpContext) return null;

                if (!httpContext.Request.Query.TryGetValue("tenant", out var tenantIdentifier) ||
                    string.IsNullOrEmpty(tenantIdentifier))
                    return null;

                return await Task.FromResult(tenantIdentifier.ToString());
            })
            .WithStore<EFCoreStore<TenantDbContext, AppTenantInfo>>(ServiceLifetime.Scoped);

        if (multitenancyOptions.UseDistributedCacheStore)
        {
            multiTenantBuilder.WithDistributedCacheStore(TimeSpan.FromMinutes(60));
        }

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<TenantDbContext>(
                name: "db:multitenancy",
                failureStatus: HealthStatus.Unhealthy)
            .AddCheck<TenantMigrationsHealthCheck>(
                name: "db:tenants-migrations",
                failureStatus: HealthStatus.Healthy);
        builder.Services.AddScoped<ITenantService, TenantService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints.MapGroup("api/v{version:apiVersion}/tenants")
            .WithTags("Tenants")
            .WithApiVersionSet(versionSet);
        ChangeTenantActivationEndpoint.Map(group);
        GetTenantsEndpoint.Map(group);
        UpgradeTenantEndpoint.Map(group);
        CreateTenantEndpoint.Map(group);
        GetTenantStatusEndpoint.Map(group);
        GetTenantProvisioningStatusEndpoint.Map(group);
        RetryTenantProvisioningEndpoint.Map(group);

        // Theme endpoints
        GetTenantThemeEndpoint.Map(group);
        UpdateTenantThemeEndpoint.Map(group);
        ResetTenantThemeEndpoint.Map(group);
    }
}

