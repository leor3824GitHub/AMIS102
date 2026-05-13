using AMIS.Modules.AssetRegister.Domain.Issuance;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetRegister.Data.Configurations;

internal sealed class PropertyIssuanceReportConfiguration : IEntityTypeConfiguration<PropertyIssuanceReport>
{
    public void Configure(EntityTypeBuilder<PropertyIssuanceReport> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PropertyIssuanceReports", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Version).IsRowVersion();
        builder.Property(x => x.ReportNo).IsRequired().HasMaxLength(64);
        builder.Property(x => x.FundCluster).IsRequired().HasMaxLength(64);

        builder.OwnsOne(x => x.PreparedBy, n => n.ConfigureEmployeeRef("PreparedBy"));
        builder.OwnsOne(x => x.CertifiedBy, n => n.ConfigureEmployeeRef("CertifiedBy"));
        builder.OwnsOne(x => x.PostedBy, n => n.ConfigureEmployeeRef("PostedBy"));
        builder.Navigation(x => x.PreparedBy).IsRequired();

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(l => l.ReportId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Lines).AutoInclude(false);

        builder.HasIndex(x => new { x.TenantId, x.ReportNo }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}

internal sealed class PropertyIssuanceReportLineConfiguration : IEntityTypeConfiguration<PropertyIssuanceReportLine>
{
    public void Configure(EntityTypeBuilder<PropertyIssuanceReportLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PropertyIssuanceReportLines", AssetRegisterModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.SnapshotResponsibilityCenterCode).HasMaxLength(64);
        builder.Property(x => x.SnapshotUnitCost).HasPrecision(18, 2);
        builder.Property(x => x.SnapshotAmount).HasPrecision(18, 2);

        builder.OwnsOne(x => x.Snapshot, n => n.ConfigureSnapshot());
        builder.Navigation(x => x.Snapshot).IsRequired();

        builder.HasIndex(x => x.AccountabilityId);
        builder.HasIndex(x => x.AccountabilityLineId);
        builder.HasIndex(x => x.AssetRegistryId);
    }
}

