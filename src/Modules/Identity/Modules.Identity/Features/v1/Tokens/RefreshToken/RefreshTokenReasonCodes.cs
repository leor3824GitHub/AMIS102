namespace AMIS.Modules.Identity.Features.v1.Tokens.RefreshToken;

internal static class RefreshTokenReasonCodes
{
    internal const string InvalidRefreshToken = "InvalidRefreshToken";
    internal const string SessionRevoked = "SessionRevoked";
    internal const string SubjectMismatch = "RefreshTokenSubjectMismatch";
    internal const string RefreshTokenRotated = "RefreshTokenRotated";
}

