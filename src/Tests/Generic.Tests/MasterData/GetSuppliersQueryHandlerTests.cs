using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.MasterData.Data;
using AMIS.Modules.MasterData.Domain;
using AMIS.Modules.MasterData.Features.v1.Suppliers.GetSuppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Generic.Tests.MasterData;

public sealed class GetSuppliersQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithVatKeyword_ReturnsOnlyVatSuppliers()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        dbContext.Suppliers.AddRange(
            Supplier.Create("SUP-VAT", "VAT Supplier", null, "VAT", null, null, null, null, null),
            Supplier.Create("SUP-EXEMPT", "Tax Exempt Supplier", null, "NON-VAT", null, null, null, null, null));
        await dbContext.SaveChangesAsync();

        var handler = new GetSuppliersQueryHandler(dbContext);

        // Act
        var result = await handler.Handle(new GetSuppliersQuery("VAT", 1, 10), CancellationToken.None);

        // Assert
        result.Items.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.Items.Single().BusinessTaxType.ShouldBe("VAT");
    }

    [Fact]
    public async Task Handle_WithTinKeyword_FiltersByTinNo()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        dbContext.Suppliers.AddRange(
            Supplier.Create("SUP-001", "Alpha", "999-111", "VAT", null, null, null, null, null),
            Supplier.Create("SUP-002", "Beta", "888-222", "NON-VAT", null, null, null, null, null));
        await dbContext.SaveChangesAsync();

        var handler = new GetSuppliersQueryHandler(dbContext);

        // Act
        var result = await handler.Handle(new GetSuppliersQuery("999-111", 1, 10), CancellationToken.None);

        // Assert
        result.Items.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.Items.Single().Code.ShouldBe("SUP-001");
    }

    private static MasterDataDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MasterDataDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var tenant = new AppTenantInfo("test-tenant", "test-tenant", "Test Tenant")
        {
            IsActive = true,
            ValidUpto = DateTime.UtcNow.AddYears(1)
        };

        var tenantAccessor = new TestTenantContextAccessor(new MultiTenantContext<AppTenantInfo>(tenant));

        var databaseOptions = Options.Create(new DatabaseOptions
        {
            Provider = DbProviders.PostgreSQL,
            MigrationsAssembly = "Generic.Tests"
        });

        return new MasterDataDbContext(tenantAccessor, options, databaseOptions, new TestHostEnvironment());
    }

    private sealed class TestTenantContextAccessor(IMultiTenantContext<AppTenantInfo> multiTenantContext)
        : IMultiTenantContextAccessor<AppTenantInfo>
    {
        public IMultiTenantContext MultiTenantContext { get; } = multiTenantContext;

        IMultiTenantContext<AppTenantInfo> IMultiTenantContextAccessor<AppTenantInfo>.MultiTenantContext =>
            (IMultiTenantContext<AppTenantInfo>)MultiTenantContext;
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = nameof(GetSuppliersQueryHandlerTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}

