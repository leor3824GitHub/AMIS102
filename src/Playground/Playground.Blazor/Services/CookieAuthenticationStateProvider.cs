using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace AMIS.Playground.Blazor.Services;

/// <summary>
/// Simple authentication state provider that reads from cookie authentication.
/// Uses the built-in ServerAuthenticationStateProvider which automatically reads HttpContext.User.
/// </summary>
/// <remarks>
/// This class intentionally has no custom logic - it inherits all behavior from ServerAuthenticationStateProvider,
/// which automatically reads from HttpContext.User populated by the ASP.NET Core Cookie Authentication middleware.
/// The class exists to provide a named type for DI registration and potential future customization.
/// </remarks>
#pragma warning disable S2094 // Classes should not be empty - intentionally inherits all behavior from base class
internal sealed class CookieAuthenticationStateProvider : ServerAuthenticationStateProvider;
#pragma warning restore S2094

