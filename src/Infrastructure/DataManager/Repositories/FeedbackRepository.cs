using Application.Common.DTOs;
using Application.Common.Exceptions;
using Application.Common.Mappings;
using Application.Interfaces.DataManager.Repositories;
using Domain.Entities;
using Infrastructure.DataManager.Contexts;
using Microsoft.EntityFrameworkCore;

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

    public async Task<string> PostFeedback(
        IDictionary<string, int> grades,
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
            CriteriaId = w.Key,
            CriteriaScore = w.Value,
            FeedbackId = feedback.Id,
        }).ToList();

        await _dbContext.AddRangeAsync(gradeList, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return feedback.Id;
    }

    public async Task<List<FeedbackDto>> GetFeedbacksByStudentId(
        string studentId,
        CancellationToken cancellationToken
    )
    {
        var query = _dbContext.Feedbacks
            .Include(f => f.CriteriaFeedbackRefs)
                .ThenInclude(cf => cf.CriteriaRef)
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
            .FirstOrDefaultAsync(f => f.Id == id && f.StudentId == studentId, cancellationToken);

        if (feedback is null)
            throw new NotFoundException(nameof(Feedback), id);

        return _mapper.MapSingle(feedback);
    }

    public async Task<FeedbackDto> PutFeedback(
        string id,
        string studentId,
        string? comment,
        IDictionary<string, int>? grades,
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
            _dbContext.RemoveRange(feedback.CriteriaFeedbackRefs);

            var newGrades = grades.Select(w => new CriteriaFeedback
            {
                CriteriaId = w.Key,
                CriteriaScore = w.Value,
                FeedbackId = feedback.Id,
            }).ToList();

            await _dbContext.AddRangeAsync(newGrades, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.MapSingle(feedback);
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
}
