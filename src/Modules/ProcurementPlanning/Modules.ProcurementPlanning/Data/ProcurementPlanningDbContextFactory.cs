using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AMIS.Modules.ProcurementPlanning.Data;

public sealed class ProcurementPlanningDbContextFactory : IDesignTimeDbContextFactory<ProcurementPlanningDbContext>
{
    public ProcurementPlanningDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var provider = configuration["DatabaseOptions:Provider"] ?? DbProviders.PostgreSQL;
        var connectionString = configuration["DatabaseOptions:ConnectionString"]
            ?? "Host=localhost;Database=AMIS-playground;Username=postgres;Password=postgres";
        var migrationsAssembly = configuration["DatabaseOptions:MigrationsAssembly"]
            ?? "AMIS.Playground.Migrations.PostgreSQL";

        var optionsBuilder = new DbContextOptionsBuilder<ProcurementPlanningDbContext>();

        if (provider.Equals(DbProviders.PostgreSQL, StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseNpgsql(connectionString, b => b.MigrationsAssembly(migrationsAssembly));
        }
        else
        {
            throw new NotSupportedException($"Database provider '{provider}' is not supported for ProcurementPlanningDbContext migrations.");
        }

        var tenant = new AppTenantInfo("design-time", "design-time", "Design Time")
        {
            ConnectionString = connectionString,
            AdminEmail = "design-time@local",
            IsActive = true,
            ValidUpto = DateTime.UtcNow.AddYears(1)
        };

        var tenantAccessor = new DesignTimeTenantContextAccessor(new MultiTenantContext<AppTenantInfo>(tenant));
        var databaseOptions = Options.Create(new DatabaseOptions
        {
            Provider = provider,
            ConnectionString = connectionString,
            MigrationsAssembly = migrationsAssembly
        });

        return new ProcurementPlanningDbContext(
            tenantAccessor,
            optionsBuilder.Options,
            databaseOptions,
            new DesignTimeHostEnvironment());
    }

    private sealed class DesignTimeTenantContextAccessor : IMultiTenantContextAccessor<AppTenantInfo>
    {
        public DesignTimeTenantContextAccessor(IMultiTenantContext<AppTenantInfo> multiTenantContext)
        {
            MultiTenantContext = multiTenantContext;
        }

        public IMultiTenantContext MultiTenantContext { get; }
        IMultiTenantContext<AppTenantInfo> IMultiTenantContextAccessor<AppTenantInfo>.MultiTenantContext =>
            (IMultiTenantContext<AppTenantInfo>)MultiTenantContext;
    }

    private sealed class DesignTimeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = nameof(ProcurementPlanningDbContextFactory);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}

