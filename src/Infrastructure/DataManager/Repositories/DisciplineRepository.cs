using Application.Common.DTOs;
using Application.Interfaces.DataManager.Repositories;
using Application.Common.Results;
using Domain.Enums;
using Infrastructure.DataManager.Contexts;
using Microsoft.EntityFrameworkCore;
using static Application.Common.Extension.DisciplineQueryExtensions;

namespace Infrastructure.DataManager.Repositories;

public class DisciplineRepository : IDisciplineRepository
{
    private readonly AppDbContext _dbContext;
    public DisciplineRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<PagedResultDto<RatingDto>> GetRating(
    int page,
    string? query,
    int pageSize,
    CancellationToken cancellationToken)
    {
        var baseQuery = _dbContext.Disciplines
            .Include(w => w.WorkloadRefs)
            .ThenInclude(w => w.FeedbackRefs)
            .ThenInclude(w => w.CriteriaFeedbackRefs)
            .ThenInclude(w => w.CriteriaRef)
            .WhereNameOrTeacherContains(query);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .OrderBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new RatingDto
            {
                Id = t.Id,
                Name = t.Name,
                Grade = t.WorkloadRefs
                    .SelectMany(w => w.FeedbackRefs)
                    .SelectMany(f => f.CriteriaFeedbackRefs)
                    .Where(cf => cf.CriteriaRef.Object == CriteriaObject.Discipline)
                    .Average(cf => (float?)cf.CriteriaScore) ?? 0f
            }).ToListAsync(cancellationToken);

        return new PagedResultDto<RatingDto>
        {
            Items = items,
            TotalPages = (int)Math.Ceiling(totalCount / (decimal)pageSize),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}