using AMIS.Modules.Expendable.Domain.Cart;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Expendable.Data.Configurations;

public class EmployeeShoppingCartConfiguration : IEntityTypeConfiguration<EmployeeShoppingCart>
{
    public void Configure(EntityTypeBuilder<EmployeeShoppingCart> builder)
    {
        builder.ToTable($"{nameof(EmployeeShoppingCart)}s", ExpendableModuleConstants.SchemaName)
            .IsMultiTenant();

        // Primary Key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.TenantId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.EmployeeId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<int>();

        builder.Property(p => p.Version)
            .IsConcurrencyToken();

        // Items (Owned Type)
        builder.OwnsMany(p => p.Items, ob =>
        {
            ob.ToJson("Items");
            ob.Property(x => x.UnitPrice).HasPrecision(18, 2);
        });

        // Indexes
        builder.HasIndex(p => new { p.TenantId, p.EmployeeId })
            .IsUnique(false);

        builder.HasIndex(p => new { p.TenantId, p.Status });

        // Soft Delete
        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        builder.HasQueryFilter("SoftDelete", p => !p.IsDeleted);
    }
}


