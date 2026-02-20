using Application.Interfaces.DataManager.Repositories;
using Application.Common.DTOs;
using Domain.Entities;
using Application.Features.FeedbackFeatures.Command;

namespace Application.Interfaces.DataManager.Repositories;

public interface IFeedbackRepository : IRepository<Feedback>
{
    public Task<FeedbackDto> PostFeedback(
        List<Grades> grades,
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
        List<Grades> grades,
        CancellationToken cancellationToken
    );

    public Task DeleteFeedback(
        string id,
        string studentId,
        CancellationToken cancellationToken
    );

    Task<bool> HasFeedbackAsync(string studentId, string workloadId, CancellationToken cancellationToken);
}
