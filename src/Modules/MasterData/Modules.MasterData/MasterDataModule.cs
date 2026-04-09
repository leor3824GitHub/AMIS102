using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.MasterData.Data;
using FSH.Modules.MasterData.Features.v1.Departments.CreateDepartment;
using FSH.Modules.MasterData.Features.v1.Departments.DeleteDepartment;
using FSH.Modules.MasterData.Features.v1.Departments.UpdateDepartment;
using FSH.Modules.MasterData.Features.v1.Employees.CreateEmployee;
using FSH.Modules.MasterData.Features.v1.Employees.DeleteEmployee;
using FSH.Modules.MasterData.Features.v1.Employees.UpdateEmployee;
using FSH.Modules.MasterData.Features.v1.Lookups;
using FSH.Modules.MasterData.Features.v1.Offices.CreateOffice;
using FSH.Modules.MasterData.Features.v1.Offices.DeleteOffice;
using FSH.Modules.MasterData.Features.v1.Offices.GetOfficeById;
using FSH.Modules.MasterData.Features.v1.Offices.UpdateOffice;
using FSH.Modules.MasterData.Features.v1.Positions.CreatePosition;
using FSH.Modules.MasterData.Features.v1.Positions.DeletePosition;
using FSH.Modules.MasterData.Features.v1.Positions.GetPositionById;
using FSH.Modules.MasterData.Features.v1.Positions.UpdatePosition;
using FSH.Modules.MasterData.Features.v1.UnitOfMeasures.CreateUnitOfMeasure;
using FSH.Modules.MasterData.Features.v1.UnitOfMeasures.DeleteUnitOfMeasure;
using FSH.Modules.MasterData.Features.v1.UnitOfMeasures.GetUnitOfMeasureById;
using FSH.Modules.MasterData.Features.v1.UnitOfMeasures.UpdateUnitOfMeasure;
using FSH.Modules.MasterData.Features.v1.Departments.GetDepartmentById;
using FSH.Modules.MasterData.Features.v1.Suppliers.CreateSupplier;
using FSH.Modules.MasterData.Features.v1.Suppliers.DeleteSupplier;
using FSH.Modules.MasterData.Features.v1.Suppliers.GetSuppliers;
using FSH.Modules.MasterData.Features.v1.Suppliers.GetSupplierById;
using FSH.Modules.MasterData.Features.v1.Suppliers.UpdateSupplier;
using FSH.Modules.MasterData.Features.v1.Categories.CreateCategory;
using FSH.Modules.MasterData.Features.v1.Categories.DeleteCategory;
using FSH.Modules.MasterData.Features.v1.Categories.GetCategories;
using FSH.Modules.MasterData.Features.v1.Categories.GetCategoryById;
using FSH.Modules.MasterData.Features.v1.Categories.UpdateCategory;
using FSH.Modules.MasterData.Features.v1.ReportSignatories.GetReportSignatories;
using FSH.Modules.MasterData.Features.v1.ReportSignatories.CreateReportSignatory;
using FSH.Modules.MasterData.Features.v1.ReportSignatories.UpdateReportSignatory;
using FSH.Modules.MasterData.Features.v1.ReportSignatories.DeleteReportSignatory;
using FSH.Modules.MasterData.Features.v1.OrganizationProfile.GetOrganizationProfile;
using FSH.Modules.MasterData.Features.v1.OrganizationProfile.UpsertOrganizationProfile;
using FSH.Modules.MasterData.Features.v1.CapitalizationThresholds.CreateCapitalizationThreshold;
using FSH.Modules.MasterData.Features.v1.CapitalizationThresholds.GetActiveThreshold;
using FSH.Modules.MasterData.Features.v1.CapitalizationThresholds.GetCapitalizationThresholds;
using FSH.Modules.MasterData.Features.v1.CapitalizationThresholds.SetActiveThreshold;
using FSH.Modules.MasterData.Features.v1.CapitalizationThresholds.UpdateCapitalizationThreshold;
using FSH.Framework.Eventing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FSH.Modules.MasterData;

public class MasterDataModule : IModule
{
    private static readonly IReadOnlyList<FshPermission> RegisteredPermissions =
    [
        new("View Employee Lookup", "View", "MasterData.Lookup", IsBasic: true),

        new("View Employees", "View", "MasterData.Employees"),
        new("Create Employees", "Create", "MasterData.Employees"),
        new("Update Employees", "Update", "MasterData.Employees"),
        new("Delete Employees", "Delete", "MasterData.Employees"),

        new("View Offices", "View", "MasterData.Offices"),
        new("Create Offices", "Create", "MasterData.Offices"),
        new("Update Offices", "Update", "MasterData.Offices"),
        new("Delete Offices", "Delete", "MasterData.Offices"),

        new("View Departments", "View", "MasterData.Departments"),
        new("Create Departments", "Create", "MasterData.Departments"),
        new("Update Departments", "Update", "MasterData.Departments"),
        new("Delete Departments", "Delete", "MasterData.Departments"),

        new("View Positions", "View", "MasterData.Positions"),
        new("Create Positions", "Create", "MasterData.Positions"),
        new("Update Positions", "Update", "MasterData.Positions"),
        new("Delete Positions", "Delete", "MasterData.Positions"),

        new("View Unit Of Measures", "View", "MasterData.UnitOfMeasures"),
        new("Create Unit Of Measures", "Create", "MasterData.UnitOfMeasures"),
        new("Update Unit Of Measures", "Update", "MasterData.UnitOfMeasures"),
        new("Delete Unit Of Measures", "Delete", "MasterData.UnitOfMeasures"),

        new("View Suppliers", "View", "MasterData.Suppliers"),
        new("Create Suppliers", "Create", "MasterData.Suppliers"),
        new("Update Suppliers", "Update", "MasterData.Suppliers"),
        new("Delete Suppliers", "Delete", "MasterData.Suppliers"),

        new("View Categories", "View", "MasterData.Categories"),
        new("Create Categories", "Create", "MasterData.Categories"),
        new("Update Categories", "Update", "MasterData.Categories"),
        new("Delete Categories", "Delete", "MasterData.Categories"),

        new("View Report Signatories", "View", "MasterData.ReportSignatories", IsBasic: true),
        new("Create Report Signatories", "Create", "MasterData.ReportSignatories"),
        new("Update Report Signatories", "Update", "MasterData.ReportSignatories"),
        new("Delete Report Signatories", "Delete", "MasterData.ReportSignatories"),

        new("View Organization Profile", "View", "MasterData.OrganizationProfile", IsBasic: true),
        new("Manage Organization Profile", "Manage", "MasterData.OrganizationProfile"),

        new("View Capitalization Thresholds", "View", "MasterData.CapitalizationThresholds", IsBasic: true),
        new("Manage Capitalization Thresholds", "Manage", "MasterData.CapitalizationThresholds")
    ];

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;

        PermissionConstants.Register(RegisteredPermissions);

        services.AddHeroDbContext<MasterDataDbContext>();
        services.AddScoped<IDbInitializer, MasterDataDbInitializer>();
        services.AddHostedService<FSH.Modules.MasterData.Provisioning.MasterDataDbInitializerHostedService>();
        services.AddIntegrationEventHandlers(typeof(MasterDataModule).Assembly);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var moduleGroup = endpoints
            .MapGroup("api/v{version:apiVersion}/master-data")
            .WithTags("MasterData")
            .WithApiVersionSet(apiVersionSet);

        var lookupGroup = moduleGroup.MapGroup("/lookup");
        var employeesGroup = moduleGroup.MapGroup("/employees");
        var officesGroup = moduleGroup.MapGroup("/offices");
        var departmentsGroup = moduleGroup.MapGroup("/departments");
        var positionsGroup = moduleGroup.MapGroup("/positions");
        var unitOfMeasuresGroup = moduleGroup.MapGroup("/unit-of-measures");
        var suppliersGroup = moduleGroup.MapGroup("/suppliers");
        var categoriesGroup = moduleGroup.MapGroup("/categories");
        var reportSignatoriesGroup = moduleGroup.MapGroup("/report-signatories");
        var organizationProfileGroup = moduleGroup.MapGroup("/organization-profile");
        var capitalizationThresholdsGroup = moduleGroup.MapGroup("/capitalization-thresholds");

        MasterDataLookupEndpoint.Map(lookupGroup);
        CreateEmployeeEndpoint.Map(employeesGroup);
        UpdateEmployeeEndpoint.Map(employeesGroup);
        DeleteEmployeeEndpoint.Map(employeesGroup);
        CreateOfficeEndpoint.Map(officesGroup);
        GetOfficeByIdEndpoint.Map(officesGroup);
        UpdateOfficeEndpoint.Map(officesGroup);
        DeleteOfficeEndpoint.Map(officesGroup);
        CreateDepartmentEndpoint.Map(departmentsGroup);
        GetDepartmentByIdEndpoint.Map(departmentsGroup);
        UpdateDepartmentEndpoint.Map(departmentsGroup);
        DeleteDepartmentEndpoint.Map(departmentsGroup);
        CreatePositionEndpoint.Map(positionsGroup);
        GetPositionByIdEndpoint.Map(positionsGroup);
        UpdatePositionEndpoint.Map(positionsGroup);
        DeletePositionEndpoint.Map(positionsGroup);
        CreateUnitOfMeasureEndpoint.Map(unitOfMeasuresGroup);
        GetUnitOfMeasureByIdEndpoint.Map(unitOfMeasuresGroup);
        UpdateUnitOfMeasureEndpoint.Map(unitOfMeasuresGroup);
        DeleteUnitOfMeasureEndpoint.Map(unitOfMeasuresGroup);
        CreateSupplierEndpoint.Map(suppliersGroup);
        GetSuppliersEndpoint.Map(suppliersGroup);
        GetSupplierByIdEndpoint.Map(suppliersGroup);
        UpdateSupplierEndpoint.Map(suppliersGroup);
        DeleteSupplierEndpoint.Map(suppliersGroup);
        CreateCategoryEndpoint.Map(categoriesGroup);
        GetCategoriesEndpoint.Map(categoriesGroup);
        GetCategoryByIdEndpoint.Map(categoriesGroup);
        UpdateCategoryEndpoint.Map(categoriesGroup);
        DeleteCategoryEndpoint.Map(categoriesGroup);
        GetReportSignatoriesEndpoint.Map(reportSignatoriesGroup);
        CreateReportSignatoryEndpoint.Map(reportSignatoriesGroup);
        UpdateReportSignatoryEndpoint.Map(reportSignatoriesGroup);
        DeleteReportSignatoryEndpoint.Map(reportSignatoriesGroup);
        GetOrganizationProfileEndpoint.Map(organizationProfileGroup);
        UpsertOrganizationProfileEndpoint.Map(organizationProfileGroup);
        GetCapitalizationThresholdsEndpoint.Map(capitalizationThresholdsGroup);
        GetActiveThresholdEndpoint.Map(capitalizationThresholdsGroup);
        CreateCapitalizationThresholdEndpoint.Map(capitalizationThresholdsGroup);
        UpdateCapitalizationThresholdEndpoint.Map(capitalizationThresholdsGroup);
        SetActiveThresholdEndpoint.Map(capitalizationThresholdsGroup);
    }
}



