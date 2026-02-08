using coo.Application.Common.DTOs;
using coo.Application.Common.Interfaces.DataManager.Repositories;
using coo.Domain.Entities;
using coo.Domain.Enums;

namespace coo.Application.Common.Interfaces.DataManager;

public interface ICriteriaRepository : IRepository<Criteria>
{
    public Task<List<CriteriaDto>> GetAllCriteria(CancellationToken cancellationToken);
    public Task<string> PostCriteria(string? name, CriteriaObject criteriaObject, CancellationToken cancellationToken);
    public Task DeleteCriteria(string id, CancellationToken cancellationToken);
    public Task<string> PutCriteria(string id, string? name, CriteriaObject criteriaObject, CancellationToken cancellationToken);
}