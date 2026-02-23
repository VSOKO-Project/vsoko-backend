using Application.Common.DTOs;
using Application.Common.Results;
using Application.Interfaces.DataManager.Repositories;
using Domain.Entities;

namespace Application.Interfaces.DataManager.Repositories;

public interface IDisciplineRepository : IRepository<Teacher>
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

    public Task<DisciplineDto> GetByIdAsync(
        string id,
        CancellationToken cancellationToken
    );
}
