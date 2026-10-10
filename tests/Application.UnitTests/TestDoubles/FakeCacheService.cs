using Application.Interfaces.CachingManager;

namespace Application.UnitTests.TestDoubles;

/// <summary>Кэш без хранения: всегда вызывает фабрику и запоминает ключи, теги (через запятую) и инвалидации.</summary>
internal sealed class FakeCacheService : ICacheService
{
    public List<(string Key, string Tags)> Requests { get; } = [];
    public List<string> RemovedKeys { get; } = [];
    public List<string> RemovedTags { get; } = [];

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T?>> factory,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default) where T : class
    {
        Requests.Add((key, string.Join(",", tags ?? [])));
        return await factory(cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        RemovedKeys.Add(key);
        return Task.CompletedTask;
    }

    public Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        RemovedTags.Add(tag);
        return Task.CompletedTask;
    }
}

internal sealed class FakeUserContext : Application.Common.Interfaces.IUserContext
{
    public string? UserName { get; init; }
    public string? UserId { get; init; }
    public string? Role { get; init; }
    public string? StudentGroup { get; init; }

    public static FakeUserContext Student(string userId = "s1", string group = "g1") =>
        new() { UserId = userId, Role = "student", StudentGroup = group };

    public static FakeUserContext Admin(string userId = "a1") =>
        new() { UserId = userId, Role = "admin" };
}
