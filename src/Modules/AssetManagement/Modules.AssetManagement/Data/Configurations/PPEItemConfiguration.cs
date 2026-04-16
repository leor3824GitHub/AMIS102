using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class PPEItemConfiguration : IEntityTypeConfiguration<PPEItem>
{
    public void Configure(EntityTypeBuilder<PPEItem> builder)
    {
        builder.ToTable("PPEItems", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.PropertyCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.PropertyNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ClassCode).HasMaxLength(4);
        builder.Property(x => x.ItemCode).HasMaxLength(2);
        builder.Property(x => x.OfficeCode).HasMaxLength(8);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SerialNumber).HasMaxLength(100);
        builder.Property(x => x.DateAcquired).IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.EstimatedUsefulLifeYears).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CurrentAccountableEmployeeId);
        builder.Property(x => x.SourcePPERRId);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => x.PropertyCode).IsUnique();
        builder.HasIndex(x => x.PropertyNumber).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CurrentAccountableEmployeeId);
        builder.HasIndex(x => x.SourcePPERRId);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
