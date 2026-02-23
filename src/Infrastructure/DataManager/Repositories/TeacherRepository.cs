using Application.Common.DTOs;
using Application.Common.Mappings;
using Application.Common.Results;
using Application.Interfaces.DataManager.Repositories;
using Domain.Enums;
using Infrastructure.DataManager.Contexts;
using Microsoft.EntityFrameworkCore;
using static Application.Common.Extension.TeacherQueryExtensions;

namespace Infrastructure.DataManager.Repositories;

public class TeacherRepository : ITeacherRepository
{
    private readonly AppDbContext _dbContext;
    private readonly TeacherMapper _mapper;

    public TeacherRepository(AppDbContext dbContext, TeacherMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<PagedResultDto<RatingDto>> GetRatingAsync(
        int page,
        string? query,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = _dbContext
            .Teachers.Include(w => w.WorkloadsRefs)
                .ThenInclude(w => w.FeedbackRefs)
                    .ThenInclude(w => w.CriteriaFeedbackRefs)
                        .ThenInclude(w => w.CriteriaRef)
            .WhereNameContains(query);

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

    public async Task<List<RatingDto>> GetAllRatingAsync(
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = _dbContext
            .Teachers.Include(w => w.WorkloadsRefs)
                .ThenInclude(w => w.FeedbackRefs)
                    .ThenInclude(w => w.CriteriaFeedbackRefs)
                        .ThenInclude(w => w.CriteriaRef);

        var items = await _mapper.ProjectToRating(baseQuery)
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<TeacherDto> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var teacher = await _dbContext.Teachers
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Teacher with id '{id}' not found.");

        return new TeacherDto
        {
            Id = teacher.Id,
            Name = teacher.Name,
            Surname = teacher.Surname,
            Patronymic = teacher.Patronymic
        };
    }
}
