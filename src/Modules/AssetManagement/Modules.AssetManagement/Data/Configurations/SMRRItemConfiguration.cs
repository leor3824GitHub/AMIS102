using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class SMRRItemConfiguration : IEntityTypeConfiguration<SMRRItem>
{
    public void Configure(EntityTypeBuilder<SMRRItem> builder)
    {
        builder.ToTable("SMRRItems", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.PropertyNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.AcquisitionDate).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.Amount).HasColumnType("numeric(18,2)").IsRequired();

        builder.HasIndex(x => x.SMRRId);
        builder.HasIndex(x => x.ItemId);
        builder.HasIndex(x => x.TangibleItemId).IsUnique();

        builder.HasOne<SuppliesMaterialsReceivingReport>()
            .WithMany()
            .HasForeignKey(x => x.SMRRId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<PropertyItemCatalog>()
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TangibleItem>()
            .WithMany()
            .HasForeignKey(x => x.TangibleItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
