using AMIS.Framework.Web;
using AMIS.Framework.Web.Modules;
using AMIS.Modules.Auditing;
using AMIS.Modules.MasterData;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.Expendable;
using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Modules.Vehicle;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using AMIS.Modules.ProcurementAcquisition;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.Finance;
using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.AssetManagement;
using AMIS.Modules.AssetRegister;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.ProcurementPlanning;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.FastReporting;
using AMIS.Modules.RdlcReporting;
using AMIS.Modules.Identity;
using AMIS.Modules.Identity.Contracts.v1.Tokens.TokenGeneration;
using AMIS.Modules.Identity.Features.v1.Tokens.TokenGeneration;
using AMIS.Modules.Multitenancy;
using AMIS.Modules.Multitenancy.Contracts.v1.GetTenantStatus;
using AMIS.Modules.Multitenancy.Features.v1.GetTenantStatus;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

if (builder.Environment.IsProduction())
{
    static void Require(IConfiguration config, string key)
    {
        if (string.IsNullOrWhiteSpace(config[key]))
        {
            throw new InvalidOperationException($"Missing required configuration '{key}' in Production.");
        }
    }

    var config = builder.Configuration;
    Require(config, "DatabaseOptions:ConnectionString");
    Require(config, "CachingOptions:Redis");
    Require(config, "JwtOptions:SigningKey");
}

builder.Services.AddMediator(o =>
{
    o.ServiceLifetime = ServiceLifetime.Scoped;
    o.Assemblies = [
        typeof(GenerateTokenCommand),
        typeof(GenerateTokenCommandHandler),
        typeof(GetTenantStatusQuery),
        typeof(GetTenantStatusQueryHandler),
        typeof(AMIS.Modules.Auditing.Contracts.AuditEnvelope),
        typeof(AMIS.Modules.Auditing.Persistence.AuditDbContext),
        typeof(CreateProductCommand),
        typeof(SearchEmployeeReferencesQuery),
        typeof(MasterDataModule),
        typeof(ExpendableModule),
        typeof(VehicleModule),
        typeof(CreateVehicleCommand),
        typeof(ProcurementAcquisitionModule),
        typeof(CreatePurchaseRequestCommand),
        typeof(FinanceModule),
        typeof(CreateDisbursementVoucherCommand),
        typeof(AssetManagementModule),
        typeof(AssetRegisterModule),
        typeof(RegisterAssetCommand),
        typeof(ProcurementPlanningModule),
        typeof(CreatePpmpCommand),
        typeof(FastReportingModule),
        typeof(RdlcReportingModule)];
});

var moduleAssemblies = new Assembly[]
{
    typeof(IdentityModule).Assembly,
    typeof(MultitenancyModule).Assembly,
    typeof(AuditingModule).Assembly,
    typeof(MasterDataModule).Assembly,
    typeof(ExpendableModule).Assembly,
    typeof(VehicleModule).Assembly,
    typeof(ProcurementAcquisitionModule).Assembly,
    typeof(FinanceModule).Assembly,
    typeof(AssetManagementModule).Assembly,
    typeof(AssetRegisterModule).Assembly,
    typeof(ProcurementPlanningModule).Assembly,
    typeof(FastReportingModule).Assembly,
    typeof(RdlcReportingModule).Assembly
};

builder.AddHeroPlatform(o =>
{
    o.EnableCaching = true;
    o.EnableMailing = true;
    o.EnableJobs = true;
});

builder.AddModules(moduleAssemblies);
var app = builder.Build();

app.UseHeroMultiTenantDatabases();
app.UseHeroPlatform(p =>
{
    p.MapModules = true;
    p.ServeStaticFiles = true;
});

app.MapGet("/", () => Results.Ok(new { message = "hello world!" }))
   .WithTags("Playground")
   .AllowAnonymous();
await app.RunAsync();

