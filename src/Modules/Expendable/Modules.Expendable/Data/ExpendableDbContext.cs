using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Eventing.Inbox;
using AMIS.Framework.Eventing.Outbox;
using AMIS.Framework.Persistence.Context;
using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Expendable.Domain.Cart;
using AMIS.Modules.Expendable.Domain.Inventory;
using AMIS.Modules.Expendable.Domain.Products;
using AMIS.Modules.Expendable.Domain.Requests;
using AMIS.Modules.Expendable.Domain.Warehouse;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AMIS.Modules.Expendable.Data;

public class ExpendableDbContext : BaseDbContext
{

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    // Product Management
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductRating> ProductRatings => Set<ProductRating>();

    // Supply Requests
    public DbSet<SupplyRequest> SupplyRequests => Set<SupplyRequest>();

    // Shopping Cart
    public DbSet<EmployeeShoppingCart> ShoppingCarts => Set<EmployeeShoppingCart>();

    // Inventory
    public DbSet<EmployeeInventory> EmployeeInventories => Set<EmployeeInventory>();
    public DbSet<InventoryConsumption> InventoryConsumptions => Set<InventoryConsumption>();

    // Warehouse Operations
    public DbSet<ProductInventory> ProductInventories => Set<ProductInventory>();

    public ExpendableDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<ExpendableDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment) : base(multiTenantContextAccessor, options, settings, environment)
    {
        ArgumentNullException.ThrowIfNull(multiTenantContextAccessor);
        ArgumentNullException.ThrowIfNull(settings);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExpendableDbContext).Assembly);

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(ExpendableModuleConstants.SchemaName));
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration(ExpendableModuleConstants.SchemaName));

        // Postgres-only: pg_trgm GIN indexes accelerate the leading-wildcard ILIKE keyword search that
        // SearchProducts runs over these columns (a b-tree can't serve '%kw%'). Guarded by provider so the
        // SQLite test harness (EnsureCreated) and EF InMemory skip the unsupported 'USING gin' DDL.
        if (Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            modelBuilder.HasPostgresExtension("pg_trgm");
            modelBuilder.Entity<Product>().HasIndex(p => p.Name)
                .HasDatabaseName("IX_Products_Name_trgm").HasMethod("gin").HasOperators("gin_trgm_ops");
            modelBuilder.Entity<Product>().HasIndex(p => p.StockNo)
                .HasDatabaseName("IX_Products_StockNo_trgm").HasMethod("gin").HasOperators("gin_trgm_ops");
            modelBuilder.Entity<Product>().HasIndex(p => p.Article)
                .HasDatabaseName("IX_Products_Article_trgm").HasMethod("gin").HasOperators("gin_trgm_ops");
        }

        // SQLite test harness only: 'xmin' is the Postgres system-column concurrency token
        // (ValueGeneratedOnAddOrUpdate) — Postgres emits no DDL and auto-maintains it. Under SQLite's
        // EnsureCreated it materializes as a real NOT NULL column with no store generation, so an EF insert
        // (which omits the token) trips a NOT NULL violation. Demote it to a plain column on SQLite so EF
        // sends the shadow default (0) on insert. No effect on Postgres, where the Npgsql path is untouched.
        if (Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var xmin = entityType.FindProperty("xmin");
                if (xmin is not null)
                    xmin.ValueGenerated = ValueGenerated.Never;
            }
        }
    }
}



