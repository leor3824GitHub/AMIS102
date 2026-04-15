using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class PhysicalCountSessionConfiguration : IEntityTypeConfiguration<PhysicalCountSession>
{
    public void Configure(EntityTypeBuilder<PhysicalCountSession> builder)
    {
        builder.ToTable("PhysicalCountSessions", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.SessionNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CountDate).IsRequired();
        builder.Property(x => x.StationOffice).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Scope).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.PreparedByEmployeeId).IsRequired();
        builder.Property(x => x.CertifiedByEmployeeId).IsRequired();
        builder.Property(x => x.ApprovedByEmployeeId).IsRequired();
        builder.Property(x => x.SubmittedOnUtc);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => x.SessionNo).IsUnique();
        builder.HasIndex(x => x.CountDate);
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
