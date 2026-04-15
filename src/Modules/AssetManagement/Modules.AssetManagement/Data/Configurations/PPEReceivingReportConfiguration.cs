using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class PPEReceivingReportConfiguration : IEntityTypeConfiguration<PPEReceivingReport>
{
    public void Configure(EntityTypeBuilder<PPEReceivingReport> builder)
    {
        builder.ToTable("PPEReceivingReports", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.PPERRNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.ReceivedFrom).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ReceiptNature).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ReceivedByEmployeeId).IsRequired();
        builder.Property(x => x.NotedByEmployeeId).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => x.PPERRNo).IsUnique();
        builder.HasIndex(x => x.Date);
        builder.HasIndex(x => x.ReceivedByEmployeeId);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
