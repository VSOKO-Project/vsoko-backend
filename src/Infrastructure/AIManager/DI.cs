using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Application.Interfaces.AIManager;

namespace Infrastructure.AIManager;

public static class Discipline
{
    public static IServiceCollection ApplyAiManager(this IServiceCollection services, IConfiguration configuration)
    {
        var AiSettings = new AiSettings();
        configuration.GetSection(AiSettings.SectionName).Bind(AiSettings);
        var builder = Kernel.CreateBuilder();

        if (string.IsNullOrWhiteSpace(AiSettings.ApiKey))
            throw new InvalidOperationException("AI Api key is missing.");

        builder.AddGoogleAIGeminiChatCompletion(
            modelId: "gemini-2.5-flash",
            apiKey: AiSettings.ApiKey
        );

        services.AddSingleton(builder.Build());
        services.AddScoped<IFeedbackSummarizer, FeedbackSummarizer>();

        return services;
    }
}