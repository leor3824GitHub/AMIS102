using AMIS.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Chat.Data.Configurations;

internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages", ChatModuleConstants.SchemaName);

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.SenderId).HasMaxLength(64).IsRequired();
        builder.Property(m => m.SenderName).HasMaxLength(200);
        builder.Property(m => m.Content).HasMaxLength(4000).IsRequired();
        builder.Property(m => m.TenantId).HasMaxLength(64);
        builder.Property(m => m.PinnedBy).HasMaxLength(64);
        builder.Property(m => m.DeletedBy).HasMaxLength(64);

        builder.Ignore(m => m.IsDeleted);

        // Keyset pagination key: newest-first within a channel ("messages before id X, take N").
        builder.HasIndex(m => new { m.ChannelId, m.Id });
        builder.HasIndex(m => m.ParentMessageId);

        builder.HasMany(m => m.Mentions)
            .WithOne()
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Reactions)
            .WithOne()
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(m => m.Mentions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(m => m.Reactions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
