using Domain.Common;

namespace Domain.Entities;

public class Employee : BaseEntity
{
    public required string RoleId { get; set; }
    public EmployeeRole? RoleRef { get; init; }
}
