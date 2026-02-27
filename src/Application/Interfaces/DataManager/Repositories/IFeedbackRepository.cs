using Application.Common.DTOs;
using Domain.Entities;
using Application.Features.FeedbackFeatures.Command;
using Application.Common.Results;

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

    public Task<PagedResultDto<FeedbackDto>> GetPagedFeedbacks(
        int page,
        int pageSize,
        string? disciplineId,
        string? teacherId,
        string? workloadId,
        CancellationToken cancellationToken
    );

    public Task<FeedbackDto> GetFeedbackById(
        string id,
        CancellationToken cancellationToken
    );

    public Task<FeedbackDto> PutFeedback(
        string id,
        string? comment,
        List<Grades> grades,
        CancellationToken cancellationToken
    );

    public Task DeleteFeedback(
        string id,
        CancellationToken cancellationToken
    );

    Task<bool> HasFeedbackAsync(string workloadId, CancellationToken cancellationToken);

    public Task<List<string>> GetCommentByDisciplineId(
        string id,
        CancellationToken cancellationToken
    );

    public Task<List<string>> GetCommentByTeacherId(
        string id,
        CancellationToken cancellationToken
    );

    public Task<List<FeedbackDto>> GetAllFeedbacksAsync(
        CancellationToken cancellationToken
    );
}
