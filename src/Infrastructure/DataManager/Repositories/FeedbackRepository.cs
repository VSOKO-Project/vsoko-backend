using Application.Common.DTOs;
using Application.Common.Exceptions;
using Application.Common.Mappings;
using Application.Features.FeedbackFeatures.Command;
using Application.Interfaces.DataManager.Repositories;
using Domain.Entities;
using Infrastructure.DataManager.Contexts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Infrastructure.DataManager.Repositories;

public class FeedbackRepository : IFeedbackRepository
{
    private readonly AppDbContext _dbContext;
    private readonly FeedbackMapper _mapper;

    public FeedbackRepository(AppDbContext dbContext, FeedbackMapper mapper)
    {
        _dbContext = dbContext;
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

    public async Task<List<FeedbackDto>> GetFeedbacksByStudentId(
        string studentId,
        CancellationToken cancellationToken
    )
    {
        var query = _dbContext.Feedbacks
            .Include(f => f.CriteriaFeedbackRefs)
                .ThenInclude(cf => cf.CriteriaRef)
            .Include(f => f.WorkloadRef)
                .ThenInclude(w => w.DisciplineRef)
            .Include(f => f.WorkloadRef)
                .ThenInclude(w => w.TeacherRef)
            .Where(f => f.StudentId == studentId);

        return await _mapper.ProjectToDto(query).ToListAsync(cancellationToken);
    }

    public async Task<FeedbackDto> GetFeedbackById(
        string id,
        string studentId,
        CancellationToken cancellationToken
    )
    {
        var feedback = await _dbContext.Feedbacks
            .Include(f => f.CriteriaFeedbackRefs)
                .ThenInclude(cf => cf.CriteriaRef)
            .Include(f => f.WorkloadRef)
                .ThenInclude(w => w.DisciplineRef)
            .Include(f => f.WorkloadRef)
                .ThenInclude(w => w.TeacherRef)
            .FirstOrDefaultAsync(f => f.Id == id && f.StudentId == studentId, cancellationToken);

        if (feedback is null)
            throw new NotFoundException(nameof(Feedback), id);

        return _mapper.MapSingle(feedback);
    }

    public async Task<FeedbackDto> PutFeedback(
        string id,
        string studentId,
        string? comment,
        List<Grades>? grades,
        CancellationToken cancellationToken
    )
    {
        var feedback = await _dbContext.Feedbacks
            .Include(f => f.CriteriaFeedbackRefs)
            .FirstOrDefaultAsync(f => f.Id == id && f.StudentId == studentId, cancellationToken);

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

    public async Task DeleteFeedback(
        string id,
        string studentId,
        CancellationToken cancellationToken
    )
    {
        var feedback = await _dbContext.Feedbacks
            .FirstOrDefaultAsync(f => f.Id == id && f.StudentId == studentId, cancellationToken);

        if (feedback is null)
            throw new NotFoundException(nameof(Feedback), id);

        feedback.IsDeleted = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasFeedbackAsync(string studentId, string workloadId, CancellationToken cancellationToken)
    {
        return await _dbContext.Feedbacks
            .AnyAsync(f => f.StudentId == studentId && f.WorkloadId == workloadId, cancellationToken);
    }
}
