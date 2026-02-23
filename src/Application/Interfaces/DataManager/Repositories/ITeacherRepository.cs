using Application.Common.DTOs;
using Application.Common.Results;
using Domain.Entities;

namespace Application.Interfaces.DataManager.Repositories;

public interface ITeacherRepository : IRepository<Teacher>
{
    public Task<PagedResultDto<RatingDto>> GetRatingAsync(
        int page,
        string query,
        int pageSize,
        CancellationToken cancellationToken
    );

    public Task<List<RatingDto>> GetAllRatingAsync(
        CancellationToken cancellationToken
    );
}
