using AMIS.Modules.AssetRegister.Domain.Incidents;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetRegister.Data.Configurations;

internal sealed class PropertyIncidentReportConfiguration : IEntityTypeConfiguration<PropertyIncidentReport>
{
    public void Configure(EntityTypeBuilder<PropertyIncidentReport> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PropertyIncidentReports", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Version).IsRowVersion();
        builder.Property(x => x.IncidentNo).IsRequired().HasMaxLength(64);
        builder.Property(x => x.FundCluster).IsRequired().HasMaxLength(64);
        builder.Property(x => x.DepartmentOffice).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Circumstances).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.AccountableOfficerDesignation).IsRequired().HasMaxLength(200);
        builder.Property(x => x.AccountableOfficerGovIdType).HasMaxLength(64);
        builder.Property(x => x.AccountableOfficerGovIdNo).HasMaxLength(64);
        builder.Property(x => x.PoliceStation).HasMaxLength(200);
        builder.Property(x => x.PoliceBlotterRef).HasMaxLength(200);
        builder.Property(x => x.NotaryDocNo).HasMaxLength(64);
        builder.Property(x => x.NotaryPageNo).HasMaxLength(64);
        builder.Property(x => x.NotaryBookNo).HasMaxLength(64);
        builder.Property(x => x.NotarySeriesOf).HasMaxLength(64);
        builder.Property(x => x.ReliefGrantedRef).HasMaxLength(200);
        builder.Property(x => x.AmountSettled).HasPrecision(18, 2);

        builder.OwnsOne(x => x.AccountableOfficer, n => n.ConfigureEmployeeRef("AccountableOfficer"));
        builder.OwnsOne(x => x.NotedBy, n => n.ConfigureEmployeeRef("NotedBy"));
        builder.Navigation(x => x.AccountableOfficer).IsRequired();

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(i => i.ReportId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).AutoInclude(false);

        builder.HasIndex(x => new { x.TenantId, x.IncidentNo }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}

internal sealed class PropertyIncidentItemConfiguration : IEntityTypeConfiguration<PropertyIncidentItem>
{
    public void Configure(EntityTypeBuilder<PropertyIncidentItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PropertyIncidentItems", AssetRegisterModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.SnapshotAcquisitionCost).HasPrecision(18, 2);
        builder.Property(x => x.SnapshotCurrentReplacementCost).HasPrecision(18, 2);

        builder.OwnsOne(x => x.Snapshot, n => n.ConfigureSnapshot());
        builder.Navigation(x => x.Snapshot).IsRequired();

        builder.HasIndex(x => x.AssetRegistryId);
    }
}

