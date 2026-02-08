using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using coo.Infrastructure.DataManager.Contexts;
using System.Security.Principal;

namespace coo.Infrastructure.SecurityManager.AspNetCoreIdentity;

public static class DI
{
    public static IServiceCollection ApplySecurityManager(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddRoles<IdentityRole>();

        services.AddScoped<SecurityService>();

        return services;
    } 
}