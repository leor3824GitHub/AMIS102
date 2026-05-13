using AMIS.Playground.Blazor.ApiClient;
using AMIS.Framework.Shared.Multitenancy;
using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AMIS.Playground.Blazor.Services;

#pragma warning disable CA1515 // Extension method classes must be public
internal static class SimpleBffAuth
#pragma warning restore CA1515
{
    public static void MapSimpleBffAuthEndpoints(this WebApplication app)
    {
        // Login endpoint - calls identity API, sets cookie, returns success
        // Note: Uses /bff/ prefix to avoid conflict with ALB routing /api/* to the API service
        app.MapPost("/bff/auth/login", async (
            HttpContext httpContext,
            ITokenClient tokenClient,
            ILogger<Program> logger) =>
        {
            try
            {
                // Read form data
                var form = await httpContext.Request.ReadFormAsync();
                var email = form["Email"].ToString();
                var password = form["Password"].ToString();
                var tenant = form["Tenant"].ToString();

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    return Results.BadRequest("Email and password are required.");
                }

                var normalizedTenant = string.IsNullOrWhiteSpace(tenant)
                    ? MultitenancyConstants.Root.Id
                    : tenant.Trim();

                logger.LogInformation("Login attempt for {Email}", email);

                // Call the identity API to get token
                var token = await tokenClient.IssueAsync(
                    normalizedTenant,
                    new GenerateTokenCommand
                    {
                        Email = email,
                        Password = password
                    });

                if (token == null || string.IsNullOrEmpty(token.AccessToken))
                {
                    return Results.Unauthorized();
                }

                // Parse JWT to extract claims
                var jwtHandler = new JwtSecurityTokenHandler();
                var jwtToken = jwtHandler.ReadJwtToken(token.AccessToken);

                var userId = jwtToken.Subject
                    ?? jwtToken.Claims.FirstOrDefault(c =>
                            c.Type == ClaimTypes.NameIdentifier ||
                            c.Type == "nameid" ||
                            c.Type == "sub")?.Value
                    ?? Guid.NewGuid().ToString();

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, userId),
                    new(ClaimTypes.Email, email),
                    new("access_token", token.AccessToken), // Store JWT for API calls
                    new("refresh_token", token.RefreshToken), // Store refresh token for token renewal
                    new("tenant", normalizedTenant), // Store tenant for token refresh
                };

                // Add name claim
                var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "name" || c.Type == ClaimTypes.Name);
                if (nameClaim != null)
                {
                    claims.Add(new Claim(ClaimTypes.Name, nameClaim.Value));
                }

                // Add role claims
                var roleClaims = jwtToken.Claims.Where(c => c.Type == "role" || c.Type == ClaimTypes.Role);
                claims.AddRange(roleClaims.Select(r => new Claim(ClaimTypes.Role, r.Value)));

                // Create identity and sign in with cookie
                var identity = new ClaimsIdentity(claims, "Cookies");
                var principal = new ClaimsPrincipal(identity);

                await httpContext.SignInAsync("Cookies", principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

                logger.LogInformation("Login successful for {Email}", email);

                // Redirect to home page - this ensures the cookie is properly read on the next request
                return Results.Redirect("/");
            }
            catch (ApiException ex) when (ex.StatusCode == 401)
            {
                return Results.Unauthorized();
            }
            catch (ApiException ex)
            {
                logger.LogError(ex, "Login failed with API status {StatusCode}", ex.StatusCode);
                return Results.Problem("Login failed", statusCode: ex.StatusCode);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Login failed due to API connectivity/TLS issue");
                return Results.Problem("Cannot reach the API for login. Check API availability and local HTTPS certificate trust.", statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Login failed");
                return Results.Problem("Login failed");
            }
        })
        .AllowAnonymous()
        .DisableAntiforgery();

        // Logout endpoint - POST for API calls
        app.MapPost("/bff/auth/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync("Cookies");
            return Results.Ok();
        })
        .DisableAntiforgery();

        // Logout endpoint - GET for browser redirects (ensures cookie is cleared in browser)
        app.MapGet("/auth/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync("Cookies");

            var toast = httpContext.Request.Query["toast"].ToString();
            if (string.IsNullOrWhiteSpace(toast))
            {
                toast = "logout_success";
            }

            return Results.Redirect($"/login?toast={Uri.EscapeDataString(toast)}");
        })
        .AllowAnonymous();
    }
}

