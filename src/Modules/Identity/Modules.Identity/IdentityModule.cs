using Asp.Versioning;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Eventing;
using AMIS.Framework.Eventing.Outbox;
using AMIS.Modules.Identity.Features.v1.Tokens.RefreshToken;
using AMIS.Modules.Identity.Features.v1.Tokens.TokenGeneration;
using AMIS.Modules.Identity.Features.v1.Users.SelfRegistration;
using AMIS.Framework.Persistence;
using AMIS.Framework.Storage.Local;
using AMIS.Framework.Storage.Services;
using AMIS.Framework.Storage;
using AMIS.Framework.Web.Modules;
using AMIS.Modules.Identity.Authorization;
using AMIS.Modules.Identity.Authorization.Jwt;
using AMIS.Modules.Identity.Contracts.Services;
using AMIS.Modules.Identity.Data;
using AMIS.Modules.Identity.Domain;
using AMIS.Modules.Identity.Features.v1.Roles;
using AMIS.Modules.Identity.Features.v1.Roles.DeleteRole;
using AMIS.Modules.Identity.Features.v1.Roles.GetRoleById;
using AMIS.Modules.Identity.Features.v1.Roles.GetRoles;
using AMIS.Modules.Identity.Features.v1.Roles.GetRoleWithPermissions;
using AMIS.Modules.Identity.Features.v1.Roles.UpdateRolePermissions;
using AMIS.Modules.Identity.Features.v1.Roles.UpsertRole;
using AMIS.Modules.Identity.Features.v1.Users.AssignUserRoles;
using AMIS.Modules.Identity.Features.v1.Users.ChangePassword;
using AMIS.Modules.Identity.Features.v1.Users.ConfirmEmail;
using AMIS.Modules.Identity.Features.v1.Users.DeleteUser;
using AMIS.Modules.Identity.Features.v1.Users.GetUserById;
using AMIS.Modules.Identity.Features.v1.Users.GetUserPermissions;
using AMIS.Modules.Identity.Features.v1.Users.GetUserProfile;
using AMIS.Modules.Identity.Features.v1.Users.GetUserRoles;
using AMIS.Modules.Identity.Features.v1.Users.GetUsers;
using AMIS.Modules.Identity.Features.v1.Users.RegisterUser;
using AMIS.Modules.Identity.Features.v1.Users.SearchUsers;
using AMIS.Modules.Identity.Features.v1.Users.ResetPassword;
using AMIS.Modules.Identity.Features.v1.Users.ToggleUserStatus;
using AMIS.Modules.Identity.Features.v1.Users.UpdateUser;
using AMIS.Modules.Identity.Features.v1.Sessions.GetMySessions;
using AMIS.Modules.Identity.Features.v1.Sessions.RevokeSession;
using AMIS.Modules.Identity.Features.v1.Sessions.RevokeAllSessions;
using AMIS.Modules.Identity.Features.v1.Sessions.GetUserSessions;
using AMIS.Modules.Identity.Features.v1.Sessions.AdminRevokeSession;
using AMIS.Modules.Identity.Features.v1.Sessions.AdminRevokeAllSessions;
using AMIS.Modules.Identity.Features.v1.Groups.CreateGroup;
using AMIS.Modules.Identity.Features.v1.Groups.UpdateGroup;
using AMIS.Modules.Identity.Features.v1.Groups.DeleteGroup;
using AMIS.Modules.Identity.Features.v1.Groups.GetGroups;
using AMIS.Modules.Identity.Features.v1.Groups.GetGroupById;
using AMIS.Modules.Identity.Features.v1.Groups.GetGroupMembers;
using AMIS.Modules.Identity.Features.v1.Groups.AddUsersToGroup;
using AMIS.Modules.Identity.Features.v1.Groups.RemoveUserFromGroup;
using AMIS.Modules.Identity.Features.v1.Users.GetUserGroups;
using AMIS.Modules.Identity.Services;
using Hangfire;
using Hangfire.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Identity;

public class IdentityModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, PathAwareAuthorizationHandler>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<ICurrentUserService>());
        services.AddScoped<ICurrentUserInitializer>(sp => sp.GetRequiredService<ICurrentUserService>());
        services.AddScoped<IRequestContextService, RequestContextService>();
        services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<IRequestContextService>());
        services.AddScoped<ITokenService, TokenService>();

        // User services - focused single-responsibility services
        services.AddTransient<IUserRegistrationService, UserRegistrationService>();
        services.AddTransient<IUserProfileService, UserProfileService>();
        services.AddTransient<IUserStatusService, UserStatusService>();
        services.AddTransient<IUserRoleService, UserRoleService>();
        services.AddTransient<IUserPasswordService, UserPasswordService>();
        services.AddTransient<IUserPermissionService, UserPermissionService>();

        // Facade for backward compatibility
        services.AddTransient<IUserService, UserService>();

        services.AddTransient<IRoleService, RoleService>();
        services.AddHeroStorage(builder.Configuration);
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddHeroDbContext<IdentityDbContext>();
        services.AddEventingCore(builder.Configuration);
        services.AddEventingForDbContext<IdentityDbContext>();
        services.AddIntegrationEventHandlers(typeof(IdentityModule).Assembly);
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<IdentityDbContext>(
                name: "db:identity",
                failureStatus: HealthStatus.Unhealthy);
        services.AddScoped<IDbInitializer, IdentityDbInitializer>();

        // Configure password policy options
        services.Configure<PasswordPolicyOptions>(builder.Configuration.GetSection("PasswordPolicy"));

        // Register password history service
        services.AddScoped<IPasswordHistoryService, PasswordHistoryService>();

        // Register password expiry service
        services.AddScoped<IPasswordExpiryService, PasswordExpiryService>();

        // Register session service and background cleanup
        services.AddScoped<ISessionService, SessionService>();
        services.AddHostedService<SessionCleanupHostedService>();

        // Register group role service for group-derived permissions
        services.AddScoped<IGroupRoleService, GroupRoleService>();

        services.AddIdentity<AmisUser, AmisRole>(options =>
        {
            options.Password.RequiredLength = IdentityModuleConstants.PasswordLength;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.User.RequireUniqueEmail = true;
        })
           .AddEntityFrameworkStores<IdentityDbContext>()
           .AddDefaultTokenProviders();

        //metrics
        services.AddSingleton<IdentityMetrics>();

        services.ConfigureJwtAuth();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/identity")
            .WithTags("Identity")
            .WithApiVersionSet(apiVersionSet);

        // tokens
        group.MapGenerateTokenEndpoint().AllowAnonymous().RequireRateLimiting("auth");
        group.MapRefreshTokenEndpoint().AllowAnonymous().RequireRateLimiting("auth");

        // Optional Hangfire-based dispatcher. Disabled by default to avoid startup races
        // where recurring jobs can execute before module migrations complete.
        // NOTE: Hangfire operations can open a DB connection; guard to avoid startup crash on transient DB timeouts.
        var jobManager = endpoints.ServiceProvider.GetService<IRecurringJobManager>();
        var configuration = endpoints.ServiceProvider.GetService<IConfiguration>();
        var loggerFactory = endpoints.ServiceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger<IdentityModule>();
        var enableIdentityOutboxJob = configuration?.GetValue<bool>("EventingOptions:EnableIdentityHangfireDispatcher") ?? false;

        if (jobManager is not null)
        {
            try
            {
                if (enableIdentityOutboxJob)
                {
                    jobManager.AddOrUpdate(
                        "identity-outbox-dispatcher",
                        Job.FromExpression<OutboxDispatcher>(d => d.DispatchAsync(CancellationToken.None)),
                        Cron.Minutely(),
                        new RecurringJobOptions());
                }
                else
                {
                    jobManager.RemoveIfExists("identity-outbox-dispatcher");
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex,
                    "Skipping identity Hangfire recurring job registration due to storage connectivity issue. API startup will continue.");
            }
        }

        // roles
        group.MapGetRolesEndpoint();
        group.MapGetRoleByIdEndpoint();
        group.MapDeleteRoleEndpoint();
        group.MapGetRolePermissionsEndpoint();
        group.MapUpdateRolePermissionsEndpoint();
        group.MapCreateOrUpdateRoleEndpoint();

        // users
        group.MapAssignUserRolesEndpoint();
        group.MapChangePasswordEndpoint();
        group.MapConfirmEmailEndpoint().RequireRateLimiting("auth");
        group.MapDeleteUserEndpoint();
        group.MapGetUserByIdEndpoint();
        group.MapGetCurrentUserPermissionsEndpoint();
        group.MapGetMeEndpoint();
        group.MapGetUserRolesEndpoint();
        group.MapGetUsersListEndpoint();
        group.MapSearchUsersEndpoint();
        group.MapRegisterUserEndpoint();
        group.MapResetPasswordEndpoint();
        group.MapSelfRegisterUserEndpoint();
        group.MapToggleUserStatusEndpoint();
        group.MapUpdateUserEndpoint();

        // sessions - user endpoints
        group.MapGetMySessionsEndpoint();
        group.MapRevokeSessionEndpoint();
        group.MapRevokeAllSessionsEndpoint();

        // sessions - admin endpoints
        group.MapGetUserSessionsEndpoint();
        group.MapAdminRevokeSessionEndpoint();
        group.MapAdminRevokeAllSessionsEndpoint();

        // groups
        group.MapGetGroupsEndpoint();
        group.MapGetGroupByIdEndpoint();
        group.MapCreateGroupEndpoint();
        group.MapUpdateGroupEndpoint();
        group.MapDeleteGroupEndpoint();
        group.MapGetGroupMembersEndpoint();
        group.MapAddUsersToGroupEndpoint();
        group.MapRemoveUserFromGroupEndpoint();

        // user groups
        group.MapGetUserGroupsEndpoint();
    }
}


