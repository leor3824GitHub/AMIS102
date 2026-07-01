using System.Net.Http;
using AMIS.Blazor.ApiClient;

namespace AMIS.Blazor;

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
            // Identify the client UI platform on login/refresh. The AuthorizationHeaderHandler
            // (which normally adds this) is intentionally not on this named client, so set it here.
            client.DefaultRequestHeaders.Add("X-Client-Id", "blazor");
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

        // Global platform settings (manual client; pending NSwag regeneration)
        services.AddTransient<IPlatformSettingsClient>(sp =>
            new PlatformSettingsClient(ResolveClient(sp)));

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

        // Product ratings (manual client; pending NSwag regeneration)
        services.AddTransient<IProductRatingsClient>(sp =>
            new ProductRatingsClient(ResolveClient(sp)));

        services.AddTransient<ISupply_requestsClient>(sp =>
            new Supply_requestsClient(ResolveClient(sp)));

        services.AddTransient<IEmployeeClient>(sp =>
            new EmployeeClient(ResolveClient(sp)));

        services.AddTransient<ICartClient>(sp =>
            new CartClient(ResolveClient(sp)));

        services.AddTransient<IWarehouseClient>(sp =>
            new WarehouseClient(ResolveClient(sp)));

        services.AddTransient<IInventoryClient>(sp =>
            new InventoryClient(ResolveClient(sp)));

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

        // Budget Disbursement module manual clients
        services.AddTransient<IDisbursementVoucherClient>(sp =>
            new DisbursementVoucherClient(ResolveClient(sp)));

        services.AddTransient<IBudgetUtilizationRequestClient>(sp =>
            new BudgetUtilizationRequestClient(ResolveClient(sp)));

        services.AddTransient<IBudgetDisbursementSignedDocumentClient>(sp =>
            new BudgetDisbursementSignedDocumentClient(ResolveClient(sp)));

        services.AddTransient<IBudgetDisbursementSettingsClient>(sp =>
            new BudgetDisbursementSettingsClient(ResolveClient(sp)));

        // Asset IAR client (merged into Procurement module)
        services.AddTransient<IInspectionAcceptanceReportClient>(sp =>
            new InspectionAcceptanceReportClient(ResolveClient(sp)));

        // Procurement module manual clients
        services.AddTransient<IPurchaseRequestClient>(sp =>
            new PurchaseRequestClient(ResolveClient(sp)));

        services.AddTransient<ICanvassRequestClient>(sp =>
            new CanvassRequestClient(ResolveClient(sp)));

        services.AddTransient<IPurchaseOrderClient>(sp =>
            new PurchaseOrderClient(ResolveClient(sp)));

        services.AddTransient<IJobOrderClient>(sp =>
            new JobOrderClient(ResolveClient(sp)));

        services.AddTransient<ISignedDocumentClient>(sp =>
            new SignedDocumentClient(ResolveClient(sp)));

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
        services.AddTransient<IArDepreciationClient>(sp =>
            new ArDepreciationClient(ResolveClient(sp)));
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
        services.AddTransient<IArRepairClient>(sp =>
            new ArRepairClient(ResolveClient(sp)));
        services.AddTransient<IArReceivingReportClient>(sp =>
            new ArReceivingReportClient(ResolveClient(sp)));
        services.AddTransient<IArReturnedPropertyClient>(sp =>
            new ArReturnedPropertyClient(ResolveClient(sp)));
        services.AddTransient<IArSignedDocumentClient>(sp =>
            new ArSignedDocumentClient(ResolveClient(sp)));
        services.AddTransient<ILocationLookupClient>(sp =>
            new LocationLookupClient(ResolveClient(sp)));
        services.AddTransient<ILocationClient>(sp =>
            new LocationClient(ResolveClient(sp)));

        // Chat module manual client
        services.AddTransient<IChatClient>(sp =>
            new ChatClient(ResolveClient(sp)));

        // Notifications module manual client
        services.AddTransient<INotificationsClient>(sp =>
            new NotificationsClient(ResolveClient(sp)));

        // Unified inspection worklist (aggregates each module's "pending-for-me" endpoint)
        services.AddTransient<IInspectionWorklistClient>(sp =>
            new InspectionWorklistClient(ResolveClient(sp)));

        return services;
    }
}

