using Domain.Entities;
using Application.Common.Results;
using Application.Common.DTOs;

namespace Application.Interfaces.DataManager.Repositories;

public interface ITeacherRepository : IRepository<Teacher>
{
    public Task<PagedResultDto<RatingDto>> GetRating(int page, string query, int pageSize, CancellationToken cancellationToken);
}