
namespace Presentation.Common;
public class CorsOptions
{
    public const string SectionName = "Cors";
    public string AllowedOrigins { get; set; } = string.Empty;

    public string[] GetOrigins() => 
        AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries);
}