namespace AMIS.Framework.Core.Domain;

/// <summary>
/// A document-of-record aggregate that can carry a single current wet-signed copy (<see cref="SignedCopy"/>).
/// Lets the upload pipeline attach the copy to a resolved aggregate without knowing its concrete type.
/// </summary>
public interface ISignedCopyHolder
{
    /// <summary>The current signed copy, or <c>null</c> if none has been uploaded.</summary>
    SignedCopy? SignedCopy { get; }

    /// <summary>Attaches (or replaces) the signed copy.</summary>
    void SetSignedCopy(SignedCopy copy);
}
