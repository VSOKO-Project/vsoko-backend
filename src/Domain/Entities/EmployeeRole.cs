using Domain.Common;

namespace Domain.Entities;

public class EmployeeRole : BaseEntity
{
    public required string Name { get; set; }

    public List<Employee>? EmployeeRefs { get; set; }
}
