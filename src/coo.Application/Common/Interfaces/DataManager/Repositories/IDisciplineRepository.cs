using coo.Domain.Entities;
using coo.Application.Common.Interfaces.DataManager.Repositories;
using coo.Application.Common.Results;
using coo.Application.Common.DTOs;

namespace coo.Application.Common.Interfaces.DataManager;

public interface IDisciplineRepository : IRepository<Teacher>
{
    public Task<PagedResultDto<RatingDto>> GetRating(int page, string query, int pageSize, CancellationToken cancellationToken);
}