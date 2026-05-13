using Mediator;

namespace AMIS.Modules.Identity.Contracts.v1.Tokens.RefreshToken;

public record RefreshTokenCommand(string Token, string RefreshToken)
    : ICommand<RefreshTokenCommandResponse>;
