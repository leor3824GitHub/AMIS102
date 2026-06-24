using Asp.Versioning;
using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Web.Modules;
using AMIS.Framework.Web.Realtime;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Features.v1.Channels.CreateChannel;
using AMIS.Modules.Chat.Features.v1.Channels.FindOrCreateDm;
using AMIS.Modules.Chat.Features.v1.Channels.ListMyChannels;
using AMIS.Modules.Chat.Features.v1.Messages.ListChannelMessages;
using AMIS.Modules.Chat.Features.v1.Messages.SendMessage;
using AMIS.Modules.Chat.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AMIS.Modules.Chat;

public class ChatModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;

        // Register module permissions so Identity role seeding can assign them (ships dark — pilot role only).
        PermissionConstants.Register(ChatModuleConstants.Permissions);

        services.AddHeroDbContext<ChatDbContext>();
        services.AddScoped<IDbInitializer, ChatDbInitializer>();

        // Hub adapters consumed by AppHub (BuildingBlocks/Web/Realtime). Scoped — they query ChatDbContext.
        // Realtime is unbootable for hub connections unless these are registered; the Chat module is their owner.
        services.AddScoped<IChannelMembershipChecker, ChannelMembershipChecker>();
        services.AddScoped<IUserChannelLookup, UserChannelLookup>();

        // Mention resolution over the identity directory (IUserService).
        services.AddScoped<IMentionResolver, MentionResolver>();

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
            .MapGroup("api/v{version:apiVersion}/chat")
            .WithTags("Chat")
            .WithApiVersionSet(apiVersionSet)
            .RequireAuthorization();

        // Channels
        CreateChannelEndpoint.Map(moduleGroup);
        FindOrCreateDmEndpoint.Map(moduleGroup);
        ListMyChannelsEndpoint.Map(moduleGroup);

        // Messages
        SendMessageEndpoint.Map(moduleGroup);
        ListChannelMessagesEndpoint.Map(moduleGroup);
    }
}
