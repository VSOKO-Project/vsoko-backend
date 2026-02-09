using Domain.Common;

namespace Domain.Entities;

public class CriteriaFeedback : BaseEntity
{
    public string CriteriaId { get; set; } = null!;
    public string FeedbackId { get; set; } = null!;
    public int CriteriaScore { get; set; }

    public Criteria CriteriaRef { get; init; } = null!;
    public Feedback? FeedbackRef { get; init; }
}
