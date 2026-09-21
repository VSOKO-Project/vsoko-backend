namespace  Infrastructure.AIManager;

public class AiSettings
{
    public const string SectionName = "AI";
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "google/gemini-2.5-flash";
    public string Endpoint { get; set; } = "https://openrouter.ai/api/v1";
    public string? ProxyUrl { get; set; }
}