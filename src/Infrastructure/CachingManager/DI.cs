using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Interfaces.CachingManager;
using StackExchange.Redis;
using Microsoft.AspNetCore.DataProtection;

namespace Infrastructure.CachingManager;

public static class DependencyInjection
{
    public static IServiceCollection AddCaching(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrEmpty(redisConnectionString))
            throw new InvalidOperationException("Redis connection string is missing.");

        var cacheSettings = new CacheSettings();
        configuration.GetSection(CacheSettings.SectionName).Bind(cacheSettings);

        var multiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        services.AddStackExchangeRedisCache(options => 
        {
            options.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(multiplexer);
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

        services.AddDataProtection()
            .SetApplicationName("VSOKO.API")
            .PersistKeysToStackExchangeRedis(multiplexer, "DataProtection-Keys");

        return services;
    }
}