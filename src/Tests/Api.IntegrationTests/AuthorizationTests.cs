using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.Identity.Domain;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Api.IntegrationTests;

/// <summary>
/// Proves that <c>.RequirePermission(...)</c> is actually enforced by the HTTP pipeline.
/// </summary>
/// <remarks>
/// <para>
/// <b>These tests are expected to FAIL until the authorization binding defect is fixed.</b>
/// <c>RequiredPermissionAuthorizationHandler</c> resolves <c>IRequiredPermissionMetadata</c> to an
/// orphan interface in <c>AMIS.Modules.Identity</c> (implemented by nothing) instead of the framework
/// one that <c>RequiredPermissionAttribute</c> implements, so the metadata lookup always returns null
/// and the handler calls <c>context.Succeed()</c>. Every authenticated user passes every endpoint.
/// </para>
/// <para>
/// This has to be an HTTP-level test. A unit test that constructs an
/// <c>AuthorizationHandlerContext</c> by hand passes against the broken code — it would be written
/// against whichever interface the author picked, which is exactly the trap that let this survive
/// since the original port. Only a real request carries real endpoint metadata.
/// </para>
/// </remarks>
[Collection(ApiCollection.Name)]
public sealed class AuthorizationTests(ApiFactory factory)
{
    private const string TenantHeader = MultitenancyConstants.Identifier;
    private const string RootTenant = MultitenancyConstants.Root.Id;
    private const string AdminEmail = MultitenancyConstants.Root.EmailAddress;
    private const string KnownPassword = MultitenancyConstants.DefaultPassword;

    /// <summary>An endpoint gated by Users.View — a permission a role-less user cannot hold.</summary>
    private static string ProtectedRoute(Guid id) => $"/api/v1/identity/users/{id}";

    private HttpClient NewClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TenantHeader, RootTenant);
        return client;
    }

    private async Task<string> IssueTokenAsync(string email, string password)
    {
        using var client = NewClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/identity/token/issue",
            new { Email = email, Password = password });

        response.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            $"Could not issue a token for '{email}'. Body: {await response.Content.ReadAsStringAsync()}");

        var token = await response.Content.ReadFromJsonAsync<TokenPayload>();
        token.ShouldNotBeNull();
        token!.AccessToken.ShouldNotBeNullOrWhiteSpace();
        return token.AccessToken;
    }

    /// <summary>
    /// Creates a user holding NO roles, so it derives zero permissions. That is deliberately stronger
    /// than picking a user who merely lacks one permission: it means *any* gated endpoint must reject
    /// it, so the assertion cannot be undermined by the Basic role quietly gaining a grant later.
    /// </summary>
    private async Task<(string Email, string Password)> CreateRolelessUserAsync()
    {
        var email = $"noroles-{Guid.NewGuid():N}@test.local";

        await using var scope = factory.Services.CreateAsyncScope();

        // UserManager writes through the multi-tenant IdentityDbContext, which has no ambient tenant
        // outside a request — set one explicitly or the row lands unscoped.
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(
                new AppTenantInfo(RootTenant, RootTenant, "Root") { AdminEmail = AdminEmail });

        var users = scope.ServiceProvider.GetRequiredService<UserManager<AmisUser>>();
        var user = new AmisUser
        {
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            EmailConfirmed = true,
            IsActive = true,
        };

        var created = await users.CreateAsync(user, KnownPassword);
        created.Succeeded.ShouldBeTrue(
            $"Test setup failed to create the user: {string.Join("; ", created.Errors.Select(e => e.Description))}");

        return (email, KnownPassword);
    }

    [Fact]
    public async Task Anonymous_request_to_a_protected_endpoint_is_rejected()
    {
        using var client = NewClient();

        var response = await client.GetAsync(ProtectedRoute(Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task User_without_the_permission_is_forbidden()
    {
        var (email, password) = await CreateRolelessUserAsync();
        var token = await IssueTokenAsync(email, password);

        using var client = NewClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(ProtectedRoute(Guid.NewGuid()));

        // The user holds no roles and therefore no permissions, so the endpoint's
        // .RequirePermission(Users.View) must reject it before the handler ever runs.
        // Anything other than 403 — including 404 from the handler — means authorization
        // did not run and the endpoint is open to every authenticated caller.
        response.StatusCode.ShouldBe(
            HttpStatusCode.Forbidden,
            $"A user with no roles reached a Users.View endpoint and got {(int)response.StatusCode}. " +
            "Permission enforcement is not running.");
    }

    [Fact]
    public async Task User_with_the_permission_is_allowed()
    {
        var token = await IssueTokenAsync(AdminEmail, KnownPassword);

        using var client = NewClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(ProtectedRoute(Guid.NewGuid()));

        // The admin holds Users.View, so authorization must pass. The id is random, so the handler
        // is expected to answer 404 — the point is that it is the *handler* answering, not authz.
        // Guards against a fix that over-corrects into denying everyone.
        response.StatusCode.ShouldNotBe(HttpStatusCode.Forbidden);
        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
    }

    private sealed record TokenPayload(string AccessToken, string RefreshToken);
}
