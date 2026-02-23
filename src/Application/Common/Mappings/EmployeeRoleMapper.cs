using Application.Common.DTOs;
using Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Application.Common.Mappings;

[Mapper]
public partial class EmployeeRoleMapper
{
    [MapperIgnoreSource(nameof(EmployeeRole.CreatedAtUtc))]
    [MapperIgnoreSource(nameof(EmployeeRole.UpdatedAtUtc))]
    [MapperIgnoreSource(nameof(EmployeeRole.IsDeleted))]
    [MapperIgnoreSource(nameof(EmployeeRole.CreatedById))]
    [MapperIgnoreSource(nameof(EmployeeRole.UpdatedById))]
    [MapperIgnoreSource(nameof(EmployeeRole.EmployeeRefs))]
    public partial EmployeeRoleDto MapSingle(EmployeeRole role);

    public partial IQueryable<EmployeeRoleDto> ProjectToDto(IQueryable<EmployeeRole> q);
}
