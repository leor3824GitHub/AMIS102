using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class UnserviceablePropertyItemConfiguration : IEntityTypeConfiguration<UnserviceablePropertyItem>
{
    public void Configure(EntityTypeBuilder<UnserviceablePropertyItem> builder)
    {
        builder.ToTable("UnserviceablePropertyItems", AssetManagementModuleConstants.SchemaName).IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReportId).IsRequired();
        builder.Property(x => x.TangibleInventoryItemId).IsRequired();
        builder.Property(x => x.ItemNo).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.AssetTypeAtTimeOfReport).HasConversion<string>().HasMaxLength(8).IsRequired();
        builder.Property(x => x.ConditionRemarks).HasMaxLength(500);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.ReportId });
        builder.HasIndex(x => new { x.TenantId, x.TangibleInventoryItemId });
        builder.HasIndex(x => x.ReportId);
        builder.HasIndex(x => x.TangibleInventoryItemId);
    }
}
