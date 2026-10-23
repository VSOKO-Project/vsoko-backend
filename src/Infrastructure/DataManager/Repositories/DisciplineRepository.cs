using Application.Common.DTOs;
using Application.Common.Exceptions;
using Application.Common.Periods;
using Application.Common.Mappings;
using Application.Common.Results;
using Application.Interfaces.DataManager.Repositories;
using Domain.Entities;
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

    public async Task<PagedResultDto<RatingDto>> GetRatingAsync(
        int page,
        string? query,
        int pageSize,
        PeriodFilter period,
        CancellationToken cancellationToken
    )
    {
        var baseQuery = _dbContext
            .Disciplines.Include(w => w.WorkloadRefs!)
                .ThenInclude(w => w.FeedbackRefs!)
                    .ThenInclude(w => w.CriteriaFeedbackRefs!)
                        .ThenInclude(w => w.CriteriaRef)
            .WhereNameOrTeacherContains(query)
            .WhereHasWorkloadIn(period);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await _mapper.ProjectToRating(baseQuery, period)
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
        PeriodFilter period,
        CancellationToken cancellationToken
    )
    {
        var baseQuery = _dbContext
            .Disciplines.Include(w => w.WorkloadRefs!)
                .ThenInclude(w => w.FeedbackRefs!)
                    .ThenInclude(w => w.CriteriaFeedbackRefs!)
                        .ThenInclude(w => w.CriteriaRef)
            .WhereHasWorkloadIn(period);

        var items = await _mapper.ProjectToRating(baseQuery, period)
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<DisciplineDto> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var discipline = await _dbContext.Disciplines
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Discipline), id);

        return new DisciplineDto
        {
            Id = discipline.Id,
            Name = discipline.Name
        };
    }

    public async Task<PagedResultDto<DisciplineDto>> GetAllAsync(
        int page,
        string query,
        int pageSize,
        PeriodFilter period,
        CancellationToken cancellationToken
    )
    {
        var baseQuery = _dbContext.Disciplines.WhereHasWorkloadIn(period);

        if (!string.IsNullOrWhiteSpace(query))
        {
            baseQuery = baseQuery.Where(d => d.Name.ToLower().Contains(query.ToLower()));
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await _mapper.ProjectToDto(baseQuery
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize))
            .ToListAsync(cancellationToken);

        return new PagedResultDto<DisciplineDto>
        {
            Items = items,
            TotalPages = (int)Math.Ceiling(totalCount / (decimal)pageSize),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<DisciplineDto> RenameAsync(string id, string name, CancellationToken cancellationToken)
    {
        var discipline = await _dbContext.Disciplines.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Discipline), id);

        // Импорт сопоставляет названия без учёта регистра, поэтому и здесь дублем считается любое совпадение.
        var lower = name.ToLower();
        if (await _dbContext.Disciplines.AnyAsync(d => d.Id != id && d.Name.ToLower() == lower, cancellationToken))
            throw new ValidationException($"Дисциплина «{name}» уже есть. Чтобы свести записи, объедините их");

        discipline.Name = name;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DisciplineDto { Id = discipline.Id, Name = discipline.Name };
    }

    public async Task<DisciplineDto> MergeAsync(string sourceId, string targetId, CancellationToken cancellationToken)
    {
        var source = await _dbContext.Disciplines.FirstOrDefaultAsync(d => d.Id == sourceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Discipline), sourceId);
        var target = await _dbContext.Disciplines.FirstOrDefaultAsync(d => d.Id == targetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Discipline), targetId);

        await WorkloadMerger.MergeAsync(
            _dbContext,
            w => w.DisciplineId == sourceId,
            w => w.DisciplineId == targetId,
            w => (w.TeacherId, w.GroupId, w.PeriodId),
            w => w.DisciplineId = targetId,
            cancellationToken);

        source.IsDeleted = true;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DisciplineDto { Id = target.Id, Name = target.Name };
    }
}
