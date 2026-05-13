using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetManagement.Data.Configurations;

public sealed class AssetAssignmentHistoryConfiguration : IEntityTypeConfiguration<AssetAssignmentHistory>
{
    public void Configure(EntityTypeBuilder<AssetAssignmentHistory> builder)
    {
        builder.ToTable("AssetAssignmentHistory", AssetManagementModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.AssetRegistryId).IsRequired();
        builder.Property(x => x.EventType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.OccurredOnUtc).IsRequired();
        builder.Property(x => x.SourceDocumentType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SourceDocumentId).IsRequired();
        builder.Property(x => x.SourceDocumentNo).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.AssetRegistryId, x.OccurredOnUtc });
        builder.HasIndex(x => new { x.TenantId, x.SourceDocumentType, x.SourceDocumentId });
        builder.HasIndex(x => new { x.TenantId, x.ToCustodianId });

        builder.HasOne<AssetRegistry>()
            .WithMany()
            .HasForeignKey(x => x.AssetRegistryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

