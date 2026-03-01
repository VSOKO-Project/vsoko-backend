using Application.Interfaces.AIManager;
using Microsoft.SemanticKernel;

namespace Infrastructure.AIManager;

public class FeedbackSummarizer : IFeedbackSummarizer
{
    private readonly Kernel _kernel;

    public FeedbackSummarizer(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> SummarizeTeacherAsync(string teacherName, List<string> comments, CancellationToken ct)
    {
        var promptPath = Path.Combine(AppContext.BaseDirectory, "AIManager", "Prompts", "TeacherSummary.txt");
        var promptTemplate = await File.ReadAllTextAsync(promptPath, ct);

        var arguments = new KernelArguments
        {
            ["name"] = teacherName,
            ["comments"] = string.Join("\n- ", comments.Take(50))
        };

        return await InvokeSafelyAsync(promptTemplate, arguments, ct);
    }

    public async Task<string> SummarizeDisciplineAsync(string disciplineName, List<string> comments, CancellationToken ct)
    {
        var promptPath = Path.Combine(AppContext.BaseDirectory, "AIManager", "Prompts", "DisciplineSummary.txt");
        var promptTemplate = await File.ReadAllTextAsync(promptPath, ct);

        var arguments = new KernelArguments
        {
            ["name"] = disciplineName,
            ["comments"] = string.Join("\n- ", comments.Take(50))
        };

        return await InvokeSafelyAsync(promptTemplate, arguments, ct);
    }

    private async Task<string> InvokeSafelyAsync(string prompt, KernelArguments arguments, CancellationToken ct)
    {
        try
        {
            var result = await _kernel.InvokePromptAsync(prompt, arguments, cancellationToken: ct);
            
            if (result is null || string.IsNullOrWhiteSpace(result.ToString()))
                return "Empty message from AI.";

            return result.ToString();
        }
        catch (HttpOperationException ex)
        {
            var googleErrorMessage = ex.ResponseContent; 
            
            return $"API Error: {ex.StatusCode}. Details: {googleErrorMessage}";
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }
}