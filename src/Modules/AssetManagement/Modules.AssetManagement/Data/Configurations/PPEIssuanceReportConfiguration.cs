using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class PPEIssuanceReportConfiguration : IEntityTypeConfiguration<PPEIssuanceReport>
{
    public void Configure(EntityTypeBuilder<PPEIssuanceReport> builder)
    {
        builder.ToTable("PPEIssuanceReports", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.PPEIRNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.IssuedToEmployeeId).IsRequired();
        builder.Property(x => x.IssuedToOfficeAddress).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IssuanceType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.IssuedByEmployeeId).IsRequired();
        builder.Property(x => x.ReceivedByEmployeeId).IsRequired();
        builder.Property(x => x.DateReceived);
        builder.Property(x => x.ApprovedByEmployeeId).IsRequired();
        builder.Property(x => x.DriverName).HasMaxLength(200);
        builder.Property(x => x.BillOfLadingNo).HasMaxLength(100);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => x.PPEIRNo).IsUnique();
        builder.HasIndex(x => x.Date);
        builder.HasIndex(x => x.IssuedToEmployeeId);
        builder.HasIndex(x => x.IssuanceType);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
