using Application.Common.DTOs;
using Application.Common.Mappings;
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
    private readonly CriteriaMapper _mapper;

    public CriteriaRepository(AppDbContext dbContext, CriteriaMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<List<CriteriaDto>> GetAllCriteria(CancellationToken cancellationToken)
    {
        return await _mapper.ProjectToDto(_dbContext.Criterias).ToListAsync(cancellationToken);
    }

    public async Task<CriteriaDto> GetCriteriaById(string id, CancellationToken cancellationToken)
    {
        var criteria = await _dbContext.Criterias.FindAsync(id, cancellationToken);

        if (criteria is null)
            throw new NotFoundException(nameof(Criteria), id);

        return _mapper.MapSingle(criteria);
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

        return _mapper.MapSingle(newCriteria);
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

        return _mapper.MapSingle(criteria);
    }

    public async Task<List<RatingDto>> GetAllRatingAsync(
        CancellationToken cancellationToken
    )
    {
        var baseQuery = _dbContext
            .Criterias.Include(w => w.CriteriaFeedbackRefs);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await _mapper.ProjectToRating(baseQuery)
            .ToListAsync(cancellationToken);

        return items;
    }
}
