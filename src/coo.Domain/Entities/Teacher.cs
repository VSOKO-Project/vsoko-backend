using coo.Domain.Common;

namespace coo.Domain.Entities;

public class Teacher : BaseEntity
{
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public string Patronymic { get; set; } = null!; 

    public List<Workload> WorkloadsRefs { get; init; } = new();
    
}