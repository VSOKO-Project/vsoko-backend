namespace Application.Common.DTOs;

public record FeedbackCategoryDto(string Id, string Name, string Description, bool RequiresAttention);
public record FeedbackEvidenceDto(string FeedbackId, string Quote, string Sentiment, string Basis);
public record FeedbackTopicDto(string CategoryId, string Name, int Count, int PositiveCount,
    int NegativeCount, int NeutralCount, List<FeedbackEvidenceDto> Evidence);
public record FeedbackSignalDto(string FeedbackId, string SignalKey, string CategoryName,
    string Quote, string Basis, string Status, string Note);
public record ProposedFeedbackCategoryDto(string Name, string Description, bool RequiresAttention, List<FeedbackEvidenceDto> Evidence);

public class FeedbackReportDto
{
    public string Name { get; set; } = "";
    public int TotalFeedbackCount { get; set; }
    public int CommentCount { get; set; }
    public int ProcessedCount { get; set; }
    public int FailedCount { get; set; }
    public int PendingCount => CommentCount - ProcessedCount - FailedCount;
    public DateTime GeneratedAtUtc { get; set; }
    public string Version { get; set; } = "";
    public string Summary { get; set; } = "";
    public List<FeedbackTopicDto> Topics { get; set; } = [];
    public List<FeedbackSignalDto> Signals { get; set; } = [];
    public List<ProposedFeedbackCategoryDto> ProposedCategories { get; set; } = [];
}

public record AddFeedbackCategoryRequest(string Name, string Description, bool RequiresAttention = false);
public record ReviewFeedbackSignalRequest(string Status, string Note);
