using Application.Common.Interfaces;
using Presentation.Services;

namespace Infrastructure.SecurityManager.Tokens;

public static class DI
{
    public static IServiceCollection ApplyUserContext(this IServiceCollection services)
    {
        services.AddScoped<IUserContext, UserContext>();

        return services;
    }
}