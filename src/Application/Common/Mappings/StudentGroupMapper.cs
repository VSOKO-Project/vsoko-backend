using Application.Common.DTOs;
using Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Application.Common.Mappings;

[Mapper]
public partial class StudentGroupMapper
{
    [MapperIgnoreSource(nameof(StudentGroup.CreatedAtUtc))]
    [MapperIgnoreSource(nameof(StudentGroup.UpdatedAtUtc))]
    [MapperIgnoreSource(nameof(StudentGroup.IsDeleted))]
    [MapperIgnoreSource(nameof(StudentGroup.CreatedById))]
    [MapperIgnoreSource(nameof(StudentGroup.UpdatedById))]
    [MapperIgnoreSource(nameof(StudentGroup.StudentRefs))]
    [MapperIgnoreSource(nameof(StudentGroup.WorkloadRefs))]
    public partial StudentGroupDto MapSingle(StudentGroup group);

    public partial IQueryable<StudentGroupDto> ProjectToDto(IQueryable<StudentGroup> q);
}
