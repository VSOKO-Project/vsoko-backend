namespace Infrastructure.SecurityManager.Tokens;

public class TokenSettings
{
    public const string SectionName = "Jwt";
    public string SecretKey { get; init; } = null!;
    public string Audience { get; init; } = null!;
    public string Issuer { get; init; } = null!;
    public int ExpireInMinute { get; init; }
    public int RefreshExpireInDays { get; init; }
}
