using Domain.Common;

namespace Domain.Entities;

public class Feedback : BaseEntity
{
    public string StudentId { get; set; } = null!;
    public string Comment { get; set; } = null!;
    public string WorkloadId { get; set; } = null!;

    public Student? StudentRef { get; init; }
    public Workload? WorkloadRef { get; init; }
    public List<CriteriaFeedback>? CriteriaFeedbackRefs { get; init; }
}
