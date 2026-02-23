namespace Application.Common.DTOs;

public class FeedbackDto
{
    public string Id { get; set; } = null!;
    public string? Comment { get; set; }
    public WorkloadDto? Workload { get; set; }
    public StudentDto? Student { get; set; }
    public List<CriteriaFeedbackDto>? CriteriaFeedback { get; set; }
}