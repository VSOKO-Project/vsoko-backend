using coo.Domain.Common;

namespace coo.Domain.Entities;

public class Workload : BaseEntity
{
    public string GroupId { get; set; } = null!;
    public string DisciplineId { get; set; } = null!;
    public string TeacherId { get; set; } = null!;

    public Teacher TeacherRef { get; set; } = null!;
    public Discipline DisciplineRef { get; set; } = null!;
    public StudentGroup GroupRef { get; set; } = null!;
    public List<Feedback> FeedbackRefs { get; init; } = new();
}