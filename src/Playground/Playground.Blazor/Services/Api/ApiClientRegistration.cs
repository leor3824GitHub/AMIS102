using System.Net.Http;
using AMIS.Playground.Blazor.ApiClient;

namespace AMIS.Playground.Blazor;

internal static class ApiClientRegistration
{
    public static IServiceCollection AddApiClients(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var apiBaseUrl = configuration["Api:BaseUrl"]
            ?? throw new InvalidOperationException("Api:BaseUrl configuration is missing.");

        var apiUri = new Uri(apiBaseUrl);

        static HttpClientHandler CreateHandler(Uri apiUri, IWebHostEnvironment environment)
        {
            var handler = new HttpClientHandler();

            // Local development convenience: allow self-signed localhost certs.
            if (environment.IsDevelopment() &&
                (string.Equals(apiUri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(apiUri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)))
            {
#pragma warning disable S4830
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#pragma warning restore S4830
            }

            return handler;
        }

        static HttpClient ResolveClient(IServiceProvider sp) =>
            sp.GetRequiredService<HttpClient>();

        // Register a named HttpClient for token operations (no auth handler to avoid circular dependency)
        services.AddHttpClient("TokenClient", client =>
        {
            client.BaseAddress = apiUri;
        })
        .ConfigurePrimaryHttpMessageHandler(() => CreateHandler(apiUri, environment));

        // TokenClient uses the named HttpClient without the AuthorizationHeaderHandler
        // This avoids circular dependency: TokenRefreshService -> ITokenClient -> HttpClient -> AuthorizationHeaderHandler -> TokenRefreshService
        services.AddTransient<ITokenClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("TokenClient");
            return new TokenClient(client);
        });

        // Framework standard: Register NSwag-generated clients directly
        services.AddTransient<IIdentityClient>(sp =>
            new IdentityClient(ResolveClient(sp)));

        services.AddTransient<IAuditsClient>(sp =>
            new AuditsClient(ResolveClient(sp)));

        services.AddTransient<ITenantsClient>(sp =>
            new TenantsClient(ResolveClient(sp)));

        services.AddTransient<IProvisioningClient>(sp =>
            new ProvisioningClient(ResolveClient(sp)));

        services.AddTransient<IThemeClient>(sp =>
            new ThemeClient(ResolveClient(sp)));

        services.AddTransient<IUsersClient>(sp =>
            new UsersClient(ResolveClient(sp)));

        services.AddTransient<IGroupsClient>(sp =>
            new GroupsClient(ResolveClient(sp)));

        services.AddTransient<ISessionsClient>(sp =>
            new SessionsClient(ResolveClient(sp)));

        services.AddTransient<IV1Client>(sp =>
            new V1Client(ResolveClient(sp)));

        // Expendable module clients (NSwag-generated aggregated client)
        services.AddTransient<IExpendableClient>(sp =>
            new ExpendableClient(ResolveClient(sp)));

        // Expendable module clients (NSwag-generated per API path)
        services.AddTransient<IProductsClient>(sp =>
            new ProductsClient(ResolveClient(sp)));

        services.AddTransient<IPurchasesClient>(sp =>
            new PurchasesClient(ResolveClient(sp)));

        services.AddTransient<ISupply_requestsClient>(sp =>
            new Supply_requestsClient(ResolveClient(sp)));

        services.AddTransient<IEmployeeClient>(sp =>
            new EmployeeClient(ResolveClient(sp)));

        services.AddTransient<ICartClient>(sp =>
            new CartClient(ResolveClient(sp)));

        services.AddTransient<IWarehouseClient>(sp =>
            new WarehouseClient(ResolveClient(sp)));

        services.AddTransient<IInspectionsClient>(sp =>
            new InspectionsClient(ResolveClient(sp)));

        services.AddTransient<IInventoryClient>(sp =>
            new InventoryClient(ResolveClient(sp)));

        services.AddTransient<IRejectedClient>(sp =>
            new RejectedClient(ResolveClient(sp)));

        services.AddTransient<IReportsClient>(sp =>
            new ReportsClient(ResolveClient(sp)));

        // Master Data module clients (NSwag-generated)
        services.AddTransient<ILookupClient>(sp =>
            new LookupClient(ResolveClient(sp)));

        services.AddTransient<IEmployeesClient>(sp =>
            new EmployeesClient(ResolveClient(sp)));

        services.AddTransient<IMaster_dataClient>(sp =>
            new Master_dataClient(ResolveClient(sp)));

        // Vehicle module manual client (temporary until OpenAPI generation is fixed)
        services.AddTransient<IVehicleClient>(sp =>
            new VehicleClient(ResolveClient(sp)));

        services.AddTransient<IReportSignatoryClient>(sp =>
            new ReportSignatoryClient(ResolveClient(sp)));

        services.AddTransient<IOrganizationProfileClient>(sp =>
            new OrganizationProfileClient(ResolveClient(sp)));

        services.AddTransient<ICapitalizationThresholdClient>(sp =>
            new CapitalizationThresholdClient(ResolveClient(sp)));

        services.AddTransient<IPropertyClassClient>(sp =>
            new PropertyClassClient(ResolveClient(sp)));

        services.AddTransient<IModeOfProcurementClient>(sp =>
            new ModeOfProcurementClient(ResolveClient(sp)));

        // Finance module manual clients
        services.AddTransient<IDisbursementVoucherClient>(sp =>
            new DisbursementVoucherClient(ResolveClient(sp)));

        services.AddTransient<IBudgetUtilizationRecordClient>(sp =>
            new BudgetUtilizationRecordClient(ResolveClient(sp)));

        // Asset Management module manual clients
        services.AddTransient<IPropertyItemCatalogClient>(sp =>
            new PropertyItemCatalogClient(ResolveClient(sp)));

        services.AddTransient<ISemiExpendablePropertyClient>(sp =>
            new SemiExpendablePropertyClient(ResolveClient(sp)));

        services.AddTransient<ISMRRClient>(sp =>
            new SMRRClient(ResolveClient(sp)));

        services.AddTransient<ITangibleInventoryItemClient>(sp =>
            new TangibleInventoryItemClient(ResolveClient(sp)));

        services.AddTransient<IICSClient>(sp =>
            new ICSClient(ResolveClient(sp)));

        services.AddTransient<ISMIRClient>(sp =>
            new SMIRClient(ResolveClient(sp)));

        services.AddTransient<IRRSPClient>(sp =>
            new RRSPClient(ResolveClient(sp)));

        services.AddTransient<IPropertyIncidentReportClient>(sp =>
            new PropertyIncidentReportClient(ResolveClient(sp)));

        services.AddTransient<IUnserviceablePropertyReportClient>(sp =>
            new UnserviceablePropertyReportClient(ResolveClient(sp)));

        services.AddTransient<IAssetManagementReportsClient>(sp =>
            new AssetManagementReportsClient(ResolveClient(sp)));

        // PPE track clients
        services.AddTransient<IPPERRClient>(sp =>
            new PPERRClient(ResolveClient(sp)));

        services.AddTransient<IPARClient>(sp =>
            new PARClient(ResolveClient(sp)));

        services.AddTransient<IPPEIRClient>(sp =>
            new PPEIRClient(ResolveClient(sp)));

        services.AddTransient<IRRPClient>(sp =>
            new RRPClient(ResolveClient(sp)));

        services.AddTransient<IPPEItemClient>(sp =>
            new PPEItemClient(ResolveClient(sp)));

        services.AddTransient<IPhysicalCountClient>(sp =>
            new PhysicalCountClient(ResolveClient(sp)));

        services.AddTransient<ITangibleItemClient>(sp =>
            new TangibleItemClient(ResolveClient(sp)));

        // Asset Procurement module manual clients
        services.AddTransient<IAssetIarClient>(sp =>
            new AssetIarClient(ResolveClient(sp)));
        services.AddTransient<IAssetPurchaseOrderClient>(sp =>
            new AssetPurchaseOrderClient(ResolveClient(sp)));

        // Procurement module manual clients
        services.AddTransient<IPurchaseRequestClient>(sp =>
            new PurchaseRequestClient(ResolveClient(sp)));

        services.AddTransient<ICanvassRequestClient>(sp =>
            new CanvassRequestClient(ResolveClient(sp)));

        services.AddTransient<IPurchaseOrderClient>(sp =>
            new PurchaseOrderClient(ResolveClient(sp)));

        // Procurement Planning module clients
        services.AddTransient<IPpmpClient>(sp =>
            new PpmpClient(ResolveClient(sp)));

        services.AddTransient<IAppClient>(sp =>
            new AppClient(ResolveClient(sp)));

        // Root endpoint client generated by NSwag for "/"
        services.AddTransient<IClient>(sp =>
            new Client(ResolveClient(sp)));

        services.AddScoped<IHealthClient>(sp =>
            new HealthClient(ResolveClient(sp)));

        // Asset Register module clients
        services.AddTransient<IAssetRegistryClient>(sp =>
            new AssetRegistryClient(ResolveClient(sp)));
        services.AddTransient<IArCatalogClient>(sp =>
            new ArCatalogClient(ResolveClient(sp)));
        services.AddTransient<IArAccountabilityClient>(sp =>
            new ArAccountabilityClient(ResolveClient(sp)));
        services.AddTransient<IArPhysicalCountClient>(sp =>
            new ArPhysicalCountClient(ResolveClient(sp)));
        services.AddTransient<IArIncidentReportClient>(sp =>
            new ArIncidentReportClient(ResolveClient(sp)));
        services.AddTransient<IArIssuanceReportClient>(sp =>
            new ArIssuanceReportClient(ResolveClient(sp)));
        services.AddTransient<IArUnserviceableReportClient>(sp =>
            new ArUnserviceableReportClient(ResolveClient(sp)));
        services.AddTransient<IArReceivingReportClient>(sp =>
            new ArReceivingReportClient(ResolveClient(sp)));

        return services;
    }
}

