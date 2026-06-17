using AMIS.Modules.Finance.Domain.DisbursementVouchers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Finance.Data.Configurations;

public sealed class DvNumberSequenceConfiguration : IEntityTypeConfiguration<DvNumberSequence>
{
    public void Configure(EntityTypeBuilder<DvNumberSequence> builder)
    {
        builder.ToTable("DvNumberSequences", FinanceModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Year).IsRequired();
        builder.Property(x => x.LastSerial).IsRequired().HasDefaultValue(0);

        // PostgreSQL xmin system column — true optimistic concurrency, auto-updated by the DB on every UPDATE.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // One counter row per year.
        builder.HasIndex(x => x.Year).IsUnique();
    }
}
