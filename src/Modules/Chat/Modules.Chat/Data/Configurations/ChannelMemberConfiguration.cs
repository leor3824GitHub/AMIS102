using AMIS.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Chat.Data.Configurations;

internal sealed class ChannelMemberConfiguration : IEntityTypeConfiguration<ChannelMember>
{
    public void Configure(EntityTypeBuilder<ChannelMember> builder)
    {
        builder.ToTable("ChannelMembers", ChatModuleConstants.SchemaName);

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.UserId).HasMaxLength(64).IsRequired();
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.TenantId).HasMaxLength(64);

        builder.HasIndex(m => new { m.ChannelId, m.UserId }).IsUnique();
        builder.HasIndex(m => m.UserId);
    }
}
