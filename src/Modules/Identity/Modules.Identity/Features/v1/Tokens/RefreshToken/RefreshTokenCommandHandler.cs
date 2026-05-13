using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Auditing.Contracts;
using AMIS.Modules.Identity.Contracts.Services;
using AMIS.Modules.Identity.Contracts.v1.Tokens.RefreshToken;
using Mediator;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AMIS.Modules.Identity.Features.v1.Tokens.RefreshToken;

public sealed class RefreshTokenCommandHandler
    : ICommandHandler<RefreshTokenCommand, RefreshTokenCommandResponse>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly ISecurityAudit _securityAudit;
    private readonly IRequestContext _requestContext;
    private readonly ISessionService _sessionService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    private static readonly Meter RefreshMeter = new("AMIS.Modules.Identity.Refresh", "1.0.0");
    private static readonly Counter<long> RefreshRequestsCounter = RefreshMeter.CreateCounter<long>("identity_refresh_requests_total");
    private static readonly Counter<long> RefreshSuccessCounter = RefreshMeter.CreateCounter<long>("identity_refresh_success_total");
    private static readonly Counter<long> RefreshFailuresCounter = RefreshMeter.CreateCounter<long>("identity_refresh_failures_total");

    public RefreshTokenCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        ISecurityAudit securityAudit,
        IRequestContext requestContext,
        ISessionService sessionService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _securityAudit = securityAudit;
        _requestContext = requestContext;
        _sessionService = sessionService;
        _logger = logger;
    }

    public async ValueTask<RefreshTokenCommandResponse> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        RefreshRequestsCounter.Add(1);

        var clientId = _requestContext.ClientId ?? "unknown-client";

        // Validate refresh token and rebuild subject + claims
        var validated = await _identityService
            .ValidateRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (validated is null)
        {
            TrackFailure(RefreshTokenReasonCodes.InvalidRefreshToken, clientId);
            await _securityAudit.TokenRevokedAsync("unknown", clientId, RefreshTokenReasonCodes.InvalidRefreshToken, cancellationToken);
            throw new UnauthorizedException("Invalid refresh token.");
        }

        var (subject, claims) = validated.Value;

        // Check if the session associated with this refresh token is still valid
        var refreshTokenHash = Sha256Short(request.RefreshToken);
        var isSessionValid = await _sessionService.ValidateSessionAsync(refreshTokenHash, cancellationToken);
        if (!isSessionValid)
        {
            TrackFailure(RefreshTokenReasonCodes.SessionRevoked, clientId);
            await _securityAudit.TokenRevokedAsync(subject, clientId, RefreshTokenReasonCodes.SessionRevoked, cancellationToken);
            throw new UnauthorizedException("Session has been revoked.");
        }

        // Optionally, cross-check the provided access token subject
        var handler = new JwtSecurityTokenHandler();
        JwtSecurityToken? parsedAccessToken = null;
        try
        {
            parsedAccessToken = handler.ReadJwtToken(request.Token);
        }
        catch
        {
            // Ignore parsing errors and rely on refresh-token validation
        }

        if (parsedAccessToken is not null)
        {
            var accessTokenSubject = parsedAccessToken.Subject
                ?? parsedAccessToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value
                ?? parsedAccessToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(accessTokenSubject) &&
                !string.Equals(accessTokenSubject, subject, StringComparison.Ordinal))
            {
                TrackFailure(RefreshTokenReasonCodes.SubjectMismatch, clientId);
                await _securityAudit.TokenRevokedAsync(subject, clientId, RefreshTokenReasonCodes.SubjectMismatch, cancellationToken);
                throw new UnauthorizedException("Access token subject mismatch.");
            }
        }

        // Audit previous token revocation by rotation (no raw tokens)
        await _securityAudit.TokenRevokedAsync(subject, clientId, RefreshTokenReasonCodes.RefreshTokenRotated, cancellationToken);

        // Issue new tokens
        var newToken = await _tokenService.IssueAsync(subject, claims, null, cancellationToken);

        // Persist rotated refresh token for this user
        await _identityService.StoreRefreshTokenAsync(subject, newToken.RefreshToken, newToken.RefreshTokenExpiresAt, cancellationToken);

        // Update the session with the new refresh token hash
        var newRefreshTokenHash = Sha256Short(newToken.RefreshToken);
        await _sessionService.UpdateSessionRefreshTokenAsync(
            refreshTokenHash,
            newRefreshTokenHash,
            newToken.RefreshTokenExpiresAt,
            cancellationToken);

        // Audit the newly issued token with a fingerprint
        var fingerprint = Sha256Short(newToken.AccessToken);
        await _securityAudit.TokenIssuedAsync(
            userId: subject,
            userName: claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty,
            clientId: clientId,
            tokenFingerprint: fingerprint,
            expiresUtc: newToken.AccessTokenExpiresAt,
            ct: cancellationToken);

        RefreshSuccessCounter.Add(1);

        return new RefreshTokenCommandResponse(
            Token: newToken.AccessToken,
            RefreshToken: newToken.RefreshToken,
            RefreshTokenExpiryTime: newToken.RefreshTokenExpiresAt);
    }

    private void TrackFailure(string reasonCode, string clientId)
    {
        RefreshFailuresCounter.Add(1, new KeyValuePair<string, object?>("reason", reasonCode));
        _logger.LogWarning("Refresh token request failed. ReasonCode={ReasonCode}, ClientId={ClientId}", reasonCode, clientId);
    }

    private static string Sha256Short(string value)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }
}

