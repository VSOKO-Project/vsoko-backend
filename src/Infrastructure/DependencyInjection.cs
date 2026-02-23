using Application.Interfaces.SecurityManager;
using Infrastructure.CachingManager;
using Infrastructure.DataManager;
using Infrastructure.FileManager;
using Infrastructure.AIManager;
using Infrastructure.SecurityManager.AspNetCoreIdentity;
using Infrastructure.SecurityManager.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.ApplyDataManager(configuration);

        services.ApplySecurityManager();

        services.ApplyTokenManager(configuration);

        services.ApplyFileManager();

        services.AddTransient<ISecurityService, SecurityService>();

        services.AddCaching(configuration);

        services.ApplyAiManager(configuration);

        return services;
    }
}
