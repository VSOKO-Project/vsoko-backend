using Application.Common.DTOs;
using Domain.Entities;
using Domain.Enums;
using Riok.Mapperly.Abstractions;

namespace Application.Common.Mappings;

[Mapper]
public partial class DisciplineMapper
{
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
