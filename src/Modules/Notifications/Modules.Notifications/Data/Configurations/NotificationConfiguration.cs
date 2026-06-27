using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Notifications.Data.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", NotificationsModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RecipientUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Link).HasMaxLength(512);
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb");
        builder.Property(x => x.Source).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.LastModifiedBy).HasMaxLength(64);

        // Inbox query: the caller's rows, unread-first filtering, newest first.
        builder.HasIndex(x => new { x.RecipientUserId, x.IsRead });

        // Idempotency: a producer redelivering the same event must not create a duplicate row.
        builder.HasIndex(x => new { x.CorrelationId, x.RecipientUserId, x.Type }).IsUnique();
    }
}
