using Application.Interfaces.CachingManager;

namespace Application.Common.Caching;

/// <summary>
/// Имена преподавателей и дисциплин есть в нагрузке, отзывах и рейтингах:
/// после переименования или объединения сбрасываются все эти выборки.
/// </summary>
public static class CatalogCache
{
    public static async Task InvalidateAsync(ICacheService cacheService, CancellationToken cancellationToken)
    {
        await cacheService.RemoveByTagAsync(CacheKeys.Teacher.ListTag, cancellationToken);
        await cacheService.RemoveByTagAsync(CacheKeys.Discipline.ListTag, cancellationToken);
        await cacheService.RemoveByTagAsync(CacheKeys.Workload.ListTag, cancellationToken);
        await cacheService.RemoveByTagAsync(CacheKeys.Feedback.Tag, cancellationToken);
    }
}
