using coo.Application.Common.Interfaces.SecurityManager;
using coo.Infrastructure.DataManager;
using coo.Infrastructure.SecurityManager.AspNetCoreIdentity;
using coo.Infrastructure.SecurityManager.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace coo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.ApplyDataManager(configuration);

        services.ApplySecurityManager();

        services.ApplyTokenManager(configuration);

        services.AddTransient<ISecurityManager, SecurityService>();

        return services;
    }
}