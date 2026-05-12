using FSH.Framework.Web;
using FSH.Framework.Web.Modules;
using FSH.Modules.Auditing;
using FSH.Modules.MasterData;
using FSH.Modules.MasterData.Contracts.v1.References;
using FSH.Modules.Expendable;
using FSH.Modules.Expendable.Contracts.v1.Products;
using FSH.Modules.Vehicle;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using FSH.Modules.ProcurementAcquisition;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using FSH.Modules.Finance;
using FSH.Modules.Finance.Contracts.v1.DisbursementVouchers;
using FSH.Modules.AssetManagement;
using FSH.Modules.AssetProcurement;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseRequests;
using FSH.Modules.AssetRegister;
using FSH.Modules.AssetRegister.Contracts.v1.Assets;
using FSH.Modules.ProcurementPlanning;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using FSH.Modules.Identity;
using FSH.Modules.Identity.Contracts.v1.Tokens.TokenGeneration;
using FSH.Modules.Identity.Features.v1.Tokens.TokenGeneration;
using FSH.Modules.Multitenancy;
using FSH.Modules.Multitenancy.Contracts.v1.GetTenantStatus;
using FSH.Modules.Multitenancy.Features.v1.GetTenantStatus;
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
        typeof(FSH.Modules.Auditing.Contracts.AuditEnvelope),
        typeof(FSH.Modules.Auditing.Persistence.AuditDbContext),
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
        typeof(AssetProcurementModule),
        typeof(CreateAssetPurchaseRequestCommand),
        typeof(AssetRegisterModule),
        typeof(RegisterAssetCommand),
        typeof(ProcurementPlanningModule),
        typeof(CreatePpmpCommand)];
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
    typeof(AssetProcurementModule).Assembly,
    typeof(AssetRegisterModule).Assembly,
    typeof(ProcurementPlanningModule).Assembly
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
   .WithTags("PlayGround")
   .AllowAnonymous();
await app.RunAsync();
