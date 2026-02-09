using Application.Interfaces.DataManager.Repositories;
using Domain.Entities;

namespace Application.Interfaces.DataManager.Repositories;

public interface IFeedbackRepository : IRepository<Feedback>
{
    public Task<string> PostFeedback(IDictionary<string, int> grades, string? comment, string workloadId, string? studentId, CancellationToken cancellationToken);
}