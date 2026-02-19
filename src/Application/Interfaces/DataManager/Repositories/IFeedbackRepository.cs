using Application.Interfaces.DataManager.Repositories;
using Application.Common.DTOs;
using Domain.Entities;

namespace Application.Interfaces.DataManager.Repositories;

public interface IFeedbackRepository : IRepository<Feedback>
{
    public Task<string> PostFeedback(
        IDictionary<string, int> grades,
        string? comment,
        string workloadId,
        string? studentId,
        CancellationToken cancellationToken
    );

    public Task<List<FeedbackDto>> GetFeedbacksByStudentId(
        string studentId,
        CancellationToken cancellationToken
    );

    public Task<FeedbackDto> GetFeedbackById(
        string id,
        string studentId,
        CancellationToken cancellationToken
    );

    public Task<FeedbackDto> PutFeedback(
        string id,
        string studentId,
        string? comment,
        IDictionary<string, int>? grades,
        CancellationToken cancellationToken
    );

    public Task DeleteFeedback(
        string id,
        string studentId,
        CancellationToken cancellationToken
    );
}
