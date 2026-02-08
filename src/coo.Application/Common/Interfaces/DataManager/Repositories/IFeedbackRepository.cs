using coo.Application.Common.Interfaces.DataManager.Repositories;
using coo.Domain.Entities;

namespace coo.Application.Common.Interfaces.DataManager;

public interface IFeedbackRepository : IRepository<Feedback>
{
    public Task<string> PostFeedback(IDictionary<string, int> grades, string? comment, string workloadId, string? studentId, CancellationToken cancellationToken);
}