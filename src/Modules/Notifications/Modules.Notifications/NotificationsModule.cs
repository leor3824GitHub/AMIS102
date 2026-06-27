using Asp.Versioning;
using AMIS.Framework.Eventing.Abstractions;
using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Web.Modules;
using AMIS.Modules.Chat.Contracts.Events;
using AMIS.Modules.Notifications.Contracts.Events;
using AMIS.Modules.Notifications.Data;
using AMIS.Modules.Notifications.Features.v1.DismissNotification;
using AMIS.Modules.Notifications.Features.v1.GetUnreadCount;
using AMIS.Modules.Notifications.Features.v1.ListMyNotifications;
using AMIS.Modules.Notifications.Features.v1.MarkAllRead;
using AMIS.Modules.Notifications.Features.v1.MarkRead;
using AMIS.Modules.Notifications.Integration;
using AMIS.Modules.Notifications.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AMIS.Modules.Notifications;

public class NotificationsModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;

        // Register the module permission so Identity role seeding can assign it (IsBasic → every user).
        PermissionConstants.Register(NotificationsModuleConstants.Permissions);

        services.AddHeroDbContext<NotificationsDbContext>();
        services.AddScoped<IDbInitializer, NotificationsDbInitializer>();

        services.AddScoped<INotificationWriter, NotificationWriter>();

        // Integration consumers. Resolved by the in-memory event bus per published event type.
        services.AddScoped<IIntegrationEventHandler<NotificationRequestedIntegrationEvent>, NotificationRequestedConsumer>();
        services.AddScoped<IIntegrationEventHandler<MentionedInChannelIntegrationEvent>, MentionedInChannelConsumer>();

        // FluentValidation validators are auto-discovered.
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var moduleGroup = endpoints
            .MapGroup("api/v{version:apiVersion}/notifications")
            .WithTags("Notifications")
            .WithApiVersionSet(apiVersionSet)
            .RequireAuthorization();

        ListMyNotificationsEndpoint.Map(moduleGroup);
        GetUnreadCountEndpoint.Map(moduleGroup);
        MarkNotificationReadEndpoint.Map(moduleGroup);
        MarkAllNotificationsReadEndpoint.Map(moduleGroup);
        DismissNotificationEndpoint.Map(moduleGroup);
    }
}
