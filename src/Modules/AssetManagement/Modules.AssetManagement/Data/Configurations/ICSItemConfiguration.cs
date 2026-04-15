using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class ICSItemConfiguration : IEntityTypeConfiguration<ICSItem>
{
    public void Configure(EntityTypeBuilder<ICSItem> builder)
    {
        builder.ToTable("ICSItems", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.ItemNo).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.CategoryAtTimeOfIssuance).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasIndex(x => x.ICSId);
        builder.HasIndex(x => x.SemiExpendablePropertyId);

        builder.HasOne<InventoryCustodianSlip>()
            .WithMany()
            .HasForeignKey(x => x.ICSId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<SemiExpendableProperty>()
            .WithMany()
            .HasForeignKey(x => x.SemiExpendablePropertyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
