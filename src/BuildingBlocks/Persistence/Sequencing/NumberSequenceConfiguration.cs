using AMIS.Framework.Core.Domain;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Framework.Persistence.Sequencing;

/// <summary>
/// Maps the shared <see cref="NumberSequence"/> counter into a module's schema. Because the entity type lives
/// in the framework (not the module assembly), it is not picked up by
/// <c>ApplyConfigurationsFromAssembly</c> — each module calls <see cref="ConfigureNumberSequences"/> explicitly
/// from its <c>OnModelCreating</c>.
/// </summary>
public static class NumberSequenceModelBuilderExtensions
{
    /// <summary>
    /// Configures a <c>NumberSequences</c> table in <paramref name="schema"/> with the standard PostgreSQL
    /// <c>xmin</c> optimistic-concurrency token and a unique index on
    /// (TenantId, SequenceKey, Year, Month).
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="schema">Owning module schema, e.g. "procurement".</param>
    /// <param name="multiTenant">
    /// <c>true</c> to apply Finbuckle tenant filtering (per-tenant series); <c>false</c> for a global series
    /// (e.g. year-only DV/BUR numbers), where rows store <c>TenantId = ""</c>.
    /// </param>
    public static ModelBuilder ConfigureNumberSequences(this ModelBuilder modelBuilder, string schema, bool multiTenant)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var entity = modelBuilder.Entity<NumberSequence>();
        var table = entity.ToTable("NumberSequences", schema);
        if (multiTenant)
            table.IsMultiTenant();

        entity.HasKey(x => x.Id);
        entity.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        entity.Property(x => x.SequenceKey).HasMaxLength(32).IsRequired();
        entity.Property(x => x.Year).IsRequired();
        entity.Property(x => x.Month).IsRequired();
        entity.Property(x => x.LastSerial).IsRequired().HasDefaultValue(0);

        // PostgreSQL xmin is a system column tracking the transaction that last modified the row. It updates
        // automatically on every UPDATE, giving true optimistic concurrency without a separate column.
        entity.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // One counter row per (TenantId, SequenceKey, Year, Month).
        entity.HasIndex(x => new { x.TenantId, x.SequenceKey, x.Year, x.Month }).IsUnique();

        return modelBuilder;
    }
}
