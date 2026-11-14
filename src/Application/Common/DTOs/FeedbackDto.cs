namespace Application.Common.DTOs;

public class FeedbackDto
{
    public DateTime? CreatedAtUtc { get; set; }
    public double? AverageGrade => CriteriaFeedback is { Count: > 0 } ? CriteriaFeedback.Average(c => c.CriteriaScore) : null;
    public string Id { get; set; } = null!;
    public string? Comment { get; set; }
    public WorkloadDto? Workload { get; set; }
    public StudentDto? Student { get; set; }
    public List<CriteriaFeedbackDto>? CriteriaFeedback { get; set; }
}