using AMIS.Modules.AssetRegister.Domain.Repairs;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetRegister.Data.Configurations;

internal sealed class PropertyRepairConfiguration : IEntityTypeConfiguration<PropertyRepair>
{
    public void Configure(EntityTypeBuilder<PropertyRepair> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PropertyRepairs", AssetRegisterModuleConstants.SchemaName).IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Property(x => x.RpriNo).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Status).HasConversion<int>();

        builder.Property(x => x.NatureOfWork).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.PartsToReplace).HasMaxLength(2000);
        builder.Property(x => x.RequestedBy).IsRequired().HasMaxLength(200);
        builder.Property(x => x.InspectorName).HasMaxLength(200);

        builder.Property(x => x.EngineNo).HasMaxLength(100);
        builder.Property(x => x.ChassisNo).HasMaxLength(100);

        builder.Property(x => x.NatureOfLastRepair).HasMaxLength(2000);

        builder.Property(x => x.PreInspectionFindings).HasMaxLength(2000);
        builder.Property(x => x.PreInspectedBy).HasMaxLength(200);
        builder.Property(x => x.NotedBy).HasMaxLength(200);

        builder.Property(x => x.RepairShop).HasMaxLength(300);
        builder.Property(x => x.JobOrderNo).HasMaxLength(64);
        builder.Property(x => x.InvoiceNo).HasMaxLength(64);
        builder.Property(x => x.AmountPerJO).HasPrecision(18, 2);
        builder.Property(x => x.PostInspectionFindings).HasMaxLength(2000);
        builder.Property(x => x.PostInspectedBy).HasMaxLength(200);

        builder.Property(x => x.PrNo).HasMaxLength(64);
        builder.Property(x => x.PoJoNo).HasMaxLength(64);
        builder.Property(x => x.BurNo).HasMaxLength(64);
        builder.Property(x => x.DvNo).HasMaxLength(64);

        builder.Property(x => x.AcceptedBy).HasMaxLength(200);

        builder.HasIndex(x => new { x.TenantId, x.AssetRegistryId });
        builder.HasIndex(x => new { x.TenantId, x.RpriNo }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}
