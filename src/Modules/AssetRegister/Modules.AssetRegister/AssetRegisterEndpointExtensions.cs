using Microsoft.AspNetCore.Builder;

namespace AMIS.Modules.AssetRegister;

public static class AssetRegisterEndpointExtensions
{
    public static RouteHandlerBuilder WithModuleName<T>(this RouteHandlerBuilder builder)
    {
        var name = typeof(T).Name;
        if (name.EndsWith("Command", System.StringComparison.Ordinal))
            name = name[..^"Command".Length];
        else if (name.EndsWith("Query", System.StringComparison.Ordinal))
            name = name[..^"Query".Length];
        return builder.WithName($"{AssetRegisterModuleConstants.ModuleName}_{name}");
    }
}
