using AMIS.Modules.BudgetDisbursement.Domain.Settings;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.BudgetDisbursement.Data.Configurations;

public sealed class BudgetDisbursementModuleSettingsConfiguration : IEntityTypeConfiguration<BudgetDisbursementModuleSettings>
{
    public void Configure(EntityTypeBuilder<BudgetDisbursementModuleSettings> builder)
    {
        builder.ToTable("BudgetDisbursementModuleSettings", BudgetDisbursementModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.WatermarkSignedCopies).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.DvSectionAName).HasMaxLength(200);
        builder.Property(x => x.DvSectionADesignation).HasMaxLength(200);
        builder.Property(x => x.DvSectionBName).HasMaxLength(200);
        builder.Property(x => x.DvSectionBDesignation).HasMaxLength(200);
        builder.Property(x => x.DvSectionCName).HasMaxLength(200);
        builder.Property(x => x.DvSectionCDesignation).HasMaxLength(200);

        builder.Property(x => x.BurSectionAName).HasMaxLength(200);
        builder.Property(x => x.BurSectionADesignation).HasMaxLength(200);
        builder.Property(x => x.BurSectionBName).HasMaxLength(200);
        builder.Property(x => x.BurSectionBDesignation).HasMaxLength(200);

        // Exactly one settings row per tenant.
        builder.HasIndex(x => x.TenantId).IsUnique();
    }
}
