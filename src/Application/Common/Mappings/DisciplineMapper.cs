using Application.Common.DTOs;
using Domain.Entities;
using Domain.Enums;
using Riok.Mapperly.Abstractions;

namespace Application.Common.Mappings;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class DisciplineMapper
{
    [MapperIgnoreSource(nameof(Discipline.CreatedAtUtc))]
    [MapperIgnoreSource(nameof(Discipline.UpdatedAtUtc))]
    [MapperIgnoreSource(nameof(Discipline.IsDeleted))]
    [MapperIgnoreSource(nameof(Discipline.CreatedById))]
    [MapperIgnoreSource(nameof(Discipline.UpdatedById))]
    [MapperIgnoreSource(nameof(Discipline.WorkloadRefs))]
    public partial DisciplineDto MapSingle(Discipline discipline);

    public partial IQueryable<DisciplineDto> ProjectToDto(IQueryable<Discipline> q);

    public IQueryable<RatingDto> ProjectToRating(IQueryable<Discipline> q)
    {
        return q.Select(t => new RatingDto
        {
            Id = t.Id,
            Name = t.Name,
            Grade = t.WorkloadRefs.SelectMany(w => w.FeedbackRefs)
                .SelectMany(f => f.CriteriaFeedbackRefs)
                .Where(cf => cf.CriteriaRef.Object == CriteriaObject.Discipline)
                .Average(cf => (float?)cf.CriteriaScore) ?? 0f
        });
    }
}
