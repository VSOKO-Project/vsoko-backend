using Application.Common.DTOs;
using Application.Common.Mappings;
using Application.Common.Results;
using Application.Interfaces.DataManager.Repositories;
using Domain.Enums;
using Infrastructure.DataManager.Contexts;
using Microsoft.EntityFrameworkCore;
using static Application.Common.Extension.DisciplineQueryExtensions;

namespace Infrastructure.DataManager.Repositories;

public class DisciplineRepository : IDisciplineRepository
{
    private readonly AppDbContext _dbContext;
    private readonly DisciplineMapper _mapper;

    public DisciplineRepository(AppDbContext dbContext, DisciplineMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<PagedResultDto<RatingDto>> GetRating(
        int page,
        string? query,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var baseQuery = _dbContext
            .Disciplines.Include(w => w.WorkloadRefs)
                .ThenInclude(w => w.FeedbackRefs)
                    .ThenInclude(w => w.CriteriaFeedbackRefs)
                        .ThenInclude(w => w.CriteriaRef)
            .WhereNameOrTeacherContains(query);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await _mapper.ProjectToRating(baseQuery)
            .OrderBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<RatingDto>
        {
            Items = items,
            TotalPages = (int)Math.Ceiling(totalCount / (decimal)pageSize),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
