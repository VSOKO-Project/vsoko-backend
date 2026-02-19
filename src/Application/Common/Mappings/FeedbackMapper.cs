using Application.Common.DTOs;
using Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Application.Common.Mappings;

[Mapper]
public partial class FeedbackMapper
{
    [MapperIgnoreSource(nameof(Feedback.CreatedAtUtc))]
    [MapperIgnoreSource(nameof(Feedback.UpdatedAtUtc))]
    [MapperIgnoreSource(nameof(Feedback.IsDeleted))]
    [MapperIgnoreSource(nameof(Feedback.CreatedById))]
    [MapperIgnoreSource(nameof(Feedback.UpdatedById))]
    [MapperIgnoreSource(nameof(Feedback.StudentId))]
    [MapperIgnoreSource(nameof(Feedback.WorkloadId))]
    [MapperIgnoreSource(nameof(Feedback.StudentRef))]
    [MapperIgnoreSource(nameof(Feedback.WorkloadRef))]
    [MapProperty(nameof(Feedback.CriteriaFeedbackRefs), nameof(FeedbackDto.CriteriaFeedback))]
    public partial FeedbackDto MapSingle(Feedback feedback);

    public partial IQueryable<FeedbackDto> ProjectToDto(IQueryable<Feedback> q);
}
