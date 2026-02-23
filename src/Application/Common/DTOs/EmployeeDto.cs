namespace Application.Common.DTOs;

public class EmployeeDto
{
    public string Id { get; set; } = null!;
    public EmployeeRoleDto? Role { get; set; }
}
