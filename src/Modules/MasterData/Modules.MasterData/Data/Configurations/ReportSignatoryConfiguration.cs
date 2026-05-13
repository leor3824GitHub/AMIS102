using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.MasterData.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.MasterData.Data.Configurations;

public sealed class ReportSignatoryConfiguration : IEntityTypeConfiguration<ReportSignatory>
{
    public void Configure(EntityTypeBuilder<ReportSignatory> builder)
    {
        builder.ToTable("ReportSignatories", MasterDataModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReportType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => new { x.TenantId, x.ReportType, x.SortOrder }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.ReportType });

        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);
    }
}

