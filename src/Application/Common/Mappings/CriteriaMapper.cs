using Application.Common.DTOs;
using Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Application.Common.Mappings;

[Mapper]
public partial class CriteriaMapper
{
    [MapperIgnoreSource(nameof(Criteria.CreatedAtUtc))]
    [MapperIgnoreSource(nameof(Criteria.UpdatedAtUtc))]
    [MapperIgnoreSource(nameof(Criteria.IsDeleted))]
    [MapperIgnoreSource(nameof(Criteria.CreatedById))]
    [MapperIgnoreSource(nameof(Criteria.UpdatedById))]
    [MapperIgnoreSource(nameof(Criteria.CriteriaFeedbackRefs))]
    public partial CriteriaDto MapSingle(Criteria criteria);
    
    public partial IQueryable<CriteriaDto> ProjectToDto(IQueryable<Criteria> q);
}
