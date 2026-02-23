using Application.Common.DTOs;
using Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Application.Common.Mappings;

[Mapper]
public partial class FeedbackMapper
{
    [UseMapper]
    private readonly WorkloadMapper _workloadMapper = new();

    [UseMapper]
    private readonly StudentMapper _studentMapper = new();

    [MapperIgnoreSource(nameof(Feedback.CreatedAtUtc))]
    [MapperIgnoreSource(nameof(Feedback.UpdatedAtUtc))]
    [MapperIgnoreSource(nameof(Feedback.IsDeleted))]
    [MapperIgnoreSource(nameof(Feedback.CreatedById))]
    [MapperIgnoreSource(nameof(Feedback.UpdatedById))]
    [MapperIgnoreSource(nameof(Feedback.StudentId))]
    [MapperIgnoreSource(nameof(Feedback.WorkloadId))]
    [MapProperty(nameof(Feedback.CriteriaFeedbackRefs), nameof(FeedbackDto.CriteriaFeedback))]
    [MapProperty(nameof(Feedback.WorkloadRef), nameof(FeedbackDto.Workload))]
    [MapProperty(nameof(Feedback.StudentRef), nameof(FeedbackDto.Student))]
    public partial FeedbackDto MapSingle(Feedback feedback);

    public partial IQueryable<FeedbackDto> ProjectToDto(IQueryable<Feedback> q);

    [MapProperty(nameof(CriteriaFeedback.CriteriaRef), nameof(CriteriaFeedbackDto.Criteria))]
    public partial CriteriaFeedbackDto MapCriteriaItem(CriteriaFeedback item);
}
