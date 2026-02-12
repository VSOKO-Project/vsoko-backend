using Application.Common.DTOs;
using Application.Interfaces.DataManager.Repositories;
using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces.DataManager.Repositories;

public interface ICriteriaRepository : IRepository<Criteria>
{
    public Task<List<CriteriaDto>> GetAllCriteria(CancellationToken cancellationToken);
    public Task<string> PostCriteria(
        string? name,
        CriteriaObject criteriaObject,
        CancellationToken cancellationToken
    );
    public Task DeleteCriteria(string id, CancellationToken cancellationToken);
    public Task<string> PutCriteria(
        string id,
        string? name,
        CriteriaObject criteriaObject,
        CancellationToken cancellationToken
    );
}
