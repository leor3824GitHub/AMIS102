using AMIS.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Chat.Data.Configurations;

internal sealed class MessageMentionConfiguration : IEntityTypeConfiguration<MessageMention>
{
    public void Configure(EntityTypeBuilder<MessageMention> builder)
    {
        builder.ToTable("MessageMentions", ChatModuleConstants.SchemaName);

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.MentionedUserId).HasMaxLength(64).IsRequired();

        builder.HasIndex(m => m.MessageId);
        builder.HasIndex(m => new { m.MessageId, m.MentionedUserId }).IsUnique();
    }
}
