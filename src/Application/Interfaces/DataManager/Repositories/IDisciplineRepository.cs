using Domain.Entities;
using Application.Interfaces.DataManager.Repositories;
using Application.Common.Results;
using Application.Common.DTOs;

namespace Application.Interfaces.DataManager.Repositories;

public interface IDisciplineRepository : IRepository<Teacher>
{
    public Task<PagedResultDto<RatingDto>> GetRating(int page, string query, int pageSize, CancellationToken cancellationToken);
}