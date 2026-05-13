using System.Security.Claims;
using AMIS.Framework.Core.Context;

namespace AMIS.Modules.Identity.Contracts.Services;

/// <summary>
/// Service interface for managing the current user context.
/// Combines user identity access with initialization capabilities.
/// </summary>
public interface ICurrentUserService : ICurrentUser, ICurrentUserInitializer
{
}

