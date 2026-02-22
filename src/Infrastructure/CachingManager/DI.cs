using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Interfaces.CachingManager;

namespace Infrastructure.CachingManager;

public static class DependencyInjection
{
    public static IServiceCollection AddCaching(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrEmpty(redisConnectionString))
        {
            throw new InvalidOperationException("Redis connection string is missing.");
        }

        var cacheSettings = new CacheSettings();
        configuration.GetSection(CacheSettings.SectionName).Bind(cacheSettings);

        services.Configure<CacheSettings>(configuration.GetSection(CacheSettings.SectionName));

        services.AddStackExchangeRedisCache(options => 
        {
            options.Configuration = redisConnectionString;
        });

        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                LocalCacheExpiration = TimeSpan.FromMinutes(cacheSettings.LocalExpirationMinutes),
                Expiration = TimeSpan.FromMinutes(cacheSettings.DistributedExpirationMinutes)
            };
        });

        services.AddSingleton<ICacheService, CacheService>();

        services.AddHealthChecks()
        .AddRedis(
            redisConnectionString: redisConnectionString,
            name: "Redis Cache",
            tags: new[] { "cache", "ready" },
            timeout: TimeSpan.FromSeconds(3)
        );

        return services;
    }
}