using Application.Common.DTOs;
using Application.Common.Results;
using Domain.Entities;

namespace Application.Interfaces.DataManager.Repositories;

public interface ITeacherRepository : IRepository<Teacher>
{
    public Task<PagedResultDto<RatingDto>> GetRating(
        int page,
        string query,
        int pageSize,
        CancellationToken cancellationToken
    );
}
