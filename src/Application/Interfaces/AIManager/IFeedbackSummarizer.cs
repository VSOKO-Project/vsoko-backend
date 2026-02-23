namespace Application.Interfaces.AIManager;

public interface IFeedbackSummarizer
{
    Task<string> SummarizeTeacherAsync(string teacherName, List<string> comments, CancellationToken ct);
    Task<string> SummarizeDisciplineAsync(string disciplineName, List<string> comments, CancellationToken ct);
}