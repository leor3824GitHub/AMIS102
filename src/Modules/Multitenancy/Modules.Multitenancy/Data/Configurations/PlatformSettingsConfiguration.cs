using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.Multitenancy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Multitenancy.Data.Configurations;

public class PlatformSettingsConfiguration : IEntityTypeConfiguration<PlatformSettings>
{
    public void Configure(EntityTypeBuilder<PlatformSettings> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PlatformSettings", MultitenancyConstants.Schema);

        builder.HasKey(s => s.Id);

        // Session
        builder.Property(s => s.MaxSessionsPerUser);
        builder.Property(s => s.IdleTimeoutMinutes);
        builder.Property(s => s.AbsoluteTimeoutDays).IsRequired();

        // Quotas
        builder.Property(s => s.MaxUsersPerTenant);
        builder.Property(s => s.StorageLimitMb);
        builder.Property(s => s.ApiRateLimitPerMinute);

        // Audit
        builder.Property(s => s.CreatedOnUtc).IsRequired();
        builder.Property(s => s.CreatedBy).HasMaxLength(256);
        builder.Property(s => s.LastModifiedBy).HasMaxLength(256);
    }
}
