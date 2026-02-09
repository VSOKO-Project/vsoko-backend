using Application.Common.Exceptions;
using Application.Interfaces.DataManager.Repositories;
using Domain.Entities;
using Infrastructure.DataManager.Contexts;

namespace Infrastructure.DataManager.Repositories;

public class FeedbackRepository : IFeedbackRepository
{
    private readonly AppDbContext _dbContext;
    public FeedbackRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<string> PostFeedback(IDictionary<string, int> grades, string? comment, string workloadId, string? studentId, CancellationToken cancellationToken)
    {
        var feedback = new Feedback
        {
            Comment = comment ?? "",
            WorkloadId = workloadId,
            StudentId = studentId ?? throw new UnauthorizationException("non Id in claims!")
        };

        await _dbContext.AddAsync(feedback, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var gradeList = new List<CriteriaFeedback>();

        gradeList.AddRange(grades.Select(w => new CriteriaFeedback
        {
            CriteriaId = w.Key,
            CriteriaScore = w.Value,
            FeedbackId = feedback.Id
        }));

        await _dbContext.AddRangeAsync(gradeList, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return feedback.Id;

    }
}