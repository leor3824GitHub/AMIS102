using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class CapitalizationThresholdPolicyConfiguration : IEntityTypeConfiguration<CapitalizationThresholdPolicy>
{
    public void Configure(EntityTypeBuilder<CapitalizationThresholdPolicy> builder)
    {
        builder.ToTable("CapitalizationThresholdPolicies", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.LowValueThreshold).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.CapitalizationThreshold).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.EffectiveDate).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.Version).IsConcurrencyToken();

        // Unique partial index: enforces that at most one policy can be active at a time.
        // A concurrent SetThresholdPolicy call will fail at the DB level rather than producing two active policies.
        builder.HasIndex(x => x.IsActive)
            .HasFilter("\"IsActive\" = true")
            .IsUnique();
        builder.HasIndex(x => x.EffectiveDate);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
