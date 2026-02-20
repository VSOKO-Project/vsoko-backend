using Domain.Common;

namespace Domain.Entities;

public class Workload : BaseEntity
{
    public string GroupId { get; set; } = null!;
    public string DisciplineId { get; set; } = null!;
    public string TeacherId { get; set; } = null!;

    public Teacher? TeacherRef { get; set; }
    public Discipline? DisciplineRef { get; set; }
    public StudentGroup? GroupRef { get; set; }
    public List<Feedback>? FeedbackRefs { get; init; }
}
