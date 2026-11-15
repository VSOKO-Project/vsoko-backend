namespace Domain.Entities;

// No author information is sent to the analysis model.
public class FeedbackAnalysis
{
    public string FeedbackId { get; set; } = null!;
    public string SourceComment { get; set; } = "";
    public string Version { get; set; } = "";
    public string Model { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string FindingsJson { get; set; } = "[]";
    public int Attempts { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime RetryAfterUtc { get; set; }
    public Feedback Feedback { get; set; } = null!;
}

public class FeedbackCategory
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string NormalizedName { get; set; } = null!;
    public string Description { get; set; } = null!;
    public bool RequiresAttention { get; set; }
}

public class FeedbackSignalReview
{
    public string FeedbackId { get; set; } = null!;
    public string SignalKey { get; set; } = null!;
    public string Status { get; set; } = "Pending";
    public string Note { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public Feedback Feedback { get; set; } = null!;
}
