using Application.Common.DTOs;
using Domain.Entities;

namespace Application.Common.DTOs;

public class FeedbackDto
{
    public int Id { get; set; }
    public string? Comment { get; set; }
    public WorkloadDto? Workload { get; set; }
    public List<CriteriaFeedbackDto>? CriteriaFeedback{ get; set; }
}