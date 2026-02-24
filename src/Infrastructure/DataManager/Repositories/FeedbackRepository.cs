using Application.Common.DTOs;
using Application.Common.Exceptions;
using Application.Common.Mappings;
using Application.Common.Results;
using Application.Common.Specification.FeedbackSpecification;
using Application.Features.FeedbackFeatures.Command;
using Application.Interfaces.DataManager.Repositories;
using Domain.Entities;
using Infrastructure.DataManager.Contexts;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.VisualBasic.FileIO;

namespace Infrastructure.DataManager.Repositories;

public class FeedbackRepository : IFeedbackRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IFeedbackAccessService _accessService;
    private readonly FeedbackMapper _mapper;

    public FeedbackRepository(AppDbContext dbContext, FeedbackMapper mapper, IFeedbackAccessService accessService)
    {
        _dbContext = dbContext;
        _accessService = accessService;
        _mapper = mapper;
    }

    public async Task<FeedbackDto> PostFeedback(
        List<Grades> grades,
        string? comment,
        string workloadId,
        string? studentId,
        CancellationToken cancellationToken
    )
    {
        var feedback = new Feedback
        {
            Comment = comment ?? "",
            WorkloadId = workloadId,
            StudentId = studentId ?? throw new UnauthorizationException("non Id in claims!"),
        };

        await _dbContext.AddAsync(feedback, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var gradeList = grades.Select(w => new CriteriaFeedback
        {
            CriteriaId = w.CriteriaId ?? throw new ValidationException("Invalid Criteria Id"),
            CriteriaScore = w.Grade,
            FeedbackId = feedback.Id,
        }).ToList();

        await _dbContext.AddRangeAsync(gradeList, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var query = _dbContext.Feedbacks
            .Include(f => f.CriteriaFeedbackRefs)
                .ThenInclude(cf => cf.CriteriaRef)
            .Include(f => f.WorkloadRef)
                .ThenInclude(w => w.DisciplineRef)
            .Include(f => f.WorkloadRef)
                .ThenInclude(w => w.TeacherRef)
            .Where(f => f.StudentId == studentId)
            .Where(w => w.Id == feedback.Id);

        return (await _mapper.ProjectToDto(query).FirstOrDefaultAsync(cancellationToken))!;
    }

    public async Task<PagedResultDto<FeedbackDto>> GetPagedFeedbacks(
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var spec = _accessService.GetSpecification();

        var baseQuery = spec.Apply(_dbContext.Feedbacks
            .Include(f => f.CriteriaFeedbackRefs)
                .ThenInclude(cf => cf.CriteriaRef)
            .Include(f => f.WorkloadRef)
                .ThenInclude(w => w.DisciplineRef)
            .Include(f => f.WorkloadRef)
                .ThenInclude(w => w.GroupRef)
            .Include(f => f.WorkloadRef)
                .ThenInclude(w => w.TeacherRef));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await _mapper.ProjectToDto(baseQuery)
            .OrderBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<FeedbackDto>
        {
            Items = items,
            TotalPages = (int)Math.Ceiling(totalCount / (decimal)pageSize),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<FeedbackDto> GetFeedbackById(
        string id,
        CancellationToken cancellationToken
    )
    {
        var spec = _accessService.GetSpecification();

        var query = spec.Apply(_dbContext.Feedbacks
            .Include(f => f.CriteriaFeedbackRefs).ThenInclude(cf => cf.CriteriaRef)
            .Include(f => f.WorkloadRef).ThenInclude(w => w.DisciplineRef)
            .Include(f => f.WorkloadRef).ThenInclude(w => w.GroupRef)
            .Include(f => f.WorkloadRef).ThenInclude(w => w.TeacherRef));

        var feedback = await _mapper.ProjectToDto(query).FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (feedback is null)
            throw new NotFoundException(nameof(Feedback), id);

        return feedback;
    }

    public async Task<FeedbackDto> PutFeedback(
        string id,
        string? comment,
        List<Grades>? grades,
        CancellationToken cancellationToken
    )
    {
        var spec = _accessService.GetSpecification();

        var feedback = await spec.Apply(_dbContext.Feedbacks
            .Include(f => f.CriteriaFeedbackRefs))
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (feedback is null)
            throw new NotFoundException(nameof(Feedback), id);

        if (comment is not null)
            feedback.Comment = comment;

        if (grades is not null)
        {
            _dbContext.RemoveRange(feedback.CriteriaFeedbackRefs!);

            var newGrades = grades.Select(w => new CriteriaFeedback
            {
                CriteriaId = w.CriteriaId ?? throw new ValidationException("Invalid Criteria Id"),
                CriteriaScore = w.Grade,
                FeedbackId = feedback.Id,
            }).ToList();

            await _dbContext.AddRangeAsync(newGrades, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.MapSingle(feedback);
    }

    public async Task DeleteFeedback(
        string id,
        CancellationToken cancellationToken
    )
    {
        var spec = _accessService.GetSpecification();

        var feedback = await spec.Apply(_dbContext.Feedbacks)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (feedback is null)
            throw new NotFoundException(nameof(Feedback), id);

        feedback.IsDeleted = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasFeedbackAsync(string workloadId, CancellationToken cancellationToken)
    {
        var spec = _accessService.GetSpecification();
        return await spec.Apply(_dbContext.Feedbacks)
            .AnyAsync(f => f.WorkloadId == workloadId, cancellationToken);
    }

    public async Task<List<string>> GetCommentByTeacherId(
        string id,
        CancellationToken cancellationToken
    )
    {

        var query = _dbContext.Feedbacks.Where(w => w.WorkloadRef.TeacherId.Equals(id));

        var comments = await _mapper.ProjectToDto(query).Select(w => w.Comment).ToListAsync(cancellationToken);

        if (comments is null)
            return ["Comment not found"];

        return comments;
    }

    public async Task<List<string>> GetCommentByDisciplineId(
        string id,
        CancellationToken cancellationToken
    )
    {
        var query = _dbContext.Feedbacks.Where(w => w.WorkloadRef.DisciplineId.Equals(id));

        var comments = await _mapper.ProjectToDto(query).Select(w => w.Comment).ToListAsync(cancellationToken);

        if (comments is null)
            return ["Comment not found"];

        return comments;
    }
}
