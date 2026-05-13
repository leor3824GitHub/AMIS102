using AMIS.Modules.AssetRegister.Domain.Unserviceable;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetRegister.Data.Configurations;

internal sealed class UnserviceablePropertyReportConfiguration : IEntityTypeConfiguration<UnserviceablePropertyReport>
{
    public void Configure(EntityTypeBuilder<UnserviceablePropertyReport> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("UnserviceablePropertyReports", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Version).IsRowVersion();
        builder.Property(x => x.ReportNo).IsRequired().HasMaxLength(64);
        builder.Property(x => x.FundCluster).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Station).IsRequired().HasMaxLength(200);

        builder.OwnsOne(x => x.AccountableOfficer, n => n.ConfigureEmployeeRef("AccountableOfficer"));
        builder.OwnsOne(x => x.ApprovedBy, n => n.ConfigureEmployeeRef("ApprovedBy"));
        builder.OwnsOne(x => x.InspectedBy, n => n.ConfigureEmployeeRef("InspectedBy"));
        builder.OwnsOne(x => x.WitnessedBy, n => n.ConfigureEmployeeRef("WitnessedBy"));
        builder.Navigation(x => x.AccountableOfficer).IsRequired();

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(i => i.ReportId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).AutoInclude(false);

        builder.HasIndex(x => new { x.TenantId, x.ReportNo }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}

internal sealed class UnserviceablePropertyItemConfiguration : IEntityTypeConfiguration<UnserviceablePropertyItem>
{
    public void Configure(EntityTypeBuilder<UnserviceablePropertyItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("UnserviceablePropertyItems", AssetRegisterModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.SnapshotAcquisitionCost).HasPrecision(18, 2);
        builder.Property(x => x.SnapshotAccumulatedDepreciation).HasPrecision(18, 2);
        builder.Property(x => x.SnapshotAccumulatedImpairmentLosses).HasPrecision(18, 2);
        builder.Property(x => x.AppraisedValue).HasPrecision(18, 2);
        builder.Property(x => x.SaleAmount).HasPrecision(18, 2);
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.DisposalOtherSpecify).HasMaxLength(500);
        builder.Property(x => x.SaleORNo).HasMaxLength(64);

        builder.Ignore(x => x.SnapshotCarryingAmount);

        builder.OwnsOne(x => x.Snapshot, n => n.ConfigureSnapshot());
        builder.Navigation(x => x.Snapshot).IsRequired();

        builder.HasIndex(x => x.AssetRegistryId);
    }
}

