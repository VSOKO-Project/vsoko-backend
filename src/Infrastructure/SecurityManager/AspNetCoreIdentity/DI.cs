using System.Security.Principal;
using Infrastructure.DataManager.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.SecurityManager.AspNetCoreIdentity;

public static class DI
{
    public static IServiceCollection ApplySecurityManager(this IServiceCollection services)
    {
        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddRoles<IdentityRole>();

        services.AddScoped<SecurityService>();

        return services;
    }
}
