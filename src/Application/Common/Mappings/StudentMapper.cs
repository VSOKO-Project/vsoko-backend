using Application.Common.DTOs;
using Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Application.Common.Mappings;

[Mapper]
public partial class StudentMapper
{
    [UseMapper]
    private readonly StudentGroupMapper _groupMapper = new();

    [MapperIgnoreSource(nameof(Student.CreatedAtUtc))]
    [MapperIgnoreSource(nameof(Student.UpdatedAtUtc))]
    [MapperIgnoreSource(nameof(Student.IsDeleted))]
    [MapperIgnoreSource(nameof(Student.CreatedById))]
    [MapperIgnoreSource(nameof(Student.UpdatedById))]
    [MapperIgnoreSource(nameof(Student.GroupId))]
    [MapperIgnoreSource(nameof(Student.FeedbackRefs))]
    [MapProperty(nameof(Student.GroupRef), nameof(StudentDto.Group))]
    public partial StudentDto MapSingle(Student student);

    public partial IQueryable<StudentDto> ProjectToDto(IQueryable<Student> q);
}
