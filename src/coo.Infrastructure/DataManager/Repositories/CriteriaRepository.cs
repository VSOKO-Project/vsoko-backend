using coo.Application.Common.DTOs;
using coo.Application.Common.Exceptions;
using coo.Application.Common.Interfaces.DataManager;
using coo.Domain.Entities;
using coo.Domain.Enums;
using coo.Infrastructure.DataManager.Contexts;
using Microsoft.EntityFrameworkCore;

namespace coo.Infrastructure.DataManager.Repositories;

public class CriteriaRepository : ICriteriaRepository
{
    private readonly AppDbContext _dbContext;
    public CriteriaRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<CriteriaDto>> GetAllCriteria(CancellationToken cancellationToken)
    {
        return await _dbContext.Criterias.Select(w => new CriteriaDto
        {
            Name = w.Name,
            Id = w.Id
        }).ToListAsync(cancellationToken);
    }
    public async Task<string> PostCriteria(string? name, CriteriaObject criteriaObject, CancellationToken cancellationToken)
    {
        var newCriteria = new Criteria
        {
            Name = name ?? "?",
            Object = criteriaObject
        };

        await _dbContext.Criterias.AddAsync(newCriteria, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return newCriteria.Id;
    }
    public async Task DeleteCriteria(string id, CancellationToken cancellationToken)
    {
        var criteria = await _dbContext.Criterias.FindAsync(id, cancellationToken);
        if(criteria != null)
        {
            _dbContext.Criterias.Remove(criteria);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
    public async Task<string> PutCriteria(string id, string? name, CriteriaObject criteriaObject, CancellationToken cancellationToken)
    {
        var criteria = await _dbContext.Criterias.FindAsync(id, cancellationToken);

        if (criteria is null)
            throw new NotFoundException(nameof(Criteria), id);

        criteria.Name = name ?? "?";
        criteria.Object = criteriaObject;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return criteria.Id;
    }
}