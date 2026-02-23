using Application.Common.DTOs;
using Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Application.Common.Mappings;

[Mapper]
public partial class EmployeeMapper
{
    [UseMapper]
    private readonly EmployeeRoleMapper _roleMapper = new();

    [MapperIgnoreSource(nameof(Employee.CreatedAtUtc))]
    [MapperIgnoreSource(nameof(Employee.UpdatedAtUtc))]
    [MapperIgnoreSource(nameof(Employee.IsDeleted))]
    [MapperIgnoreSource(nameof(Employee.CreatedById))]
    [MapperIgnoreSource(nameof(Employee.UpdatedById))]
    [MapperIgnoreSource(nameof(Employee.RoleId))]
    [MapProperty(nameof(Employee.RoleRef), nameof(EmployeeDto.Role))]
    public partial EmployeeDto MapSingle(Employee employee);

    public partial IQueryable<EmployeeDto> ProjectToDto(IQueryable<Employee> q);
}
