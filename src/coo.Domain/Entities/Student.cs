using coo.Domain.Common;

namespace coo.Domain.Entities;

public class Student : BaseEntity
{
    public string GroupId { get; set; } = null!;
    public StudentGroup? GroupRef { get; init; }
    public List<Feedback>? FeedbackRefs { get; init; }
}