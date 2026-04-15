using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class PropertyIncidentReportConfiguration : IEntityTypeConfiguration<PropertyIncidentReport>
{
    public void Configure(EntityTypeBuilder<PropertyIncidentReport> builder)
    {
        builder.ToTable("PropertyIncidentReports", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReportNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.IncidentDate);
        builder.Property(x => x.IncidentType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.FundCluster).HasMaxLength(50);
        builder.Property(x => x.IncidentDetails).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => x.ReportNo).IsUnique();
        builder.HasIndex(x => x.Date);
        builder.HasIndex(x => x.IncidentType);
        builder.HasIndex(x => x.AccountableEmployeeId);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
