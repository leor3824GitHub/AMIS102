using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class PhysicalCountEntryConfiguration : IEntityTypeConfiguration<PhysicalCountEntry>
{
    public void Configure(EntityTypeBuilder<PhysicalCountEntry> builder)
    {
        builder.ToTable("PhysicalCountEntries", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SessionId).IsRequired();

        builder.Property(x => x.PPEItemId);
        builder.Property(x => x.SemiExpendablePropertyId);

        // Snapshot fields
        builder.Property(x => x.PropertyNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,2)").IsRequired();

        // Walk-through results
        builder.Property(x => x.Result).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Condition).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.QuantityOnHand).IsRequired();
        builder.Property(x => x.ScannedOnUtc);
        builder.Property(x => x.PhotoPath).HasMaxLength(500);

        // Indexes for common query patterns
        builder.HasIndex(x => x.SessionId);
        builder.HasIndex(x => x.PPEItemId);
        builder.HasIndex(x => x.SemiExpendablePropertyId);
        builder.HasIndex(x => x.Result);
        builder.HasIndex(x => new { x.SessionId, x.PPEItemId });
        builder.HasIndex(x => new { x.SessionId, x.SemiExpendablePropertyId });

        // FK — session
        builder.HasOne<PhysicalCountSession>()
            .WithMany()
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK — PPE item (restrict: don't delete a PPE item that appears in a count)
        builder.HasOne<PPEItem>()
            .WithMany()
            .HasForeignKey(x => x.PPEItemId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // FK — semi-expendable property (restrict)
        builder.HasOne<SemiExpendableProperty>()
            .WithMany()
            .HasForeignKey(x => x.SemiExpendablePropertyId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
