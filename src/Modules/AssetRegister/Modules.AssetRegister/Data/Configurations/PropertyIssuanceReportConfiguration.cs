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
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.Property(x => x.ReportNo).IsRequired().HasMaxLength(64);
        builder.Property(x => x.FundCluster).IsRequired().HasMaxLength(64);
        builder.Property(x => x.IssuedToOfficeAddress).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DriverName).HasMaxLength(200);
        builder.Property(x => x.BillOfLadingNo).HasMaxLength(100);
        builder.Property(x => x.Remarks).HasMaxLength(1000);

        builder.OwnsOne(x => x.IssuedBy, n => n.ConfigureEmployeeRef("IssuedBy"));
        builder.OwnsOne(x => x.ApprovedBy, n => n.ConfigureEmployeeRef("ApprovedBy"));
        builder.OwnsOne(x => x.IssuedTo, n => n.ConfigureEmployeeRef("IssuedTo"));
        builder.OwnsOne(x => x.ReceivedBy, n => n.ConfigureEmployeeRef("ReceivedBy"));
        builder.Navigation(x => x.IssuedBy).IsRequired();
        builder.Navigation(x => x.ApprovedBy).IsRequired();
        builder.Navigation(x => x.IssuedTo).IsRequired();
        builder.Navigation(x => x.ReceivedBy).IsRequired();

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(l => l.ReportId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Lines).AutoInclude(false);

        builder.HasIndex(x => new { x.TenantId, x.ReportNo }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Date });
    }
}

internal sealed class PropertyIssuanceReportLineConfiguration : IEntityTypeConfiguration<PropertyIssuanceReportLine>
{
    public void Configure(EntityTypeBuilder<PropertyIssuanceReportLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PropertyIssuanceReportLines", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.SnapshotUnitCost).HasPrecision(18, 2);
        builder.Property(x => x.SnapshotAmount).HasPrecision(18, 2);
        builder.Property(x => x.AccumulatedDepreciation).HasPrecision(18, 2);
        builder.Property(x => x.BookValue).HasPrecision(18, 2);

        builder.OwnsOne(x => x.Snapshot, n => n.ConfigureSnapshot());
        builder.Navigation(x => x.Snapshot).IsRequired();

        builder.HasIndex(x => x.AssetRegistryId);
        builder.HasIndex(x => new { x.ReportId, x.ItemNo });
    }
}
