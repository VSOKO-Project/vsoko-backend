using Application.Common.DTOs;
using Application.Common.Exceptions;
using Application.Interfaces.DataManager.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataManager.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataManager.Repositories;

public class CriteriaRepository : ICriteriaRepository
{
    private readonly AppDbContext _dbContext;

    public CriteriaRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<CriteriaDto>> GetAllCriteria(CancellationToken cancellationToken)
    {
        return await _dbContext
            .Criterias.Select(w => new CriteriaDto { Name = w.Name, Id = w.Id , Object = w.Object})
            .ToListAsync(cancellationToken);
    }

    public async Task<CriteriaDto> PostCriteria(
        string? name,
        CriteriaObject criteriaObject,
        CancellationToken cancellationToken
    )
    {
        var newCriteria = new Criteria { Name = name ?? "?", Object = criteriaObject };

        await _dbContext.Criterias.AddAsync(newCriteria, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CriteriaDto
        {
            Name = newCriteria.Name,
            Id = newCriteria.Id,
            Object = newCriteria.Object
        };
    }

    public async Task DeleteCriteria(string id, CancellationToken cancellationToken)
    {
        var criteria = await _dbContext.Criterias.FindAsync(id, cancellationToken);
        if (criteria != null)
        {
            criteria.IsDeleted = true;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<CriteriaDto> PutCriteria(
        string id,
        string? name,
        CriteriaObject criteriaObject,
        CancellationToken cancellationToken
    )
    {
        var criteria = await _dbContext.Criterias.FindAsync(id, cancellationToken);

        if (criteria is null)
            throw new NotFoundException(nameof(Criteria), id);

        criteria.Name = name ?? "?";
        criteria.Object = criteriaObject;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CriteriaDto
        {
            Id = criteria.Id,
            Name = criteria.Name,
            Object = criteria.Object
        };
    }
}
