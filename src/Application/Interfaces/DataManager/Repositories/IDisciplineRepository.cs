using Application.Common.DTOs;
using Application.Common.Periods;
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
        PeriodFilter period,
        CancellationToken cancellationToken
    );
    public Task<List<RatingDto>> GetAllRatingAsync(
        PeriodFilter period,
        CancellationToken cancellationToken
    );

    public Task<DisciplineDto> GetByIdAsync(
        string id,
        CancellationToken cancellationToken
    );

    public Task<PagedResultDto<DisciplineDto>> GetAllAsync(
        int page,
        string query,
        int pageSize,
        CancellationToken cancellationToken
    );
}
