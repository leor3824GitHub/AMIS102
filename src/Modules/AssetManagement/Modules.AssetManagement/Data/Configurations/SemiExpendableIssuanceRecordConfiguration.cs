using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class SemiExpendableIssuanceRecordConfiguration : IEntityTypeConfiguration<SemiExpendableIssuanceRecord>
{
    public void Configure(EntityTypeBuilder<SemiExpendableIssuanceRecord> builder)
    {
        builder.ToTable("SemiExpendableIssuanceRecords", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.SMIRNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.FundCluster).HasMaxLength(50);
        builder.Property(x => x.IssuanceType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.TransferredToTenantId).HasMaxLength(64);
        builder.Property(x => x.TransferredToOfficerName).HasMaxLength(200);
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => x.SMIRNo).IsUnique();
        builder.HasIndex(x => x.Date);
        builder.HasIndex(x => x.IssuanceType);
        builder.HasIndex(x => x.TransferredToTenantId);
        builder.HasIndex(x => x.IssuedByEmployeeId);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
