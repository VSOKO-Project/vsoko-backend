namespace  Infrastructure.CachingManager;

public class CacheSettings
{
    public const string SectionName = "CacheSettings";

    public int LocalExpirationMinutes { get; set; } = 1;
    public int DistributedExpirationMinutes { get; set; } = 10;
}