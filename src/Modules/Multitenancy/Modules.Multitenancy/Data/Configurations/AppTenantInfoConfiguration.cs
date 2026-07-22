using AMIS.Framework.Shared.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Multitenancy.Data.Configurations;

public class AppTenantInfoConfiguration : IEntityTypeConfiguration<AppTenantInfo>
{
    public void Configure(EntityTypeBuilder<AppTenantInfo> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Tenants", MultitenancyConstants.Schema);

        builder.Property(x => x.OfficeCode).HasMaxLength(32);

        // Two tenants must never claim the same office, or resolving an employee's office to a destination
        // agency becomes ambiguous. Filtered so the many unlinked (null) tenants don't collide with each other.
        builder.HasIndex(x => x.OfficeId)
            .IsUnique()
            .HasFilter("\"OfficeId\" IS NOT NULL");
    }
}

