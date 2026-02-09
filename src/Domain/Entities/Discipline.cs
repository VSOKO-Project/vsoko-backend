using Domain.Common;
namespace Domain.Entities;

public class Discipline : BaseEntity
{
    public string Name { get; set; } = null!;

    public List<Workload> WorkloadRefs { get; init; } = new();
}