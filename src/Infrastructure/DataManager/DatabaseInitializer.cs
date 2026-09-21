using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Infrastructure.DataManager.Contexts;
using Microsoft.AspNetCore.Identity;
using Infrastructure.DataManager.Seeders;

using Infrastructure.SecurityManager.AspNetCoreIdentity;

namespace Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var environment = services.GetRequiredService<IHostEnvironment>();
            if (context.Database.IsRelational())
            {
                await context.Database.MigrateAsync();
            }

            if (!environment.IsProduction())
            {
                await DataSeeder.SeedAsync(context, userManager, roleManager);
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<AppDbContext>>();
            logger.LogError(ex, "Произошла ошибка при инициализации базы данных.");
            throw;
        }
    }
}