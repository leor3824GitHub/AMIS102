using AMIS.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Chat.Data.Configurations;

internal sealed class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.ToTable("MessageReactions", ChatModuleConstants.SchemaName);

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.UserId).HasMaxLength(64).IsRequired();
        builder.Property(r => r.Emoji).HasMaxLength(32).IsRequired();

        // Idempotent reactions: a double-tap or reconnect replay can't create a duplicate row.
        builder.HasIndex(r => new { r.MessageId, r.UserId, r.Emoji }).IsUnique();
    }
}
