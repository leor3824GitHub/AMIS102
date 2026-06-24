using AMIS.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Chat.Data.Configurations;

internal sealed class ChatChannelConfiguration : IEntityTypeConfiguration<ChatChannel>
{
    public void Configure(EntityTypeBuilder<ChatChannel> builder)
    {
        builder.ToTable("Channels", ChatModuleConstants.SchemaName);

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.TenantId).HasMaxLength(64);
        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.Scope).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(200);
        builder.Property(c => c.Topic).HasMaxLength(1000);
        builder.Property(c => c.DirectKey).HasMaxLength(256);
        builder.Property(c => c.CreatedBy).HasMaxLength(64).IsRequired();
        builder.Property(c => c.DeletedBy).HasMaxLength(64);
        builder.Property(c => c.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(c => c.TenantId);

        // Find-or-create DM race guard: in Postgres NULLs are distinct, so non-DM channels (DirectKey == null)
        // are unaffected while at most one DM per (office, user-pair) can exist.
        builder.HasIndex(c => new { c.TenantId, c.DirectKey }).IsUnique();

        builder.HasMany(c => c.Members)
            .WithOne()
            .HasForeignKey(m => m.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
