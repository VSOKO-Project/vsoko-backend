using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Application.Interfaces.AIManager;
using System.Net;

namespace Infrastructure.AIManager;

public static class Discipline
{
    public static IServiceCollection ApplyAiManager(this IServiceCollection services, IConfiguration configuration)
    {
        var AiSettings = new AiSettings();
        configuration.GetSection(AiSettings.SectionName).Bind(AiSettings);

        var webProxy = new WebProxy("http://5.180.97.175:3128");

        var handler = new HttpClientHandler
        {
            Proxy = webProxy,
            UseProxy = true
        };

        var proxyHttpClient = new HttpClient(handler);
        var builder = Kernel.CreateBuilder();

        if (string.IsNullOrWhiteSpace(AiSettings.ApiKey))
            throw new InvalidOperationException("AI Api key is missing.");

        builder.AddGoogleAIGeminiChatCompletion(
            modelId: "gemini-2.5-flash",
            apiKey: AiSettings.ApiKey,
            httpClient: proxyHttpClient
        );

        services.AddSingleton(builder.Build());
        services.AddScoped<IFeedbackSummarizer, FeedbackSummarizer>();

        return services;
    }
}