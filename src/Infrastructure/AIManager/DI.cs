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

        if (string.IsNullOrWhiteSpace(AiSettings.ApiKey))
            throw new InvalidOperationException("AI Api key is missing.");

        var handler = new HttpClientHandler();

        if (!string.IsNullOrWhiteSpace(AiSettings.ProxyUrl))
        {
            handler.Proxy = new WebProxy(AiSettings.ProxyUrl);
            handler.UseProxy = true;
        }

        var proxyHttpClient = new HttpClient(handler);
        var builder = Kernel.CreateBuilder();

        builder.AddOpenAIChatCompletion(
            modelId: AiSettings.Model,
            endpoint: new Uri(AiSettings.Endpoint),
            apiKey: AiSettings.ApiKey,
            httpClient: proxyHttpClient
        );

        services.AddSingleton(builder.Build());
        services.AddScoped<IFeedbackSummarizer, FeedbackSummarizer>();

        return services;
    }
}