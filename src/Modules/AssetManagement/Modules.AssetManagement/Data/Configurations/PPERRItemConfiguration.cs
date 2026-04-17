using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class PPERRItemConfiguration : IEntityTypeConfiguration<PPERRItem>
{
    public void Configure(EntityTypeBuilder<PPERRItem> builder)
    {
        builder.ToTable("PPERRItems", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.PPERRId).IsRequired();
        builder.Property(x => x.ItemNo).IsRequired();
        builder.Property(x => x.PropertyCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ClassCode).HasMaxLength(4);
        builder.Property(x => x.ItemCode).HasMaxLength(2);
        builder.Property(x => x.OfficeCode).HasMaxLength(8);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DateAcquired).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.Amount).HasColumnType("numeric(18,2)").IsRequired();

        builder.Property(x => x.ItemId);
        builder.HasIndex(x => x.PPERRId);
        builder.HasIndex(x => x.ItemId);

        builder.HasOne<PropertyItemCatalog>()
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
