using System.Linq.Expressions;
using AMIS.Framework.Core.Domain;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Framework.Persistence.SignedDocuments;

/// <summary>
/// Maps the shared <see cref="SignedCopy"/> value object as an optional owned type on a document-of-record
/// aggregate. Each module's document config calls <see cref="ConfigureSignedCopy"/> so the column shape and
/// lengths stay identical across all modules that carry a signed copy.
/// </summary>
public static class SignedCopyConfigurationExtensions
{
    /// <summary>
    /// Configures <paramref name="navigation"/> (e.g. <c>x =&gt; x.SignedCopy</c>) as a nullable owned type,
    /// emitting <c>SignedCopy_*</c> columns on the owner table. When no copy is present the required columns
    /// are NULL, which EF uses to materialize the navigation as <c>null</c>.
    /// </summary>
    public static EntityTypeBuilder<TOwner> ConfigureSignedCopy<TOwner>(
        this EntityTypeBuilder<TOwner> builder,
        Expression<Func<TOwner, SignedCopy?>> navigation)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(navigation);

        builder.OwnsOne(navigation, b =>
        {
            // Required within the value — these are the presence-detection columns for the optional owned type.
            b.Property(p => p.StorageKey).HasMaxLength(1024).IsRequired();
            b.Property(p => p.Sha256).HasMaxLength(64).IsRequired();
            b.Property(p => p.FileName).HasMaxLength(260).IsRequired();
            b.Property(p => p.UploadedByName).HasMaxLength(200);
        });

        return builder;
    }
}
