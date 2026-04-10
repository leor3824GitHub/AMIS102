using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class SemiExpendablePropertyConfiguration : IEntityTypeConfiguration<SemiExpendableProperty>
{
    public void Configure(EntityTypeBuilder<SemiExpendableProperty> builder)
    {
        builder.ToTable("SemiExpendableProperties", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.PropertyNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.SerialNo).HasMaxLength(100);
        builder.Property(x => x.AcquisitionDate).IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.FundCluster).HasMaxLength(50);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => x.PropertyNo).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CurrentCustodianId);

        builder.Property(x => x.SMRRItemId);
        builder.HasIndex(x => x.SMRRItemId);

        builder.HasOne(x => x.SemiExpendableItem)
            .WithMany()
            .HasForeignKey(x => x.SemiExpendableItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
