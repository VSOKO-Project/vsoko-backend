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
    [MapperIgnoreSource(nameof(Workload.FeedbackRefs))]
    [MapperIgnoreSource(nameof(Workload.CreatedAtUtc))]
    [MapperIgnoreSource(nameof(Workload.UpdatedAtUtc))]
    [MapperIgnoreSource(nameof(Workload.IsDeleted))]
    [MapperIgnoreSource(nameof(Workload.CreatedById))]
    [MapperIgnoreSource(nameof(Workload.UpdatedById))]

    [MapProperty(nameof(Workload.TeacherRef), nameof(WorkloadDto.Teacher))]
    [MapProperty(nameof(Workload.DisciplineRef), nameof(WorkloadDto.Discipline))]
    [MapProperty(nameof(Workload.GroupRef), nameof(WorkloadDto.Group))]
    public partial WorkloadDto MapSingle(Workload workload);

    public partial IQueryable<WorkloadDto> WorkloadToDto(IQueryable<Workload> workload);
}