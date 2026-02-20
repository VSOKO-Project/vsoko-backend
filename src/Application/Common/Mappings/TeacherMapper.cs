using Application.Common.DTOs;
using Domain.Entities;
using Domain.Enums;
using Riok.Mapperly.Abstractions;

namespace Application.Common.Mappings;

[Mapper]
public partial class TeacherMapper
{
    [MapperIgnoreSource(nameof(Teacher.CreatedAtUtc))]
    [MapperIgnoreSource(nameof(Teacher.UpdatedAtUtc))]
    [MapperIgnoreSource(nameof(Teacher.IsDeleted))]
    [MapperIgnoreSource(nameof(Teacher.CreatedById))]
    [MapperIgnoreSource(nameof(Teacher.UpdatedById))]
    [MapperIgnoreSource(nameof(Teacher.WorkloadsRefs))]
    [MapperIgnoreSource(nameof(Teacher.Name))]
    [MapperIgnoreSource(nameof(Teacher.Patronymic))]
    [MapProperty(nameof(Teacher.Surname), nameof(RatingDto.Name))]
    public partial RatingDto MapToRating(Teacher teacher);

    public IQueryable<RatingDto> ProjectToRating(IQueryable<Teacher> q)
    {
        return q.Select(t => new RatingDto
        {
            Id = t.Id,
            Name = (t.Surname + " " + t.Name + " " + t.Patronymic).Trim(),
            Grade = t.WorkloadsRefs.SelectMany(w => w.FeedbackRefs)
                .SelectMany(f => f.CriteriaFeedbackRefs)
                .Where(cf => cf.CriteriaRef.Object == CriteriaObject.Teacher)
                .Average(cf => (float?)cf.CriteriaScore) ?? 0f
        });
    }
}
