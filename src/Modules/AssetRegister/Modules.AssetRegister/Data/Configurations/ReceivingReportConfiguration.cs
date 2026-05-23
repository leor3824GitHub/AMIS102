using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.AssetRegister.Domain.Receiving;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetRegister.Data.Configurations;

internal sealed class ReceivingReportConfiguration : IEntityTypeConfiguration<ReceivingReport>
{
    public void Configure(EntityTypeBuilder<ReceivingReport> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ReceivingReports", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.Property(x => x.ReportNo).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ReceivedFrom).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.OtherReceiptType).HasMaxLength(200);
        builder.Property(x => x.FundCluster).HasMaxLength(64);

        builder.OwnsOne(x => x.ReceivedBy, n => n.ConfigureEmployeeRef("ReceivedBy"));
        builder.OwnsOne(x => x.NotedBy, n => n.ConfigureEmployeeRef("NotedBy"));
        builder.Navigation(x => x.ReceivedBy).IsRequired();

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(i => i.ReportId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).AutoInclude(false);

        builder.HasIndex(x => new { x.TenantId, x.ReportNo }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.DocumentKind, x.Date });
    }
}

internal sealed class ReceivingReportItemConfiguration : IEntityTypeConfiguration<ReceivingReportItem>
{
    public void Configure(EntityTypeBuilder<ReceivingReportItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ReceivingReportItems", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Reference).HasMaxLength(64);
        builder.Property(x => x.PropertyNo).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.UnitCost).HasPrecision(18, 2);
        builder.Property(x => x.SerialNo).HasMaxLength(200);
        builder.Property(x => x.Brand).HasMaxLength(200);
        builder.Property(x => x.Model).HasMaxLength(200);
        builder.Property(x => x.UacsObjectCode).HasMaxLength(64);
        builder.Property(x => x.SourceAgencyName).HasMaxLength(200);
        builder.Property(x => x.SourcePropertyNo).HasMaxLength(64);
        builder.Property(x => x.SourceDocumentRef).HasMaxLength(200);
        builder.Ignore(x => x.Amount);

        builder.HasIndex(x => x.CatalogItemId);
    }
}

internal sealed class PPERRFormSeriesConfiguration : IEntityTypeConfiguration<PPERRFormSeries>
{
    public void Configure(EntityTypeBuilder<PPERRFormSeries> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PPERRFormSeries", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Label).IsRequired().HasMaxLength(200);
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Ignore(x => x.IsExhausted);
        builder.Ignore(x => x.Remaining);

        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}

