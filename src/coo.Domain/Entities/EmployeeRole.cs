using coo.Domain.Common;

namespace coo.Domain.Entities;

public class EmployeeRole : BaseEntity
{
    public string Name { get; set; } = null!;

    public List<Employee>? EmployeeRefs { get; init; }
}