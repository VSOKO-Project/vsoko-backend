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
        PeriodFilter period,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = _dbContext
            .Teachers.Include(w => w.WorkloadsRefs!)
                .ThenInclude(w => w.FeedbackRefs!)
                    .ThenInclude(w => w.CriteriaFeedbackRefs!)
                        .ThenInclude(w => w.CriteriaRef)
            .WhereNameContains(query)
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
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = _dbContext
            .Teachers.Include(w => w.WorkloadsRefs!)
                .ThenInclude(w => w.FeedbackRefs!)
                    .ThenInclude(w => w.CriteriaFeedbackRefs!)
                        .ThenInclude(w => w.CriteriaRef)
            .WhereHasWorkloadIn(period);

        var items = await _mapper.ProjectToRating(baseQuery, period)
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<TeacherDto> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var teacher = await _dbContext.Teachers
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Teacher), id);

        return new TeacherDto
        {
            Id = teacher.Id,
            Name = teacher.Name,
            Surname = teacher.Surname,
            Patronymic = teacher.Patronymic
        };
    }

    public async Task<PagedResultDto<TeacherDto>> GetAllAsync(
        int page,
        string query,
        int pageSize,
        PeriodFilter period,
        CancellationToken cancellationToken
    )
    {
        var baseQuery = _dbContext.Teachers.WhereHasWorkloadIn(period);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.ToLower();
            baseQuery = baseQuery.Where(t =>
                t.Name.ToLower().Contains(q) ||
                t.Surname.ToLower().Contains(q) ||
                t.Patronymic.ToLower().Contains(q)
            );
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await _mapper.ProjectToDto(baseQuery
            .OrderBy(t => t.Surname)
            .ThenBy(t => t.Name)
            .ThenBy(t => t.Patronymic)
            .Skip((page - 1) * pageSize)
            .Take(pageSize))
            .ToListAsync(cancellationToken);

        return new PagedResultDto<TeacherDto>
        {
            Items = items,
            TotalPages = (int)Math.Ceiling(totalCount / (decimal)pageSize),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<TeacherDto> RenameAsync(
        string id,
        string surname,
        string name,
        string patronymic,
        CancellationToken cancellationToken
    )
    {
        var teacher = await _dbContext.Teachers.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Teacher), id);

        // Импорт сопоставляет ФИО без учёта регистра, поэтому и здесь дублем считается любое совпадение.
        var (s, n, p) = (surname.ToLower(), name.ToLower(), patronymic.ToLower());
        var taken = await _dbContext.Teachers.AnyAsync(t =>
            t.Id != id && t.Surname.ToLower() == s && t.Name.ToLower() == n && t.Patronymic.ToLower() == p,
            cancellationToken);
        if (taken)
            throw new ValidationException($"Преподаватель «{surname} {name} {patronymic}» уже есть. Чтобы свести записи, объедините их");

        teacher.Surname = surname;
        teacher.Name = name;
        teacher.Patronymic = patronymic;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(teacher);
    }

    public async Task<TeacherDto> MergeAsync(string sourceId, string targetId, CancellationToken cancellationToken)
    {
        var source = await _dbContext.Teachers.FirstOrDefaultAsync(t => t.Id == sourceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Teacher), sourceId);
        var target = await _dbContext.Teachers.FirstOrDefaultAsync(t => t.Id == targetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Teacher), targetId);

        await WorkloadMerger.MergeAsync(
            _dbContext,
            w => w.TeacherId == sourceId,
            w => w.TeacherId == targetId,
            w => (w.DisciplineId, w.GroupId, w.PeriodId),
            w => w.TeacherId = targetId,
            cancellationToken);

        source.IsDeleted = true;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(target);
    }

    private static TeacherDto ToDto(Teacher teacher) =>
        new() { Id = teacher.Id, Surname = teacher.Surname, Name = teacher.Name, Patronymic = teacher.Patronymic };
}
