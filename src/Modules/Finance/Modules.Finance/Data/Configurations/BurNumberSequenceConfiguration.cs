using AMIS.Modules.Finance.Domain.BudgetUtilizationRecords;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Finance.Data.Configurations;

public sealed class BurNumberSequenceConfiguration : IEntityTypeConfiguration<BurNumberSequence>
{
    public void Configure(EntityTypeBuilder<BurNumberSequence> builder)
    {
        builder.ToTable("BurNumberSequences", FinanceModuleConstants.SchemaName);

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
