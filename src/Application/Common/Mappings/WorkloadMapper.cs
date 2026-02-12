using Application.Common.DTOs;
using Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Application.Common.Mappings;

[Mapper]
public partial class WorkloadMapper
{
    [MapperIgnoreSource(nameof(Workload.GroupId))]
    [MapperIgnoreSource(nameof(Workload.DisciplineId))]
    [MapperIgnoreSource(nameof(Workload.TeacherId))]
    [MapperIgnoreSource(nameof(Workload.GroupRef))]
    [MapperIgnoreSource(nameof(Workload.FeedbackRefs))]
    [MapperIgnoreSource(nameof(Workload.CreatedAtUtc))]
    [MapperIgnoreSource(nameof(Workload.UpdatedAtUtc))]
    [MapperIgnoreSource(nameof(Workload.IsDeleted))]
    [MapperIgnoreSource(nameof(Workload.CreatedById))]
    [MapperIgnoreSource(nameof(Workload.UpdatedById))]
    [MapperIgnoreTarget(nameof(WorkloadDto.TeacherFullName))]

    [MapProperty("DisciplineRef.Name", nameof(WorkloadDto.Name))]
    [MapProperty(nameof(Workload.TeacherRef), nameof(WorkloadDto.Teacher))]
    [MapProperty("TeacherRef.Surname", nameof(WorkloadDto.TeacherSurname))]
    [MapProperty("TeacherRef.Name", nameof(WorkloadDto.TeacherName))]
    [MapProperty("TeacherRef.Patronymic", nameof(WorkloadDto.TeacherPatronymic))]
    public partial WorkloadDto MapSingle(Workload workload);

    public partial IQueryable<WorkloadDto> WorkloadToDto(IQueryable<Workload> workload);
}