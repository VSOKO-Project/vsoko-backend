using Domain.Common;

namespace Domain.Entities;

public class StudentGroup : BaseEntity
{
    public int Semester { get; set; }
    public string Name { get; set; } = null!;

    public List<Student>? StudentRefs { get; init; }
    public List<Workload>? WorkloadRefs { get; init; }
}