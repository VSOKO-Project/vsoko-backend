using Application.Common.DTOs;
using Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Application.Common.Mappings;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class AcademicPeriodMapper
{
    [MapperIgnoreSource(nameof(AcademicPeriod.CreatedAtUtc))]
    [MapperIgnoreSource(nameof(AcademicPeriod.UpdatedAtUtc))]
    [MapperIgnoreSource(nameof(AcademicPeriod.IsDeleted))]
    [MapperIgnoreSource(nameof(AcademicPeriod.CreatedById))]
    [MapperIgnoreSource(nameof(AcademicPeriod.UpdatedById))]
    [MapperIgnoreSource(nameof(AcademicPeriod.WorkloadRefs))]
    [MapperIgnoreTarget(nameof(PeriodDto.Title))]
    public partial PeriodDto MapSingle(AcademicPeriod period);

    public partial IQueryable<PeriodDto> ProjectToDto(IQueryable<AcademicPeriod> q);

    public IQueryable<PeriodListItemDto> ProjectToListItem(IQueryable<AcademicPeriod> q)
    {
        return q.Select(p => new PeriodListItemDto
        {
            Id = p.Id,
            StartYear = p.StartYear,
            Term = p.Term,
            IsFeedbackOpen = p.IsFeedbackOpen,
            WorkloadCount = p.WorkloadRefs!.Count(),
            FeedbackCount = p.WorkloadRefs!.SelectMany(w => w.FeedbackRefs!).Count(),
        });
    }
}
